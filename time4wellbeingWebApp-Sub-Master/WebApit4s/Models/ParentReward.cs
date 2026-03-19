using System.ComponentModel.DataAnnotations;

namespace WebApit4s.Models
{
    public class ParentReward : IValidatableObject
    {
        public int Id { get; set; }

        [Required]
        public string ParentUserId { get; set; }     // set server-side from logged-in user

        [Required, StringLength(80)]
        public string Title { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        [Range(1, 100000, ErrorMessage = "Coin cost must be at least 1.")]
        public int CoinCost { get; set; }

        public bool IsActive { get; set; } = true;

        // NEW
        public bool IsCommon { get; set; } = false;  // true => visible to all children of this parent
        public int? ChildId { get; set; }

        public bool RequiresParentApproval { get; set; } = true;

        public DateTime? ValidFromUtc { get; set; }
        public DateTime? ValidToUtc { get; set; }

        [Range(0, 3650, ErrorMessage = "Cooldown must be between 0 and 3650 days.")]
        public int? CooldownDaysPerChild { get; set; }

        // Cross-field validation
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (ValidFromUtc.HasValue && ValidToUtc.HasValue && ValidFromUtc > ValidToUtc)
            {
                yield return new ValidationResult(
                    "Valid From must be before Valid To.",
                    new[] { nameof(ValidFromUtc), nameof(ValidToUtc) });
            }
        }
        public enum ParentRewardRedemptionStatus
        {
            Requested,
            Approved,
            Redeemed,
            Rejected,
            Cancelled
        }
    }
}
 