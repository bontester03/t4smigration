using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebApit4s.DAL;
using WebApit4s.DTO.Dashboard;
using WebApit4s.Identity;
using WebApit4s.Models;
using WebApit4s.TagHelpers;

namespace WebApit4s.Services;

public interface IDashboardService
{
    Task<DashboardResponse> BuildDashboardAsync(
        string userId,
        DashboardRequest request,
        CancellationToken ct = default);
}

public class DashboardService : IDashboardService
{
    private readonly IHttpContextAccessor _http;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly TimeContext _context;

    public DashboardService(
        IHttpContextAccessor httpContextAccessor,
        UserManager<ApplicationUser> userManager,
        TimeContext context)
    {
        _http = httpContextAccessor;
        _userManager = userManager;
        _context = context;
    }

    public async Task<DashboardResponse> BuildDashboardAsync(  // ✅ Changed name
    string userId,  // ✅ Added parameter
    DashboardRequest request,
    CancellationToken ct = default)
    {
        // ✅ userId comes as parameter - no authentication code needed
        Console.WriteLine($"✅ DashboardService received userId: {userId}");

        // Rest of your code...
        var children = await _context.Children
            .AsNoTracking()
            .Where(c => c.UserId == userId && !c.IsDeleted && c.EngagementStatus == EngagementStatus.Engaged)
            .OrderBy(c => c.CreatedAt)
            .Select(c => new ChildSummaryDto
            {
                Id = c.Id,
                FullName = c.ChildName,
                AgeYears = DateTime.UtcNow.Year - c.DateOfBirth.Year,
                AvatarUrl = c.AvatarUrl,
                 TotalPoints = c.TotalPoints,  // ✅ ADD THIS
                Level = c.Level
            })
            .ToListAsync(ct);

        // ✅  Process avatar URLs for all children
        Console.WriteLine($"📸 Processing avatar URLs for {children.Count} children");

        foreach (var child in children)
        {
            var originalUrl = child.AvatarUrl;
            child.AvatarUrl = UrlHelper.GetAvatarUrl(child.AvatarUrl, _http.HttpContext?.Request);
            Console.WriteLine($"🖼️ Child {child.Id} avatar: '{originalUrl}' -> '{child.AvatarUrl}'");
        }
        // --- resolve active child ---
        // --- resolve active child ---
        int? activeChildId = request.ActiveChildId;

        if (activeChildId.HasValue && !children.Any(ch => ch.Id == activeChildId.Value))
            activeChildId = null;

        // Default to the OLDEST child when none specified
        // Prefer using DOB in SQL; falling back to AgeYears if that's all you have
        var defaultId = children
            .OrderByDescending(c => c.AgeYears)   // or .OrderBy(c => c.DateOfBirth) for better accuracy
            .ThenBy(c => c.Id)
            .Select(c => c.Id)
            .FirstOrDefault();                    // returns 0 if list empty

        if (!activeChildId.HasValue && defaultId != 0)
            activeChildId = defaultId;

        // now activeChildId is either a real child id or remains null if there truly are no children


        // OPTIONAL: If AgeYears is not accurate, use DateOfBirth instead (better method):
        /*
        activeChildId ??= _context.Children
            .Where(c => c.UserId == userId && !c.IsDeleted && c.EngagementStatus == EngagementStatus.Engaged)
            .OrderBy(c => c.DateOfBirth)   // oldest = smallest DOB
            .Select(c => c.Id)
            .FirstOrDefault();
        */


        // --- totals across parent’s engaged children ---
        var childIds = children.Select(c => c.Id).ToArray();

        var totalMeasurements = await _context.WeeklyMeasurements
            .AsNoTracking()
            .Where(w => childIds.Contains(w.ChildId) && !w.IsDeleted)
            .CountAsync(ct);

        var totalHealthScores = await _context.HealthScores
            .AsNoTracking()
            .Where(h => childIds.Contains(h.ChildId) && !h.IsDeleted)
            .CountAsync(ct);

        var totalUnreadNotifications = await _context.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId && !n.IsRead)
            .CountAsync(ct);

        var totalActiveTasks = 0; // TODO when you share task model

        // --- latest measurement for active child ---
        MeasurementDto? latestMeasurement = null;
        DateTime? nextDueUtc = null;

        if (activeChildId.HasValue)
        {
            var m = await _context.WeeklyMeasurements
                .AsNoTracking()
                .Where(w => w.ChildId == activeChildId.Value && !w.IsDeleted)
                .OrderByDescending(w => w.DateRecorded)
                .FirstOrDefaultAsync(ct);

            if (m is not null)
            {
                var heightCm = m.Height;  // cm
                var weightKg = m.Weight;  // kg
                var heightM = (double)heightCm / 100d;
                decimal? bmi = null;
                if (heightM > 0.0)
                    bmi = (decimal)Math.Round(((double)weightKg) / (heightM * heightM), 1);

                latestMeasurement = new MeasurementDto
                {
                    Id = m.Id,
                    RecordedOnUtc = DateTime.SpecifyKind(m.DateRecorded, DateTimeKind.Utc), // adjust after fetch
                    HeightCm = heightCm,
                    HeightIn = Math.Round(heightCm / 2.54m, 1),
                    WeightKg = weightKg,
                    WeightLbs = Math.Round(weightKg * 2.20462m, 1),
                    BMI = bmi,
                    CentileBand = m.HealthRange
                };

                nextDueUtc = m.DateRecorded.AddDays(7);
            }
        }

        // --- latest health score for active child ---
        HealthScoreDto? latestHealthScore = null;

        if (activeChildId.HasValue)
        {
            var hs = await _context.HealthScores
                .AsNoTracking()
                .Where(h => h.ChildId == activeChildId.Value && !h.IsDeleted)
                .OrderByDescending(h => h.DateRecorded)
                .FirstOrDefaultAsync(ct);

            if (hs is not null)
            {
                var total = hs.TotalScore ?? (hs.PhysicalActivityScore + hs.BreakfastScore + hs.FruitVegScore + hs.SweetSnacksScore + hs.FattyFoodsScore);
                var overall = Math.Round((decimal)total / 2m, 1); // 0–20 -> 0–10

                latestHealthScore = new HealthScoreDto
                {
                    Id = hs.Id,
                    SubmittedOnUtc = DateTime.SpecifyKind(hs.DateRecorded, DateTimeKind.Utc),
                    PhysicalActivityScore = hs.PhysicalActivityScore,
                    BreakfastScore = hs.BreakfastScore,
                    FruitVegScore = hs.FruitVegScore,
                    SweetSnacksScore = hs.SweetSnacksScore,
                    FattyFoodsScore = hs.FattyFoodsScore,
                    TotalScore = hs.TotalScore
                };
            }
        }

        // --- recent notifications (user-wide) ---
        var notificationsRows = await _context.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(Math.Max(1, request.NotificationsTake))
            .Select(n => new { n.Id, n.Type, n.Message, n.IsRead, n.CreatedAt })
            .ToListAsync(ct);

        var notifications = notificationsRows
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                Title = n.Type ?? "Notification",
                Body = n.Message,
                IsRead = n.IsRead,
                CreatedUtc = DateTime.SpecifyKind(n.CreatedAt, DateTimeKind.Utc)
            })
            .ToList();

        // --- parent summary (fetch simple, then map) ---
        var rawParent = await _context.Users
            .Where(u => u.Id == userId)
            .Select(u => new
            {
                u.Email,
                FullName = u.PersonalDetails != null ? u.PersonalDetails.ParentGuardianName : u.UserName
            })
            .FirstAsync(ct);

        var parent = new ParentSummaryDto
        {
            UserId = 0, // string key in Identity; keep 0 or add your own int surrogate if needed
            Email = rawParent.Email ?? "",
            FirstName = rawParent.FullName?.Split(' ').FirstOrDefault() ?? "Parent",
            LastName = rawParent.FullName?.Split(' ').Skip(1).FirstOrDefault()
        };

        // --- return (RecentActivities removed) ---
        return new DashboardResponse
        {
            ServerTimeUtc = DateTime.UtcNow,
            Parent = parent,
            ActiveChildId = activeChildId,
            Children = children,
            TotalChildren = children.Count,
            TotalMeasurements = totalMeasurements,
            TotalHealthScores = totalHealthScores,
            TotalUnreadNotifications = totalUnreadNotifications,
            TotalActiveTasks = totalActiveTasks,
            LatestMeasurement = latestMeasurement,
            LatestHealthScore = latestHealthScore,
            NextMeasurementDueUtc = nextDueUtc,
            RecentNotifications = notifications,
            RecentActivities = new List<ActivityDto>() // intentionally empty
        };
    }
}
