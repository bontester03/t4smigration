namespace WebApit4s.Models
{
    public class AdminDashboardViewModel
    {
        public int UserCount { get; set; }
        public int TotalWeightLogs { get; set; }
        public int TotalHealthScores { get; set; }
        public int TotalChildren { get; set; }
        public List<TimelineActivityViewModel> RecentActivities { get; set; } = new List<TimelineActivityViewModel>();
    }

}
