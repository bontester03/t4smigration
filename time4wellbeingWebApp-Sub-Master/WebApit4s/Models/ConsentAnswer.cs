using System.ComponentModel.DataAnnotations;
using WebApit4s.Identity; // Make sure this is your Identity namespace

namespace WebApit4s.Models
{
    public class ConsentAnswer
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = null!; // Updated from int to string

        [Required]
        public int ConsentQuestionId { get; set; }

        [Required]
        [RegularExpression("Yes|No", ErrorMessage = "Answer must be 'Yes' or 'No'.")]
        public string Answer { get; set; } = "No"; // Default to "No" for safety

        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ConsentQuestion ConsentQuestion { get; set; } = null!;
        public ApplicationUser? User { get; set; }

      

    }
}
