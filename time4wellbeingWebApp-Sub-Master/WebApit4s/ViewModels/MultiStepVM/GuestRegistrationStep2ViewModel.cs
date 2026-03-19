using System.ComponentModel.DataAnnotations;
using WebApit4s.Models;

namespace WebApit4s.ViewModels.MultiStepVM
{
    public class GuestRegistrationStep2ViewModel
    {
        public string Code { get; set; }

        [Required(ErrorMessage = "Child's name is required")]
        [Display(Name = "Child's Full Name")]
        public string ChildName { get; set; }

        [Required(ErrorMessage = "Date of birth is required")]
        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        [CustomValidation(typeof(GuestRegistrationStep2ViewModel), nameof(ValidateAge))]
        public DateTime? DateOfBirth { get; set; } // nullable for validation

        [Required(ErrorMessage = "Please select a gender")]
        [Display(Name = "Gender")]
        public Gender? Gender { get; set; } // nullable so [Required] works

        // Custom validation method
        public static ValidationResult? ValidateAge(object value, ValidationContext context)
        {
            if (value == null)
            {
                return new ValidationResult("Date of birth is required.");
            }

            if (value is DateTime dob)
            {
                var today = DateTime.Today;
                var age = today.Year - dob.Year;

                if (dob.Date > today.AddYears(-age)) age--;

                return age >= 2 && age <= 17
                    ? ValidationResult.Success
                    : new ValidationResult("Child's age must be between 2 and 17 years.");
            }

            return new ValidationResult("Invalid date format.");
        }
    }
}
