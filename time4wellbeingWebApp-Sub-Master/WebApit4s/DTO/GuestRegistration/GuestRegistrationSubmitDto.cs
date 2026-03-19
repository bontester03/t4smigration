using System.ComponentModel.DataAnnotations;
using WebApit4s.Models;

namespace WebApit4s.DTO.GuestRegistration
{
    public class GuestRegistrationSubmitDto
    {
        [Required]
        public string Code { get; set; } = string.Empty;

        [Required]
        public GuestParentDto Parent { get; set; } = new();

        [MinLength(1)]
        public List<GuestChildDto> Children { get; set; } = new();

        public List<GuestConsentAnswerDto> ConsentAnswers { get; set; } = new();
    }

    public class GuestParentDto
    {
        [Required]
        public string ParentName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        public string Postcode { get; set; } = string.Empty;

        [Required]
        public string Relationship { get; set; } = string.Empty;
    }

    public class GuestChildDto
    {
        [Required]
        public string ChildName { get; set; } = string.Empty;

        [Required]
        public DateTime DateOfBirthUtc { get; set; }

        [Required]
        public Gender Gender { get; set; }

        public GuestChildScoreDto? Score { get; set; }
        public GuestChildMedicalDto? Medical { get; set; }
    }

    public class GuestChildScoreDto
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

    public class GuestChildMedicalDto
    {
        [Required]
        public string GPPracticeName { get; set; } = string.Empty;

        [Required]
        public string GPContactNumber { get; set; } = string.Empty;

        public string? MedicalConditions { get; set; }
        public string? Allergies { get; set; }
        public string? Medications { get; set; }
        public string? AdditionalNotes { get; set; }
        public bool IsSensitive { get; set; }
    }

    public class GuestConsentAnswerDto
    {
        public int ConsentQuestionId { get; set; }
        public string Answer { get; set; } = string.Empty;
    }
}
