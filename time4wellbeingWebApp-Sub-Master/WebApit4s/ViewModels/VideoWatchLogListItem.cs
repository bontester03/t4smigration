namespace WebApit4s.ViewModels
{
    public class VideoWatchLogListItem
    {
        public Guid Id { get; set; }

        public int ChildId { get; set; }
        public string ChildName { get; set; }

        public int VideoRewardId { get; set; }
        public string VideoTitle { get; set; }

        public DateTime StartedUtc { get; set; }
        public DateTime? EndedUtc { get; set; }

        public double DurationSeconds { get; set; }
        public double VideoDurationSeconds { get; set; }
        public double MaxPositionSeconds { get; set; }
        public double PercentWatched { get; set; }

        public bool CoinsAwarded { get; set; }
        public DateTime? AwardedUtc { get; set; }

        public string ClientIp { get; set; }
        public string UserAgent { get; set; }
        public string DeviceId { get; set; }
    }
}
