using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.RegularExpressions;
using WebApit4s.DAL;
using WebApit4s.Identity;
using WebApit4s.Models;
using WebApit4s.ViewModels;


namespace WebApit4s.Controllers
{
    [Authorize]
    public class VideoRewardController : Controller
    {
        private readonly TimeContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public VideoRewardController(TimeContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
           
        }

        private async Task<ApplicationUser?> GetCurrentUserAsync() => await _userManager.GetUserAsync(User);

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var user = GetCurrentUserAsync().Result;

            if (user != null)
            {
                // Try to get ActiveChildId from session
                var activeChildId = context.HttpContext.Session.GetInt32("ActiveChildId");

                Child child = null;
                if (activeChildId.HasValue)
                {
                    // Prefer active child from session
                    child = _context.Children.FirstOrDefault(c => c.Id == activeChildId.Value && c.UserId == user.Id);
                }

                // Fallback: first child if no active session child found
                if (child == null)
                {
                    child = _context.Children.FirstOrDefault(c => c.UserId == user.Id);
                }

                ViewBag.UserEmail = user.Email ?? "No Email Found";
                ViewBag.ChildName = child?.ChildName ?? "No Child Assigned";
                ViewBag.ChildAvatar = !string.IsNullOrEmpty(child?.AvatarUrl)
                               ? child.AvatarUrl
                               : "/images/default-child-avatar.png";  // ✅ added avatar
            }
            else
            {
                ViewBag.UserEmail = "Guest";
                ViewBag.ChildName = "Guest";
                ViewBag.ChildAvatar = "/images/default-child-avatar.png"; // ✅ added avatar for guest
            }

            base.OnActionExecuting(context);
        }
        private static string? ExtractYouTubeId(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;
            try
            {
                var u = new Uri(url);
                var host = u.Host.ToLowerInvariant();

                if (host.Contains("youtube.com"))
                {
                    var query = System.Web.HttpUtility.ParseQueryString(u.Query);
                    var v = query["v"];
                    if (!string.IsNullOrWhiteSpace(v)) return v;

                    var parts = u.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 1 && (parts[0].Equals("embed", StringComparison.OrdinalIgnoreCase) ||
                                             parts[0].Equals("shorts", StringComparison.OrdinalIgnoreCase)))
                        return parts[1];
                }
                if (host.Contains("youtu.be"))
                {
                    var parts = u.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 0) return parts[^1];
                }
            }
            catch { }
            var trimmed = url.Trim();
            return Regex.IsMatch(trimmed, "^[a-zA-Z0-9_-]{6,}$") ? trimmed : null;
        }

        private static string YouTubeThumb(string url)
        {
            var id = ExtractYouTubeId(url);
            return id is null ? "" : $"https://img.youtube.com/vi/{id}/hqdefault.jpg";
        }

        private static string CategoryDisplayName(VideoCategory c)
        {
            var mem = typeof(VideoCategory).GetMember(c.ToString()).FirstOrDefault();
            var attr = mem?.GetCustomAttribute<DisplayAttribute>();
            return attr?.GetName() ?? c.ToString();
        }

        // GET: /VideoReward/List?category=FruitVeg   (optional)
        public async Task<IActionResult> List()
        {

            ViewData["ActivePage"] = "VideoReward";

            var activeChildId = HttpContext.Session.GetInt32("ActiveChildId");
            if (activeChildId == null)
            {
                TempData["Error"] = "Select a child first.";
                return RedirectToAction("Index", "Dashboard");
            }

            var items = await _context.VideoRewards
                .AsNoTracking()
                .Where(v => v.IsActive)
                .OrderByDescending(v => v.Id)
                .Select(v => new VideoSuggestionItemVM
                {
                    Id = v.Id,
                    Title = v.Title,
                    ThumbnailUrl = YouTubeThumb(v.YouTubeUrl),
                    CoinValue = v.CoinValue,
                    Category = v.Category
                })
                .ToListAsync();

            // Group only categories that have videos
            var groups = items
     .GroupBy(i => i.Category)
     .OrderBy(g => g.Key == VideoCategory.Miscellaneous ? 1 : 0) // Put Miscellaneous last
     .ThenBy(g => g.Key.ToString()) // Order others alphabetically
     .Select(g => new VideoRewardCategoryGroupVM
     {
         Category = g.Key,
         CategoryDisplayName = CategoryDisplayName(g.Key),
         Videos = g.ToList()
     })
     .ToList();


            return View(groups); // Views/VideoReward/List.cshtml
        }

        [HttpGet]
        public async Task<IActionResult> Watch(int id)
        {
            // Debug: log the incoming ID
            Console.WriteLine($"[VideoReward.Watch] Requested ID: {id}");

            // Step 1: Validate child selection
            var activeChildId = HttpContext.Session.GetInt32("ActiveChildId");
            if (activeChildId == null)
            {
                TempData["Error"] = "Select a child first.";
                return RedirectToAction("Index", "Dashboard");
            }

            // Step 2: Check if there are any videos at all
            var totalVideos = await _context.VideoRewards.CountAsync();
            Console.WriteLine($"[VideoReward.Watch] Total videos in DB: {totalVideos}");

            if (totalVideos == 0)
            {
                TempData["Error"] = "No videos exist in the database.";
                return RedirectToAction("Index", "Dashboard");
            }

            // Step 3: Find video by ID
            var video = await _context.VideoRewards
                .FirstOrDefaultAsync(v => v.Id == id);

            if (video == null)
            {
                TempData["Error"] = $"Video with ID {id} not found.";
                return RedirectToAction("Index", "Dashboard");
            }

            // Step 4: Ensure it's active
            if (!video.IsActive)
            {
                TempData["Error"] = "This video is not active.";
                return RedirectToAction("Index", "Dashboard");
            }

            // Debug: success
            Console.WriteLine($"[VideoReward.Watch] Found video: {video.Title}");

            // Step 5: Show the watch page
            return View(video);
        }


        // POST: /VideoReward/Start
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Start(int videoId)
        {
            var activeChildId = HttpContext.Session.GetInt32("ActiveChildId");
            if (activeChildId == null) return Unauthorized();

            var v = await _context.VideoRewards.FindAsync(videoId);
            if (v == null || !v.IsActive) return NotFound();

            // enforce max awards & cooldown
            var now = DateTime.UtcNow;

            if (v.MaxAwardsPerChild > 0)
            {
                var count = await _context.UserPointHistories
                    .CountAsync(h => h.ChildId == activeChildId && h.SourceType == "VideoReward" && h.SourceId == videoId && h.Reason == PointChangeReason.VideoWatched);
                if (count >= v.MaxAwardsPerChild) return BadRequest("Max awards reached for this video.");
            }

            if (v.CooldownHoursPerChild > 0)
            {
                var since = now.AddHours(-v.CooldownHoursPerChild);
                var recent = await _context.UserPointHistories
                    .AnyAsync(h => h.ChildId == activeChildId && h.SourceType == "VideoReward" && h.SourceId == videoId && h.Reason == PointChangeReason.VideoWatched && h.CreatedUtc >= since);
                if (recent) return BadRequest("Cooldown active. Try again later.");
            }

            var log = new VideoWatchLog
            {
                ChildId = activeChildId.Value,
                VideoRewardId = videoId,
                UserAgent = Request.Headers.UserAgent.ToString(),
                ClientIp = HttpContext.Connection.RemoteIpAddress?.ToString()
            };
            _context.VideoWatchLogs.Add(log);
            await _context.SaveChangesAsync();

            return Json(new { sessionId = log.Id });
        }

        // POST: /VideoReward/Progress
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Progress(Guid sessionId, double currentTime, double duration, double deltaPlayed)
        {
            var log = await _context.VideoWatchLogs.FindAsync(sessionId);
            if (log == null) return NotFound();

            log.VideoDurationSeconds = Math.Max(log.VideoDurationSeconds, duration);
            log.DurationSeconds += Math.Max(0, deltaPlayed);             // add played time since last ping
            log.MaxPositionSeconds = Math.Max(log.MaxPositionSeconds, currentTime);

            await _context.SaveChangesAsync();
            return Ok();
        }

        // POST: /VideoReward/Complete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(Guid sessionId)
        {
            var log = await _context.VideoWatchLogs.FindAsync(sessionId);
            if (log == null) return NotFound();

            var video = await _context.VideoRewards.FindAsync(log.VideoRewardId);
            var child = await _context.Children.FindAsync(log.ChildId);
            if (video == null || child == null) return NotFound();

            log.EndedUtc = DateTime.UtcNow;

            // Compute % watched from best-known metrics
            var denom = Math.Max(log.VideoDurationSeconds, log.MaxPositionSeconds);
            var pct = denom > 0 ? (log.MaxPositionSeconds / denom) * 100.0 : 0.0;
            log.PercentWatched = Math.Min(100.0, Math.Round(pct, 2));

            // gate: award once per session when threshold met and not already awarded
            if (!log.CoinsAwarded && log.PercentWatched >= video.MinWatchPercent)
            {
                // Update child's coins + ledger
                child.TotalPoints += video.CoinValue;
                _context.UserPointHistories.Add(new UserPointHistory
                {
                    UserId = child.UserId,
                    ChildId = child.Id,
                    Delta = video.CoinValue,
                    BalanceAfter = child.TotalPoints,
                    Reason = PointChangeReason.VideoWatched,
                    SourceType = "VideoReward",
                    SourceId = video.Id,
                    Notes = $"Watched {video.Title} ({log.PercentWatched}%)",
                    CreatedUtc = DateTime.UtcNow
                });

                log.CoinsAwarded = true;
                log.AwardedUtc = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return Json(new { awarded = log.CoinsAwarded, percent = log.PercentWatched, coins = video.CoinValue });
        }
    }

}
