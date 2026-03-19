using WebApit4s.Identity;

namespace WebApit4s.Services.Interfaces
{
    public interface IJwtFactory
    {
        Task<string> CreateAccessTokenAsync(ApplicationUser user, CancellationToken ct = default);
    }
}
