using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebApit4s.DAL;        // TimeContext if you still need it elsewhere
using WebApit4s.DTO.Auth;   // LoginRequestDto, SignUpRequest, ForgotPasswordRequest, ResetPasswordRequest
using WebApit4s.Identity;
using WebApit4s.Services; // EmailSender
using WebApit4s.Services.Interfaces;// ITokenService
using WebApit4s.Services.Options;// ITokenService

namespace WebApit4s.API
{
    [ApiController]
    [Route("api/auth")]
    [Produces("application/json")]
    public class ApiAuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailSender _emailSender;      // your existing abstraction
        private readonly ITokenService _tokenService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ApiAuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IEmailSender emailSender,
            ITokenService tokenService,
            IHttpContextAccessor httpContextAccessor)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _tokenService = tokenService;
            _httpContextAccessor = httpContextAccessor;
        }

        // ----------------- LOGIN -----------------
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var user = await _userManager.Users.SingleOrDefaultAsync(u => u.Email == request.Email);
            if (user is null)
                return Unauthorized(new { message = "Invalid email or password." });

            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
            if (!result.Succeeded)
                return Unauthorized(new { message = "Invalid email or password." });

            var (ip, userAgent) = GetClientInfo();
            var deviceId = GetDeviceIdFromRequest(); // header: X-Device-Id (optional)

            var (access, refresh) = await _tokenService.IssueAsync(user, deviceId, userAgent, ip);

            // Include lightweight user payload as you prefer
            return Ok(new
            {
                user.Id,
                user.Email,
                Roles = await _userManager.GetRolesAsync(user),
                AccessToken = access,
                RefreshToken = refresh
            });
        }

        // ----------------- SIGNUP -----------------
        [HttpPost("signup")]
        [AllowAnonymous]
        public async Task<IActionResult> SignUp([FromBody] SignUpRequest request)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var existing = await _userManager.FindByEmailAsync(request.Email);
            if (existing != null)
                return Conflict(new { message = "Account already exists." });

            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                EmailConfirmed = true,
                RegistrationDate = DateTime.UtcNow,
                IsApprovedByAdmin = true
            };

            var create = await _userManager.CreateAsync(user, request.Password);
            if (!create.Succeeded)
                return BadRequest(new { errors = create.Errors.Select(e => e.Description) });

            // assign default role if you use roles (e.g., "Parent")
            // await _userManager.AddToRoleAsync(user, "Parent");

            return Ok(new { user.Id, message = "Account created successfully." });
        }

        // ----------------- FORGOT PASSWORD -----------------
        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest model)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
                return Ok(new { message = "If the email exists, a reset link has been sent." });

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            // Build callback URL (either provided base URL from client or fallback to MVC route)
            var callbackUrl = model.ResetCallbackBaseUrl is not null
                ? $"{model.ResetCallbackBaseUrl}?email={Uri.EscapeDataString(model.Email)}&token={Uri.EscapeDataString(token)}"
                : Url.Action("ResetPassword", "Account", new { token, email = model.Email }, Request.Scheme) ?? string.Empty;

            await _emailSender.SendEmailAsync(
                model.Email,
                "Reset Your Password",
                $"Please reset your password by <a href='{callbackUrl}'>clicking here</a>."
            );

            return Ok(new { message = "If the email exists, a reset link has been sent." });
        }

        // ----------------- RESET PASSWORD -----------------
        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest model)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return NotFound(new { message = "Invalid email." });

            var resetResult = await _userManager.ResetPasswordAsync(user, model.Token, model.NewPassword);
            if (!resetResult.Succeeded)
                return BadRequest(new { errors = resetResult.Errors.Select(e => e.Description) });

            return Ok(new { message = "Password reset successfully." });
        }

        // ----------------- REFRESH TOKEN -----------------
        [HttpPost("refresh-token")]
        [AllowAnonymous]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest model)
        {
            if (string.IsNullOrWhiteSpace(model.RefreshToken))
                return BadRequest(new { message = "Refresh token is required." });

            var (ip, userAgent) = GetClientInfo();
            var deviceId = GetDeviceIdFromRequest();

            var result = await _tokenService.RefreshAsync(model.RefreshToken, deviceId, userAgent, ip);
            if (result is null)
                return Unauthorized(new { message = "Invalid or expired refresh token." });

            var (access, refresh) = result.Value;
            return Ok(new { AccessToken = access, RefreshToken = refresh });
        }

        // ----------------- LOGOUT / REVOKE -----------------
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout([FromQuery] bool allDevices = false)
        {
            var userId = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier)
                        ?? User.FindFirstValue("sub"); // sub = user.Id in our JwtFactory

            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var deviceId = GetDeviceIdFromRequest();
            await _tokenService.RevokeAsync(userId, deviceId, allDevices);
            return Ok(new { message = allDevices ? "All sessions revoked." : "Session revoked." });
        }

        // ----------------- Helpers -----------------
        private (string? ip, string? userAgent) GetClientInfo()
        {
            var http = _httpContextAccessor.HttpContext;
            string? ip = null;
            string? userAgent = null;

            if (http is not null)
            {
                ip = http.Connection.RemoteIpAddress?.ToString();
                userAgent = http.Request.Headers.UserAgent.ToString();

                if (string.IsNullOrWhiteSpace(ip) &&
                    http.Request.Headers.TryGetValue("X-Forwarded-For", out var xff))
                {
                    ip = xff.ToString().Split(',').FirstOrDefault()?.Trim();
                }
            }
            return (ip, userAgent);
        }

        private string? GetDeviceIdFromRequest()
        {
            // Prefer a header e.g. X-Device-Id set by your MAUI app; fallback to null
            if (Request.Headers.TryGetValue("X-Device-Id", out var val))
                return val.ToString();

            return null;
        }
    }

    // Minimal DTO used for refresh
    public sealed class RefreshTokenRequest
    {
        public string RefreshToken { get; set; } = string.Empty;
    }
}
