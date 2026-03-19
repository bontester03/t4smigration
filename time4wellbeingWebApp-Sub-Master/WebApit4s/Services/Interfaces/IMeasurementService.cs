using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WebApit4s.DTO.Measurements;

namespace WebApit4s.Services.Interfaces
{
    public interface IMeasurementService
    {
        Task<IReadOnlyList<MeasurementDto>> GetListAsync(string userId, int? activeChildId, bool isAdmin, int take = 25, int skip = 0, CancellationToken ct = default);
        Task<MeasurementDto?> GetLatestAsync(string userId, int? activeChildId, bool isAdmin, CancellationToken ct = default);
        Task<int> UpsertAsync(string userId, UpsertMeasurementRequest request, int? activeChildId, bool isAdmin, CancellationToken ct = default);
        Task DeleteAsync(string userId, int id, bool isAdmin, CancellationToken ct = default); // soft delete
    }
}
