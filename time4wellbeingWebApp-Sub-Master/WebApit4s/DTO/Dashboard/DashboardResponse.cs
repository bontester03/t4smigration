namespace WebApit4s.DTO.Dashboard
{
    public class DashboardResponse
    {
        // Timing
        public DateTime ServerTimeUtc { get; set; }

        // Parent summary
        public ParentSummaryDto Parent { get; set; } = new();

        // Child context + lists
        public int? ActiveChildId { get; set; }
        public List<ChildSummaryDto> Children { get; set; } = new();

        // Totals (user-side)
        public int TotalChildren { get; set; }
        public int TotalMeasurements { get; set; }
        public int TotalHealthScores { get; set; }
        public int TotalUnreadNotifications { get; set; }
        public int TotalActiveTasks { get; set; }

        // Latest data for ActiveChild
        public MeasurementDto? LatestMeasurement { get; set; }
        public HealthScoreDto? LatestHealthScore { get; set; }

        // Upcoming / due
        public DateTime? NextMeasurementDueUtc { get; set; }

        // Recent lists
        public List<NotificationDto> RecentNotifications { get; set; } = new();
        public List<ActivityDto> RecentActivities { get; set; } = new();
    }

    public class ParentSummaryDto
    {
        public int UserId { get; set; }
        public string FirstName { get; set; } = "";
        public string? LastName { get; set; }
        public string Email { get; set; } = "";
    }

    public class ChildSummaryDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = "";
        public int AgeYears { get; set; }
        public string? AvatarUrl { get; set; }

        public int TotalPoints { get; set; }  // Coins/points for gamification
        public int Level { get; set; }
    }

    public class MeasurementDto
    {
        public int Id { get; set; }
        public DateTime RecordedOnUtc { get; set; }

        // Store canonical metric values and optionally the imperial for convenience
        public decimal WeightKg { get; set; }
        public decimal? WeightLbs { get; set; }

        public decimal HeightCm { get; set; }
        public decimal? HeightIn { get; set; }

        // Optional derived values
        public decimal? BMI { get; set; }
        public string? CentileBand { get; set; } // e.g., "Healthy", "Overweight"
    }

    public class HealthScoreDto
    {
        public int Id { get; set; }
        public DateTime SubmittedOnUtc { get; set; }

        // Example component scores (0–10)
        public int PhysicalActivityScore { get; set; }
        public int BreakfastScore { get; set; }
        public int FruitVegScore { get; set; }
        public int SweetSnacksScore { get; set; }
        public int FattyFoodsScore { get; set; }

        // Convenience overall (0–10)
        public int? TotalScore { get; set; }
    }

    public class NotificationDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string? Body { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedUtc { get; set; }
    }

    public class ActivityDto
    {
        public int Id { get; set; }
        public string Description { get; set; } = ""; // e.g., "Logged weight 32.5 lbs"
        public DateTime OccurredUtc { get; set; }
        public string? Category { get; set; } // e.g., "Measurement", "HealthScore", "Task"
    }
}
