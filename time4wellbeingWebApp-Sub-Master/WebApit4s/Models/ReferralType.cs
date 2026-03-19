using System.ComponentModel.DataAnnotations;
using WebApit4s.Identity; // Make sure this is the correct namespace for ApplicationUser

namespace WebApit4s.Models
{
    public class ReferralType
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Referral name is required.")]
        [StringLength(100, ErrorMessage = "Referral name cannot exceed 100 characters.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Referral category is required.")]
        public ReferralCategory Category { get; set; }

        // ✅ Determines if school dropdowns should appear on registration
        public bool RequiresSchoolSelection { get; set; } = false;

        // ✅ Determines if the type is currently in use
        public bool IsActive { get; set; } = true;

        // ✅ Optional: Used for analytics/tracking
        public int UsageCount { get; set; } = 0;

        // ✅ Navigation property to all users using this referral type
        public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
    }

    public enum ReferralCategory
    {
        SelfReferral = 0,
        School = 1,
        Nurse = 2,
        NCMP = 3,
        GPReferral = 4,
        CommunityProgram = 5,
        Other = 99
    }
}
