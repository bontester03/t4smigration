using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WebApit4s.Identity; // <-- Ensure this points to where ApplicationUser is

namespace WebApit4s.Models
{
    public class RegistrationReminder
    {
        public int Id { get; set; }

        // ✅ Use Identity UserId (string)
        [Required]
        public string UserId { get; set; } = string.Empty;

        public DateTime SentAt { get; set; }

        // ✅ Navigation property to ApplicationUser
        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; } 

       



    }
}
