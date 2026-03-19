using System.ComponentModel.DataAnnotations;
using static WebApit4s.Models.ParentReward;

namespace WebApit4s.Models
{
    public class ParentRewardRedemption :IValidatableObject
    {
        public int Id { get; set; }

        [Required]
        public int ChildId { get; set; }
        public Child Child { get; set; } // ✅ Navigation property

        [Required]
        public int ParentRewardId { get; set; }

        // 🔹 Navigation property to ParentReward
        public ParentReward ParentReward { get; set; }

        [Range(1, 100000)]
        public int CoinCostAtPurchase { get; set; }

        [Required]
        public DateTime RequestedUtc { get; set; }

        [Required]
        public ParentRewardRedemptionStatus Status { get; set; }

        public DateTime? ApprovedUtc { get; set; }
        public DateTime? RedeemedUtc { get; set; }

        [StringLength(500)]
        public string Notes { get; set; }

        // ✅ NEW: track if coins were deducted (at request or at approval)
        public bool CoinsDeducted { get; set; }

        // Basic state consistency checks
        public IEnumerable<ValidationResult> Validate(ValidationContext context)
        {
            if (ApprovedUtc.HasValue && ApprovedUtc < RequestedUtc)
            {
                yield return new ValidationResult(
                    "Approved date cannot be earlier than requested date.",
                    new[] { nameof(ApprovedUtc), nameof(RequestedUtc) });
            }

            if (RedeemedUtc.HasValue)
            {
                if (!ApprovedUtc.HasValue || RedeemedUtc < ApprovedUtc)
                {
                    yield return new ValidationResult(
                        "Redeemed date must be after approval.",
                        new[] { nameof(RedeemedUtc), nameof(ApprovedUtc) });
                }
            }
        }
    }
}
