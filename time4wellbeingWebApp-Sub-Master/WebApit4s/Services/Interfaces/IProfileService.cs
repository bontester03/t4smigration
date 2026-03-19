using System.Threading.Tasks;
using WebApit4s.DTO.Children;
using WebApit4s.DTO.Profile;


namespace WebAPIts.Services.Interfaces
{
    public interface IProfileService
    {
        Task<ProfileResponseDto?> GetProfileAsync(string userId, int? activeChildId = null, CancellationToken ct = default); // ✅ Add parameter
        Task<bool> UpdateProfileAsync(string userId, UpdateProfileRequest request, CancellationToken ct = default);
        Task<bool> UpdateAvatarAsync(string userId, UpdateAvatarDto request, CancellationToken ct = default);
    }
}
