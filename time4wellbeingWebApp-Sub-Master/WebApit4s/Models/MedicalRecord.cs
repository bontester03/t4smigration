using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace WebApit4s.Models
{
    public class MedicalRecord
    {
        public int Id { get; set; }

        [Required]
        public int ChildId { get; set; }

        [ValidateNever] // ✅ Ignore validation on navigation property
        public Child Child { get; set; } = null!;

        [Required(ErrorMessage = "GP Practice Name is required.")]
        [StringLength(100, ErrorMessage = "GP Practice Name cannot exceed 100 characters.")]
        public string GPPracticeName { get; set; } = null!;

        [Required(ErrorMessage = "GP Contact Number is required.")]
        [Phone(ErrorMessage = "Invalid phone number.")]
        public string GPContactNumber { get; set; } = null!;

        [StringLength(500, ErrorMessage = "Medical conditions cannot exceed 500 characters.")]
        public string? MedicalConditions { get; set; }

        [StringLength(300, ErrorMessage = "Allergies cannot exceed 300 characters.")]
        public string? Allergies { get; set; }

        [StringLength(300, ErrorMessage = "Medications cannot exceed 300 characters.")]
        public string? Medications { get; set; }

        [StringLength(500, ErrorMessage = "Additional notes cannot exceed 500 characters.")]
        public string? AdditionalNotes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsSensitive { get; set; } = false; // Optional GDPR tagging
    }
}
