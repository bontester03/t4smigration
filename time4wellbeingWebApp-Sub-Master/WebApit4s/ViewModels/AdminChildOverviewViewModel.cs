// ViewModels/AdminChildOverviewViewModel.cs
using System.Collections.Generic;
using WebApit4s.Models;

namespace WebApit4s.ViewModels
{
    public class AdminChildOverviewViewModel
    {
        public Child Child { get; set; }

        // Tasks
        public List<ChildGameTask> ActiveTasks { get; set; } = new();
        public List<ChildGameTask> CompletedOrExpiredTasks { get; set; } = new();

        // Points
        public List<UserPointHistory> PointHistory { get; set; } = new();

        // Parent rewards visible to this child (common + child-specific)
        public List<ParentReward> ParentRewards { get; set; } = new();

        // Redemptions by this child
        public List<ParentRewardRedemption> Redemptions { get; set; } = new();
    }
}
