using System.ComponentModel.DataAnnotations;

namespace WebApit4s.ViewModels.MultiStepVM
{
    /// <summary>
    /// Step 4 (Medical) - Captures medical details for the child at the same index
    /// as Step 2 (GuestStep2List) and Step 3 (GuestStep3List).
    /// </summary>
    public class GuestRegistrationStep4MedicalViewModel
    {
        [Required]
        public string Code { get; set; } = null!;

        /// <summary>
        /// The index of the child in the GuestStep2List (0-based).
        /// This keeps medical data aligned to the correct child during the wizard.
        /// </summary>
        [Required(ErrorMessage = "Child reference is required.")]
        [Range(0, int.MaxValue, ErrorMessage = "Invalid child reference.")]
        public int ChildIndex { get; set; }

        [Required(ErrorMessage = "GP Practice Name is required.")]
        [StringLength(100, ErrorMessage = "GP Practice Name cannot exceed 100 characters.")]
        [Display(Name = "GP Practice Name")]
        public string GPPracticeName { get; set; } = null!;

        [Required(ErrorMessage = "GP Contact Number is required.")]
        [Phone(ErrorMessage = "Invalid phone number.")]
        [StringLength(30, ErrorMessage = "Phone number cannot exceed 30 characters.")]
        [Display(Name = "GP Contact Number")]
        public string GPContactNumber { get; set; } = null!;

        [StringLength(500, ErrorMessage = "Medical conditions cannot exceed 500 characters.")]
        [Display(Name = "Medical Conditions (optional)")]
        public string? MedicalConditions { get; set; }

        [StringLength(300, ErrorMessage = "Allergies cannot exceed 300 characters.")]
        [Display(Name = "Allergies (optional)")]
        public string? Allergies { get; set; }

        [StringLength(300, ErrorMessage = "Medications cannot exceed 300 characters.")]
        [Display(Name = "Medications (optional)")]
        public string? Medications { get; set; }

        [StringLength(500, ErrorMessage = "Additional notes cannot exceed 500 characters.")]
        [Display(Name = "Additional Notes (optional)")]
        public string? AdditionalNotes { get; set; }

        /// <summary>
        /// Optional GDPR tagging for downstream processing.
        /// </summary>
        [Display(Name = "Contains sensitive medical information")]
        public bool IsSensitive { get; set; } = false;
    }
}
