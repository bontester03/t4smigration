using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebApit4s.DTO.Children;
using WebApit4s.DTO.Profile;
using WebApit4s.Services;
using WebApit4s.Services.Interfaces;
using WebAPIts.Services.Interfaces;

namespace WebApit4s.API
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Produces("application/json")]
    public sealed class ProfileController : ControllerBase
    {
        private readonly IProfileService _service;

        public ProfileController(IProfileService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<ProfileResponseDto>> GetProfile(
    [FromQuery] int? activeChildId = null, // ✅ Add this
    CancellationToken ct = default)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized();

            Console.WriteLine($"📥 API Controller: GetProfile called with activeChildId={activeChildId}");

            var result = await _service.GetProfileAsync(userId, activeChildId, ct); // ✅ Pass the parameter
            return result == null ? NotFound() : Ok(result);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken ct)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized();

            await _service.UpdateProfileAsync(userId, request, ct);
            return NoContent();
        }

        [HttpPut("avatar")]
        public async Task<IActionResult> UpdateAvatar([FromBody] UpdateAvatarDto request, CancellationToken ct)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized();

            await _service.UpdateAvatarAsync(userId, request, ct);
            return NoContent();
        }
    }
}
