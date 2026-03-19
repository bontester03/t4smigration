using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WebApit4s.DAL;
using WebApit4s.DTO.HealthScores;

namespace WebApit4s.Services
{
    public sealed class HealthScoreService : WebApit4s.Services.Interfaces.IHealthScoreService
    {
        private readonly TimeContext _db;

        public HealthScoreService(TimeContext db) => _db = db;

        private async Task<int?> ResolveChildIdAsync(string userId, int? activeChildId, bool isAdmin, CancellationToken ct)
        {
            if (activeChildId.HasValue)
            {
                // ensure access
                var ok = await _db.Children
                    .AnyAsync(c => c.Id == activeChildId.Value && !c.IsDeleted && (isAdmin || c.UserId == userId), ct);
                return ok ? activeChildId : null;
            }

            // Default: pick oldest engaged child of this parent
            var childId = await _db.Children
                .Where(c => !c.IsDeleted && (isAdmin || c.UserId == userId))
                .OrderBy(c => c.DateOfBirth)
                .Select(c => (int?)c.Id)
                .FirstOrDefaultAsync(ct);

            return childId;
        }

        public async Task<IReadOnlyList<HealthScoreDto>> GetListAsync(string userId, int? activeChildId, bool isAdmin, int take = 25, int skip = 0, CancellationToken ct = default)
        {
            var childId = await ResolveChildIdAsync(userId, activeChildId, isAdmin, ct);
            if (!childId.HasValue) return Array.Empty<HealthScoreDto>();

            return await _db.HealthScores
                .AsNoTracking()
                .Where(h => h.ChildId == childId && !h.IsDeleted)
                .OrderByDescending(h => h.DateRecorded)
                .Skip(skip)
                .Take(take)
                .Select(h => new HealthScoreDto
                {
                    Id = h.Id,
                    ChildId = h.ChildId,
                    DateRecorded = h.DateRecorded,
                    PhysicalActivityScore = h.PhysicalActivityScore,
                    BreakfastScore = h.BreakfastScore,
                    FruitVegScore = h.FruitVegScore,
                    SweetSnacksScore = h.SweetSnacksScore,
                    FattyFoodsScore = h.FattyFoodsScore,
                    TotalScore = h.TotalScore ?? (h.PhysicalActivityScore + h.BreakfastScore + h.FruitVegScore + h.SweetSnacksScore + h.FattyFoodsScore),
                    OverallScore10 = Math.Round(((h.TotalScore ?? (h.PhysicalActivityScore + h.BreakfastScore + h.FruitVegScore + h.SweetSnacksScore + h.FattyFoodsScore)) / 2m), 1)
                })
                .ToListAsync(ct);
        }

        public async Task<HealthScoreDto?> GetLatestAsync(string userId, int? activeChildId, bool isAdmin, CancellationToken ct = default)
        {
            var childId = await ResolveChildIdAsync(userId, activeChildId, isAdmin, ct);
            if (!childId.HasValue) return null;

            var h = await _db.HealthScores
                .AsNoTracking()
                .Where(x => x.ChildId == childId && !x.IsDeleted)
                .OrderByDescending(x => x.DateRecorded)
                .FirstOrDefaultAsync(ct);

            if (h is null) return null;

            var total = h.TotalScore ?? (h.PhysicalActivityScore + h.BreakfastScore + h.FruitVegScore + h.SweetSnacksScore + h.FattyFoodsScore);
            return new HealthScoreDto
            {
                Id = h.Id,
                ChildId = h.ChildId,
                DateRecorded = h.DateRecorded,
                PhysicalActivityScore = h.PhysicalActivityScore,
                BreakfastScore = h.BreakfastScore,
                FruitVegScore = h.FruitVegScore,
                SweetSnacksScore = h.SweetSnacksScore,
                FattyFoodsScore = h.FattyFoodsScore,
                TotalScore = total,
                OverallScore10 = Math.Round(total / 2m, 1)
            };
        }

        public async Task<int> UpsertAsync(string userId, UpsertHealthScoreRequest request, int? activeChildId, bool isAdmin, CancellationToken ct = default)
        {
            var childId = request.ChildId ?? await ResolveChildIdAsync(userId, activeChildId, isAdmin, ct);
            if (!childId.HasValue) throw new KeyNotFoundException("Child not found or not accessible.");

            if (request.Id.HasValue)
            {
                var entity = await _db.HealthScores
                    .FirstOrDefaultAsync(h => h.Id == request.Id.Value && !h.IsDeleted, ct);
                if (entity == null) throw new KeyNotFoundException("Health score not found.");

                // authorisation check
                if (!isAdmin)
                {
                    var ownerOk = await _db.Children.AnyAsync(c => c.Id == entity.ChildId && c.UserId == userId, ct);
                    if (!ownerOk) throw new UnauthorizedAccessException("Not allowed.");
                }

                entity.ChildId = childId.Value;
                entity.DateRecorded = request.DateRecorded;
                entity.PhysicalActivityScore = request.PhysicalActivityScore;
                entity.BreakfastScore = request.BreakfastScore;
                entity.FruitVegScore = request.FruitVegScore;
                entity.SweetSnacksScore = request.SweetSnacksScore;
                entity.FattyFoodsScore = request.FattyFoodsScore;
                entity.TotalScore = request.PhysicalActivityScore + request.BreakfastScore + request.FruitVegScore + request.SweetSnacksScore + request.FattyFoodsScore;

                await _db.SaveChangesAsync(ct);
                return entity.Id;
            }
            else
            {
                var entity = new Models.HealthScore
                {
                    ChildId = childId.Value,
                    DateRecorded = request.DateRecorded,
                    PhysicalActivityScore = request.PhysicalActivityScore,
                    BreakfastScore = request.BreakfastScore,
                    FruitVegScore = request.FruitVegScore,
                    SweetSnacksScore = request.SweetSnacksScore,
                    FattyFoodsScore = request.FattyFoodsScore,
                    TotalScore = request.PhysicalActivityScore + request.BreakfastScore + request.FruitVegScore + request.SweetSnacksScore + request.FattyFoodsScore
                };
                _db.HealthScores.Add(entity);
                await _db.SaveChangesAsync(ct);
                return entity.Id;
            }
        }

        public async Task DeleteAsync(string userId, int id, bool isAdmin, CancellationToken ct = default)
        {
            var entity = await _db.HealthScores.FirstOrDefaultAsync(h => h.Id == id && !h.IsDeleted, ct);
            if (entity == null) return;

            if (!isAdmin)
            {
                var ownerOk = await _db.Children.AnyAsync(c => c.Id == entity.ChildId && c.UserId == userId, ct);
                if (!ownerOk) throw new UnauthorizedAccessException("Not allowed.");
            }

            entity.IsDeleted = true;
            await _db.SaveChangesAsync(ct);
        }
    }
}
