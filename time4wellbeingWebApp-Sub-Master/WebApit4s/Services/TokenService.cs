using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebApit4s.DAL;
using WebApit4s.Identity;
using WebApit4s.Models;
using WebApit4s.Services;
using WebApit4s.Services.Interfaces;

public class TokenService : ITokenService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly TimeContext _context;
    private readonly IJwtFactory _jwt; // keep if you already use this

    public TokenService(
        IHttpContextAccessor httpContextAccessor,
        UserManager<ApplicationUser> userManager,
        TimeContext context,
        IJwtFactory jwt // if you don't use this, replace with your own token builder
    )
    {
        _httpContextAccessor = httpContextAccessor;
        _userManager = userManager;
        _context = context;
        _jwt = jwt;
    }

    public async Task<(string accessToken, string refreshToken)> IssueAsync(
        ApplicationUser user, string? deviceId, string? userAgent, string? ip)
    {
        // Fallbacks from HttpContext if not supplied
        (ip, userAgent) = FillFromHttpContext(ip, userAgent);

        var (raw, hash) = RefreshTokenFactory.CreateToken();
        var familyId = Guid.NewGuid().ToString("N");

        var entity = new UserRefreshToken
        {
            UserId = user.Id,
            TokenHash = hash,
            FamilyId = familyId,
            DeviceId = deviceId,
            UserAgent = userAgent,
            IpAddress = ip,
            ExpiresUtc = DateTime.UtcNow.AddDays(30) // your policy
        };

        _context.UserRefreshTokens.Add(entity);
        await _context.SaveChangesAsync();

        var access = await _jwt.CreateAccessTokenAsync(user); // or your own builder
        return (access, raw);
    }

    public async Task<(string accessToken, string refreshToken)?> RefreshAsync(
        string refreshTokenRaw, string? deviceId, string? userAgent, string? ip)
    {
        // Fallbacks from HttpContext if not supplied
        (ip, userAgent) = FillFromHttpContext(ip, userAgent);

        var hash = RefreshTokenFactory.Hash(refreshTokenRaw);

        var token = await _context.UserRefreshTokens
            .Include(t => t.User)
            .SingleOrDefaultAsync(t => t.TokenHash == hash);

        if (token == null || !token.IsActive)
        {
            // Optional: revoke family if reuse detected
            // await RevokeFamilyByTokenHash(hash);
            return null;
        }

        // Mark used (rotation)
        token.UsedUtc = DateTime.UtcNow;
        _context.UserRefreshTokens.Update(token);

        // Rotate new token in same family
        var (raw, newHash) = RefreshTokenFactory.CreateToken();
        var rotated = new UserRefreshToken
        {
            UserId = token.UserId,
            TokenHash = newHash,
            ParentTokenHash = token.TokenHash,
            FamilyId = token.FamilyId,
            DeviceId = deviceId ?? token.DeviceId,
            UserAgent = userAgent,
            IpAddress = ip,
            ExpiresUtc = DateTime.UtcNow.AddDays(30)
        };
        _context.UserRefreshTokens.Add(rotated);

        await _context.SaveChangesAsync();

        var access = await _jwt.CreateAccessTokenAsync(token.User); // or your own builder
        return (access, raw);
    }

    public async Task RevokeAsync(string userId, string? deviceId = null, bool allDevices = false)
    {
        var q = _context.UserRefreshTokens.Where(t => t.UserId == userId && t.RevokedUtc == null);
        if (!allDevices && deviceId is not null)
            q = q.Where(t => t.DeviceId == deviceId);

        var list = await q.ToListAsync();
        foreach (var t in list)
            t.RevokedUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    private (string? ip, string? userAgent) FillFromHttpContext(string? ip, string? userAgent)
    {
        var http = _httpContextAccessor.HttpContext;
        if (http is null) return (ip, userAgent);

        ip ??= http.Connection.RemoteIpAddress?.ToString();
        userAgent ??= http.Request.Headers.UserAgent.ToString();

        // If you use a reverse proxy, consider X-Forwarded-For:
        if (string.IsNullOrWhiteSpace(ip) && http.Request.Headers.TryGetValue("X-Forwarded-For", out var xff))
        {
            // take first IP in the list
            ip = xff.ToString().Split(',').FirstOrDefault()?.Trim();
        }

        return (ip, userAgent);
    }
}
