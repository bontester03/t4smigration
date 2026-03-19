using System.ComponentModel.DataAnnotations;

namespace WebApit4s.ViewModels.MultiStepVM
{
    public class GuestRegistrationStep1ViewModel
    {
        public string Code { get; set; }

        [Required(ErrorMessage = "Parent/Guardian name is required")]
        [Display(Name = "Parent / Guardian Name")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
        public string ParentName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        public string Email { get; set; }

        // These are usually set from the school link context (optional on the form)
        public string? School { get; set; }
        public string? Class { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [Phone(ErrorMessage = "Please enter a valid phone number")]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Postcode is required")]
        [StringLength(10, ErrorMessage = "Postcode cannot exceed 10 characters.")]
        public string Postcode { get; set; }

        [Required(ErrorMessage = "Please select your relationship to the child")]
        [Display(Name = "Relationship to Child")]
        public ParentRelationship? Relationship { get; set; }

        // ✅ Simple helper for saving into PersonalDetails.RelationshipToChild
        // If user selects Other -> "Other" will be saved.
        public string RelationshipToChildString => Relationship?.ToString() ?? string.Empty;
    }
}
