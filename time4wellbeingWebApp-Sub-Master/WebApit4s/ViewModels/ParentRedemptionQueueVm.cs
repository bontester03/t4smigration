using WebApit4s.Models;


namespace WebApit4s.ViewModels
{
    public class ParentRedemptionQueueVm
    {
        public List<ParentRewardRedemption> Requested { get; set; } = new();
        public List<ParentRewardRedemption> Approved { get; set; } = new();

        public string? ChildName { get; set; }
    }
}
