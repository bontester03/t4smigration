using WebApit4s.Models;

namespace WebApit4s.ViewModels
{
    public class AvailableRewardsViewModel
    {
        public int ChildId { get; set; }

        public string ChildName { get; set; } = "";   // <-- add this
        public int ChildCoins { get; set; }
        public List<ParentReward> Rewards { get; set; } = new();
    }
}
