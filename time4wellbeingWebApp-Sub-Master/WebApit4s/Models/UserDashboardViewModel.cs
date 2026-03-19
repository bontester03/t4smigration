using WebApit4s.ViewModels;

namespace WebApit4s.Models
{
    public class UserDashboardViewModel
    {

        public string UserName { get; set; }
        public DateTime RegistrationDate { get; set; }
        public List<Child> Children { get; set; }
        public List<WeeklyMeasurements> Measurements { get; set; }
        public List<HealthScore> HealthScores { get; set; } // ✅ Replaced `Questionnaires`
        public List<TimelineActivityViewModel> RecentActivities { get; set; }
        public List<Notification> Notifications { get; set; } // ✅ Added this for notifications

        public AvailableRewardsViewModel AvailableRewards { get; set; }

        public int ActiveTasksCount { get; set; }
    }
}
