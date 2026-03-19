using System.Security.Claims;
using System.IO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApit4s.DAL;
using WebApit4s.DTO.Children;
using WebApit4s.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace WebApit4s.API
{
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Produces("application/json")]
    [Route("api/children")]
    public sealed class ApiChildrenController : ControllerBase
    {
        private readonly TimeContext _db;
        private readonly IWebHostEnvironment _env;

        public ApiChildrenController(TimeContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        private string? CurrentUserId =>
            User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

        // GET: api/children
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ChildDto>>> List([FromQuery] bool includeDeleted = false, CancellationToken ct = default)
        {
            var userId = CurrentUserId;
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var q = _db.Children.AsNoTracking().Where(c => c.UserId == userId);
            if (!includeDeleted) q = q.Where(c => !c.IsDeleted);

            var list = await q.OrderByDescending(c => c.UpdatedAt).ToListAsync(ct);
            return Ok(list.Select(c => c.ToDto()));
        }

        // GET: api/children/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ChildDto>> Get(int id, CancellationToken ct = default)
        {
            var userId = CurrentUserId;
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var child = await _db.Children.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId && !c.IsDeleted, ct);

            if (child is null) return NotFound();
            return Ok(child.ToDto());
        }

        // POST: api/children
        [HttpPost]
        public async Task<ActionResult<ChildDto>> Create([FromBody] CreateChildDto dto, CancellationToken ct = default)
        {
            var userId = CurrentUserId;
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var now = DateTime.UtcNow;
            var entity = new Child
            {
                UserId = userId,
                ChildName = dto.ChildName,
                Gender = dto.Gender,
                DateOfBirth = dto.DateOfBirth,
                AvatarUrl = dto.AvatarUrl,
                LastLogin = now,
                EngagementStatus = dto.EngagementStatus ?? EngagementStatus.Engaged,
                TotalPoints = 0,
                Level = 1,
                IsDeleted = false,
                CreatedAt = now,
                UpdatedAt = now,
                ChildGuid = Guid.NewGuid()
            };

            _db.Children.Add(entity);
            await _db.SaveChangesAsync(ct);

            var read = entity.ToDto();
            return CreatedAtAction(nameof(Get), new { id = entity.Id }, read);
        }

        // PUT: api/children/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateChildDto dto, CancellationToken ct = default)
        {
            var userId = CurrentUserId;
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var entity = await _db.Children
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId && !c.IsDeleted, ct);

            if (entity is null) return NotFound();

            entity.ChildName = dto.ChildName;
            entity.Gender = dto.Gender;
            entity.DateOfBirth = dto.DateOfBirth;
            entity.AvatarUrl = dto.AvatarUrl;
            entity.EngagementStatus = dto.EngagementStatus;
            entity.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
            return NoContent();
        }

        // PATCH: api/children/5/avatar
        [HttpPatch("{id:int}/avatar")]
        public async Task<IActionResult> UpdateAvatar(int id, [FromBody] UpdateAvatarDto dto, CancellationToken ct = default)
        {
            var userId = CurrentUserId;
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var entity = await _db.Children
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId && !c.IsDeleted, ct);

            if (entity is null) return NotFound();

            entity.AvatarUrl = dto.AvatarUrl;
            entity.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
            return NoContent();
        }

        // DELETE: api/children/5 (soft delete)
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> SoftDelete(int id, CancellationToken ct = default)
        {
            var userId = CurrentUserId;
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var entity = await _db.Children
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId && !c.IsDeleted, ct);

            if (entity is null) return NotFound();

            entity.IsDeleted = true;
            entity.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
            return NoContent();
        }

        // GET: api/children/avatars
        [HttpGet("avatars")]
        public ActionResult<IEnumerable<string>> GetAvatars()
        {
            var folder = Path.Combine(_env.WebRootPath, "characters"); // lowercase as in your site
            if (!Directory.Exists(folder)) return Ok(Array.Empty<string>());

            var urls = Directory.GetFiles(folder, "*.svg")
                .OrderBy(Path.GetFileName)
                .Select(f => "/characters/" + Path.GetFileName(f))
                .ToList();

            return Ok(urls);
        }
    }
}
