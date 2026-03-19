namespace WebApit4s.Models
{
    public class VideoWatchLog
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public int ChildId { get; set; }
        public int VideoRewardId { get; set; }

        public DateTime StartedUtc { get; set; } = DateTime.UtcNow;
        public DateTime? EndedUtc { get; set; }

        public double DurationSeconds { get; set; }        // total play time tracked
        public double VideoDurationSeconds { get; set; }   // from player API
        public double MaxPositionSeconds { get; set; }     // furthest point reached
        public double PercentWatched { get; set; }         // computed at end

        public bool CoinsAwarded { get; set; }             // whether coins given for this session
        public DateTime? AwardedUtc { get; set; }

        // Optional telemetry
        public string? UserAgent { get; set; }
        public string? ClientIp { get; set; }
        public string? DeviceId { get; set; }
    }
}
