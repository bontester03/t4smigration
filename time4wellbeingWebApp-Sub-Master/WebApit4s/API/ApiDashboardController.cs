using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Security.Claims;
using WebApit4s.DTO.Dashboard;
using WebApit4s.Services;

namespace WebApit4s.API  // ✅ Same as your other API controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]  // ✅ Match ApiChildrenController
    [Produces("application/json")]  // ✅ Match your other controllers
    public class ApiDashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public ApiDashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        private string? CurrentUserId =>
            User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

        [HttpGet]
        public async Task<ActionResult<DashboardResponse>> GetDashboard(
            [FromQuery] int? activeChildId,
            [FromQuery] int notificationsTake = 5,
            [FromQuery] int activitiesTake = 5,
            CancellationToken ct = default)
        {
            Console.WriteLine("🔵 ApiDashboardController.GetDashboard called");

            var userId = CurrentUserId;

            if (string.IsNullOrWhiteSpace(userId))
            {
                Console.WriteLine("❌ Could not extract userId");
                return Unauthorized(new { error = "User not authenticated" });
            }

            Console.WriteLine($"✅ Controller extracted userId: {userId}");

            var request = new DashboardRequest
            {
                ActiveChildId = activeChildId,
                NotificationsTake = notificationsTake,
                ActivitiesTake = activitiesTake,
                ClientVersion = "maui-1.0"
            };

            var response = await _dashboardService.BuildDashboardAsync(userId, request, ct);

            Console.WriteLine("✅ Dashboard response built successfully");

            return Ok(response);
        }
    }
}