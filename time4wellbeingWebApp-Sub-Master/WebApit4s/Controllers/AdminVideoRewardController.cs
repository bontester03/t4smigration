using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using WebApit4s.DAL;
using WebApit4s.Models;
using WebApit4s.ViewModels;
using X.PagedList;
using X.PagedList.Extensions;

namespace WebApit4s.Controllers
{
    [Authorize(Roles = "Admin,Employee")]
    public class AdminVideoRewardController : Controller
    {
        private readonly TimeContext _context;

        public AdminVideoRewardController(TimeContext context)
        {
            _context = context;
        }

        // GET: /AdminVideoReward
        public IActionResult Index(string? q, int page = 1, int pageSize = 10)
        {
            var query = _context.VideoRewards.AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                query = query.Where(v => v.Title.Contains(term) || v.YouTubeUrl.Contains(term));
            }

            var paged = query
                .OrderByDescending(v => v.Id)
                .ToPagedList(page, pageSize); // synchronous

            ViewBag.Search = q;
            return View(paged);
        }


        // GET: /AdminVideoReward/Create
        // GET: /AdminVideoReward/Create
        public IActionResult Create()
        {
            return View(new VideoReward
            {
                Title = string.Empty,
                YouTubeUrl = string.Empty,
                IsActive = true,
                CoinValue = 5,               // default coin value
                MinWatchPercent = 80,        // default rule
                CooldownHoursPerChild = 0,   // no cooldown by default
                MaxAwardsPerChild = 1,       // at most once by default
                Category = VideoCategory.Miscellaneous
            });
        }

        // POST: /AdminVideoReward/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(VideoReward model)
        {
            // Basic guards on new fields
            if (model.MinWatchPercent < 0 || model.MinWatchPercent > 100)
                ModelState.AddModelError(nameof(model.MinWatchPercent), "Min watch percent must be between 0 and 100.");

            if (model.CooldownHoursPerChild < 0)
                ModelState.AddModelError(nameof(model.CooldownHoursPerChild), "Cooldown must be 0 or greater.");

            if (model.MaxAwardsPerChild < 1)
                ModelState.AddModelError(nameof(model.MaxAwardsPerChild), "Max awards per child must be at least 1.");

            if (model.CoinValue < 0)
                ModelState.AddModelError(nameof(model.CoinValue), "Coin value cannot be negative.");

            if (!ModelState.IsValid)
                return View(model);

            // Light YouTube validation
            if (string.IsNullOrWhiteSpace(model.YouTubeUrl) || !model.YouTubeUrl.Contains("youtu", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(model.YouTubeUrl), "Please provide a valid YouTube URL.");
                return View(model);
            }

            model.Title = model.Title?.Trim();
            model.YouTubeUrl = model.YouTubeUrl?.Trim();

            _context.VideoRewards.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Video reward created.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /AdminVideoReward/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var video = await _context.VideoRewards.FindAsync(id);
            if (video == null) return NotFound();
            return View(video);
        }

        // POST: /AdminVideoReward/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, VideoReward model)
        {
            if (id != model.Id) return BadRequest();

            // Guards on new fields
            if (model.MinWatchPercent < 0 || model.MinWatchPercent > 100)
                ModelState.AddModelError(nameof(model.MinWatchPercent), "Min watch percent must be between 0 and 100.");

            if (model.CooldownHoursPerChild < 0)
                ModelState.AddModelError(nameof(model.CooldownHoursPerChild), "Cooldown must be 0 or greater.");

            if (model.MaxAwardsPerChild < 1)
                ModelState.AddModelError(nameof(model.MaxAwardsPerChild), "Max awards per child must be at least 1.");

            if (model.CoinValue < 0)
                ModelState.AddModelError(nameof(model.CoinValue), "Coin value cannot be negative.");

            if (!ModelState.IsValid) return View(model);

            var db = await _context.VideoRewards.FindAsync(id);
            if (db == null) return NotFound();

            // Light YouTube validation
            if (string.IsNullOrWhiteSpace(model.YouTubeUrl) || !model.YouTubeUrl.Contains("youtu", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(model.YouTubeUrl), "Please provide a valid YouTube URL.");
                return View(model);
            }

            // Update fields (including new ones)
            db.Title = model.Title?.Trim();
            db.YouTubeUrl = model.YouTubeUrl?.Trim();
            db.CoinValue = model.CoinValue;
            db.IsActive = model.IsActive;

            db.MinWatchPercent = model.MinWatchPercent;
            db.CooldownHoursPerChild = model.CooldownHoursPerChild;
            db.MaxAwardsPerChild = model.MaxAwardsPerChild;

            db.Category = model.Category;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Video reward updated.";
            return RedirectToAction(nameof(Index));
        }


        // POST: /AdminVideoReward/ToggleActive/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var video = await _context.VideoRewards.FindAsync(id);
            if (video == null) return NotFound();

            video.IsActive = !video.IsActive;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Video reward {(video.IsActive ? "activated" : "deactivated")}.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /AdminVideoReward/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var video = await _context.VideoRewards.FindAsync(id);
            if (video == null) return NotFound();

            _context.VideoRewards.Remove(video);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Video reward deleted.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult Result(
     string? q,
     int? childId,
     int? videoId,
     bool? awarded,
     DateTime? fromUtc,
     DateTime? toUtc,
     int page = 1,
     int pageSize = 20)
        {
            page = page <= 0 ? 1 : page;
            pageSize = pageSize <= 0 ? 20 : pageSize;

            // base query
            var query =
                from log in _context.VideoWatchLogs.AsNoTracking()
                join c in _context.Children.AsNoTracking() on log.ChildId equals c.Id
                join v in _context.VideoRewards.AsNoTracking() on log.VideoRewardId equals v.Id
                select new VideoWatchLogListItem
                {
                    Id = log.Id,
                    ChildId = c.Id,
                    ChildName = c.ChildName,
                    VideoRewardId = v.Id,
                    VideoTitle = v.Title,
                    StartedUtc = log.StartedUtc,
                    EndedUtc = log.EndedUtc,
                    DurationSeconds = log.DurationSeconds,
                    VideoDurationSeconds = log.VideoDurationSeconds,
                    MaxPositionSeconds = log.MaxPositionSeconds,
                    PercentWatched = log.PercentWatched,
                    CoinsAwarded = log.CoinsAwarded,
                    AwardedUtc = log.AwardedUtc,
                    ClientIp = log.ClientIp,
                    UserAgent = log.UserAgent,
                    DeviceId = log.DeviceId
                };

            // filters
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                query = query.Where(x =>
                    x.ChildName.Contains(term) ||
                    x.VideoTitle.Contains(term) ||
                    (x.ClientIp != null && x.ClientIp.Contains(term)) ||
                    (x.UserAgent != null && x.UserAgent.Contains(term)));
            }

            if (childId.HasValue) query = query.Where(x => x.ChildId == childId.Value);
            if (videoId.HasValue) query = query.Where(x => x.VideoRewardId == videoId.Value);
            if (awarded.HasValue) query = query.Where(x => x.CoinsAwarded == awarded.Value);

            if (fromUtc.HasValue) query = query.Where(x => x.StartedUtc >= fromUtc.Value);
            if (toUtc.HasValue) query = query.Where(x => x.StartedUtc <= toUtc.Value);

            // dropdown data
            ViewBag.Children = _context.Children
                .AsNoTracking()
                .OrderBy(c => c.ChildName)
                .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.ChildName })
                .ToList();

            ViewBag.Videos = _context.VideoRewards
                .AsNoTracking()
                .OrderBy(v => v.Title)
                .Select(v => new SelectListItem { Value = v.Id.ToString(), Text = v.Title })
                .ToList();

            ViewBag.Search = q;
            ViewBag.ChildId = childId;
            ViewBag.VideoId = videoId;
            ViewBag.Awarded = awarded;
            ViewBag.FromUtc = fromUtc?.ToString("yyyy-MM-dd");
            ViewBag.ToUtc = toUtc?.ToString("yyyy-MM-dd");

            // ✅ Use synchronous paging
            var paged = query
                .OrderByDescending(x => x.StartedUtc)
                .ToPagedList(page, pageSize);

            return View(paged);
        }



        // GET: /AdminVideoWatchLog/Details/{id}
        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            var model =
                await (from log in _context.VideoWatchLogs.AsNoTracking()
                       join c in _context.Children.AsNoTracking() on log.ChildId equals c.Id
                       join v in _context.VideoRewards.AsNoTracking() on log.VideoRewardId equals v.Id
                       where log.Id == id
                       select new VideoWatchLogListItem
                       {
                           Id = log.Id,
                           ChildId = c.Id,
                           ChildName = c.ChildName,
                           VideoRewardId = v.Id,
                           VideoTitle = v.Title,
                           StartedUtc = log.StartedUtc,
                           EndedUtc = log.EndedUtc,
                           DurationSeconds = log.DurationSeconds,
                           VideoDurationSeconds = log.VideoDurationSeconds,
                           MaxPositionSeconds = log.MaxPositionSeconds,
                           PercentWatched = log.PercentWatched,
                           CoinsAwarded = log.CoinsAwarded,
                           AwardedUtc = log.AwardedUtc,
                           ClientIp = log.ClientIp,
                           UserAgent = log.UserAgent,
                           DeviceId = log.DeviceId
                       }).FirstOrDefaultAsync();

            if (model == null) return NotFound();

            return View(model);
        }

        // GET: /AdminVideoWatchLog/ExportCsv
        [HttpGet]
        public async Task<IActionResult> ExportCsv(
            string? q,
            int? childId,
            int? videoId,
            bool? awarded,
            DateTime? fromUtc,
            DateTime? toUtc)
        {
            var query =
                from log in _context.VideoWatchLogs.AsNoTracking()
                join c in _context.Children.AsNoTracking() on log.ChildId equals c.Id
                join v in _context.VideoRewards.AsNoTracking() on log.VideoRewardId equals v.Id
                select new { log, c.ChildName, v.Title };

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                query = query.Where(x =>
                    x.ChildName.Contains(term) ||
                    x.Title.Contains(term) ||
                    (x.log.ClientIp != null && x.log.ClientIp.Contains(term)) ||
                    (x.log.UserAgent != null && x.log.UserAgent.Contains(term)));
            }

            if (childId.HasValue) query = query.Where(x => x.log.ChildId == childId.Value);
            if (videoId.HasValue) query = query.Where(x => x.log.VideoRewardId == videoId.Value);
            if (awarded.HasValue) query = query.Where(x => x.log.CoinsAwarded == awarded.Value);
            if (fromUtc.HasValue) query = query.Where(x => x.log.StartedUtc >= fromUtc.Value);
            if (toUtc.HasValue) query = query.Where(x => x.log.StartedUtc <= toUtc.Value);

            var rows = await query.OrderByDescending(x => x.log.StartedUtc).ToListAsync();

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Id,ChildId,ChildName,VideoId,VideoTitle,StartedUtc,EndedUtc,DurationSeconds,VideoDurationSeconds,MaxPositionSeconds,PercentWatched,CoinsAwarded,AwardedUtc,ClientIp,DeviceId,UserAgent");

            foreach (var r in rows)
            {
                string ua = r.log.UserAgent?.Replace("\"", "\"\"") ?? "";
                sb.AppendLine($"{r.log.Id},{r.log.ChildId},\"{r.ChildName}\",{r.log.VideoRewardId},\"{r.Title}\",{r.log.StartedUtc:O},{(r.log.EndedUtc.HasValue ? r.log.EndedUtc.Value.ToString("O") : "")},{r.log.DurationSeconds},{r.log.VideoDurationSeconds},{r.log.MaxPositionSeconds},{r.log.PercentWatched},{r.log.CoinsAwarded},{(r.log.AwardedUtc.HasValue ? r.log.AwardedUtc.Value.ToString("O") : "")},{r.log.ClientIp},{r.log.DeviceId},\"{ua}\"");
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "text/csv", $"video_watch_logs_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");
        }
    }
}
