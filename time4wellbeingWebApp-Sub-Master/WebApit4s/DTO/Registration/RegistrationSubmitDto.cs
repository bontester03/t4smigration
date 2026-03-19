using System.ComponentModel.DataAnnotations;
using WebApit4s.Models;

namespace WebApit4s.DTO.Registration
{
    public class RegistrationSubmitDto
    {
        [Required]
        public RegistrationAccountDto Account { get; set; } = new();

        [Required]
        public RegistrationParentDto Parent { get; set; } = new();

        [Required]
        public RegistrationChildDto Child { get; set; } = new();

        [Required]
        public RegistrationMedicalDto Medical { get; set; } = new();

        [Required]
        public RegistrationHealthScoreDto HealthScore { get; set; } = new();
    }

    public class RegistrationAccountDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

        [Range(1, int.MaxValue)]
        public int ReferralTypeId { get; set; }
    }

    public class RegistrationParentDto
    {
        [Required]
        [StringLength(100)]
        public string ParentGuardianName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string RelationshipToChild { get; set; } = string.Empty;

        [Required]
        [Phone]
        public string TeleNumber { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(10)]
        public string Postcode { get; set; } = string.Empty;
    }

    public class RegistrationChildDto
    {
        [Required]
        [StringLength(100)]
        public string ChildName { get; set; } = string.Empty;

        [Required]
        public Gender Gender { get; set; }

        [Required]
        public DateTime DateOfBirthUtc { get; set; }

        [StringLength(100)]
        public string? School { get; set; }

        [StringLength(50)]
        public string? Class { get; set; }

        [Required]
        [StringLength(255)]
        public string AvatarUrl { get; set; } = string.Empty;
    }

    public class RegistrationMedicalDto
    {
        [Required]
        [StringLength(100)]
        public string GPPracticeName { get; set; } = string.Empty;

        [Required]
        [Phone]
        public string GPContactNumber { get; set; } = string.Empty;

        [StringLength(500)]
        public string? MedicalConditions { get; set; }

        [StringLength(300)]
        public string? Allergies { get; set; }

        [StringLength(300)]
        public string? Medications { get; set; }

        [StringLength(500)]
        public string? AdditionalNotes { get; set; }
    }

    public class RegistrationHealthScoreDto
    {
        [Range(0, 4)]
        public int PhysicalActivityScore { get; set; }

        [Range(0, 4)]
        public int BreakfastScore { get; set; }

        [Range(0, 4)]
        public int FruitVegScore { get; set; }

        [Range(0, 4)]
        public int SweetSnacksScore { get; set; }

        [Range(0, 4)]
        public int FattyFoodsScore { get; set; }
    }
}
