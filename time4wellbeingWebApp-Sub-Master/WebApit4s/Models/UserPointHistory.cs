using WebApit4s.Identity;

namespace WebApit4s.Models
{
    public class UserPointHistory
    {

        public int Id { get; set; }
        public string UserId { get; set; } = null!;
        public ApplicationUser User { get; set; } = null!;
        public int? ChildId { get; set; }
        public Child? Child { get; set; }
        public int Delta { get; set; }              // +earned, -spent
        public int BalanceAfter { get; set; }       // snapshot
        public PointChangeReason Reason { get; set; }
        public string? SourceType { get; set; }     // "VideoReward"
        public int? SourceId { get; set; }          // VideoReward.Id
        public string? Notes { get; set; }
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    }
    public enum PointChangeReason { VideoWatched, QuizCompleted, GamePlayed, TaskCompleted, RewardRequest, RewardApproved, RewardInstant, RewardRejectedRefund, AdminAdjustment }

}
