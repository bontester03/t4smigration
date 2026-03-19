using WebApit4s.Identity;

namespace WebApit4s.Services.Interfaces
{
    public interface ITokenService
    {
        Task<(string accessToken, string refreshToken)> IssueAsync(ApplicationUser user, string? deviceId, string? userAgent, string? ip);
        Task<(string accessToken, string refreshToken)?> RefreshAsync(string refreshTokenRaw, string? deviceId, string? userAgent, string? ip);
        Task RevokeAsync(string userId, string? deviceId = null, bool allDevices = false);
    }

}
