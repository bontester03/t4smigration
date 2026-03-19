using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WebApit4s.DAL;
using WebApit4s.DTO.Measurements;

namespace WebApit4s.Services
{
    public sealed class MeasurementService : WebApit4s.Services.Interfaces.IMeasurementService
    {
        private readonly TimeContext _db;

        public MeasurementService(TimeContext db) => _db = db;

        private async Task<int?> ResolveChildIdAsync(string userId, int? activeChildId, bool isAdmin, CancellationToken ct)
        {
            if (activeChildId.HasValue)
            {
                var ok = await _db.Children
                    .AnyAsync(c => c.Id == activeChildId.Value && !c.IsDeleted && (isAdmin || c.UserId == userId), ct);
                return ok ? activeChildId : null;
            }

            var childId = await _db.Children
                .Where(c => !c.IsDeleted && (isAdmin || c.UserId == userId))
                .OrderBy(c => c.DateOfBirth)
                .Select(c => (int?)c.Id)
                .FirstOrDefaultAsync(ct);

            return childId;
        }

        private static decimal? ComputeBmi(decimal heightCm, decimal weightKg)
        {
            if (heightCm <= 0 || weightKg <= 0) return null;
            var hM = (double)heightCm / 100.0;
            var bmi = (double)weightKg / (hM * hM);
            return Math.Round((decimal)bmi, 1);
        }

        public async Task<IReadOnlyList<MeasurementDto>> GetListAsync(string userId, int? activeChildId, bool isAdmin, int take = 25, int skip = 0, CancellationToken ct = default)
        {
            var childId = await ResolveChildIdAsync(userId, activeChildId, isAdmin, ct);
            if (!childId.HasValue) return Array.Empty<MeasurementDto>();

            var rows = await _db.WeeklyMeasurements
                .AsNoTracking()
                .Where(w => w.ChildId == childId && !w.IsDeleted)
                .OrderByDescending(w => w.DateRecorded)
                .Skip(skip)
                .Take(take)
                .Select(w => new
                {
                    w.Id,
                    w.ChildId,
                    w.DateRecorded,
                    w.Height,
                    w.Weight,
                    w.HealthRange
                })
                .ToListAsync(ct);

            return rows.Select(w => new MeasurementDto
            {
                Id = w.Id,
                ChildId = w.ChildId,
                DateRecorded = w.DateRecorded,
                Height = w.Height,
                Weight = w.Weight,
                BMI = ComputeBmi(w.Height, w.Weight),
                HealthRange = w.HealthRange
            }).ToList();
        }

        public async Task<MeasurementDto?> GetLatestAsync(string userId, int? activeChildId, bool isAdmin, CancellationToken ct = default)
        {
            var childId = await ResolveChildIdAsync(userId, activeChildId, isAdmin, ct);
            if (!childId.HasValue) return null;

            var w = await _db.WeeklyMeasurements
                .AsNoTracking()
                .Where(x => x.ChildId == childId && !x.IsDeleted)
                .OrderByDescending(x => x.DateRecorded)
                .FirstOrDefaultAsync(ct);

            if (w is null) return null;

            return new MeasurementDto
            {
                Id = w.Id,
                ChildId = w.ChildId,
                DateRecorded = w.DateRecorded,
                Height = w.Height,
                Weight = w.Weight,
                BMI = ComputeBmi(w.Height, w.Weight),
                HealthRange = w.HealthRange
            };
        }

        public async Task<int> UpsertAsync(string userId, UpsertMeasurementRequest request, int? activeChildId, bool isAdmin, CancellationToken ct = default)
        {
            var childId = request.ChildId ?? await ResolveChildIdAsync(userId, activeChildId, isAdmin, ct);
            if (!childId.HasValue) throw new KeyNotFoundException("Child not found or not accessible.");

            if (request.Id.HasValue)
            {
                var entity = await _db.WeeklyMeasurements
                    .FirstOrDefaultAsync(w => w.Id == request.Id.Value && !w.IsDeleted, ct);
                if (entity == null) throw new KeyNotFoundException("Measurement not found.");

                if (!isAdmin)
                {
                    var ownerOk = await _db.Children.AnyAsync(c => c.Id == entity.ChildId && c.UserId == userId, ct);
                    if (!ownerOk) throw new UnauthorizedAccessException("Not allowed.");
                }

                entity.ChildId = childId.Value;
                entity.DateRecorded = request.DateRecorded;
                entity.Height = request.Height;
                entity.Weight = request.Weight;

                await _db.SaveChangesAsync(ct);
                return entity.Id;
            }
            else
            {
                var entity = new Models.WeeklyMeasurements
                {
                    ChildId = childId.Value,
                    DateRecorded = request.DateRecorded,
                    Height = request.Height,
                    Weight = request.Weight
                };
                _db.WeeklyMeasurements.Add(entity);
                await _db.SaveChangesAsync(ct);
                return entity.Id;
            }
        }

        public async Task DeleteAsync(string userId, int id, bool isAdmin, CancellationToken ct = default)
        {
            var entity = await _db.WeeklyMeasurements.FirstOrDefaultAsync(w => w.Id == id && !w.IsDeleted, ct);
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
