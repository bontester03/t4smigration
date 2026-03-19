using System.ComponentModel.DataAnnotations;
using WebApit4s.Identity;

namespace WebApit4s.Models
{
    public class PersonalDetails
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Parent or guardian name is required.")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
        public string ParentGuardianName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Relationship to child is required.")]
        [StringLength(50, ErrorMessage = "Relationship description cannot exceed 50 characters.")]
        public string RelationshipToChild { get; set; } = string.Empty;

        [Required(ErrorMessage = "Telephone number is required.")]
        [Phone(ErrorMessage = "Enter a valid phone number.")]
        public string TeleNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Postcode is required.")]
        [StringLength(10, ErrorMessage = "Postcode cannot exceed 10 characters.")]
        public string Postcode { get; set; } = string.Empty;
      
               
        public string UserId { get; set; } = null!;

        public ApplicationUser? User { get; set; }

       


    }
}
