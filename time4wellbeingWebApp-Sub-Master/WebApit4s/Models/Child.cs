using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WebApit4s.Identity;

namespace WebApit4s.Models
{
    public class Child
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }

        [Required(ErrorMessage = "Child's name is required.")]
        [StringLength(100)]
        public string ChildName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Gender is required.")]
        public Gender Gender { get; set; }

        [Required(ErrorMessage = "Date of birth is required.")]
        [CustomValidation(typeof(Child), nameof(ValidateAge))]
        public DateTime DateOfBirth { get; set; }

        // Gamification-ready fields
        public int TotalPoints { get; set; } = 0;
        public int Level { get; set; } = 1;

        public DateTime? LastLogin { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; } = false;

        [Required]
        public Guid ChildGuid { get; set; } = Guid.NewGuid();
        public string? AvatarUrl { get; set; }

        [Required]
        public EngagementStatus EngagementStatus { get; set; } = EngagementStatus.Engaged;

        [NotMapped]
        public int Age => DateTime.Today.Year - DateOfBirth.Year -
                     (DateOfBirth.Date > DateTime.Today.AddYears(-(DateTime.Today.Year - DateOfBirth.Year)) ? 1 : 0);


        // ✅ Navigation collections for EF
        public ICollection<HealthScore> HealthScores { get; set; } = new List<HealthScore>();
        public ICollection<WeeklyMeasurements> WeeklyMeasurements { get; set; } = new List<WeeklyMeasurements>();
        public ICollection<AdminNote> AdminNotes { get; set; } = new List<AdminNote>();
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();

        public ICollection<MedicalRecord> MedicalRecords { get; set; } = new List<MedicalRecord>();
        public static ValidationResult? ValidateAge(DateTime dob, ValidationContext context)
        {
            var age = DateTime.Now.Year - dob.Year;
            if (dob > DateTime.Now.AddYears(-age)) age--;

            return age >= 2 && age <= 17
                ? ValidationResult.Success
                : new ValidationResult("Child's age must be between 2 and 17 years.");
        }

        [StringLength(100)]
        public string? School { get; set; }

        [StringLength(50)]
        public string? Class { get; set; }

    }
    public enum EngagementStatus
    {
        Engaged = 0,
        Withdrawn = 1,
        Ineligible = 2,
        Uncontactable = 3
    }

    public enum Gender
    {
        Male,
        Female,
        Other
    }
}
