using System;
using System.ComponentModel.DataAnnotations;
using WebApit4s.Identity;

namespace WebApit4s.Models
{
    public class Notification
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = null!; // Identity User ID
        public ApplicationUser? User { get; set; }

        // ✅ Optional: Attach to a specific child
        public int? ChildId { get; set; }
        public Child? Child { get; set; }

        [Required(ErrorMessage = "Notification message is required.")]
        [StringLength(500, ErrorMessage = "Message cannot exceed 500 characters.")]
        public string Message { get; set; } = null!;

        public bool IsRead { get; set; } = false;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // ✅ Optional: Type or category of notification (for filtering)
        [StringLength(50)]
        public string? Type { get; set; } // e.g. "Reminder", "HealthScore", "Quiz", "Task"

        // ✅ Optional: For system-wide broadcast/alert logic
        public bool IsSystemWide { get; set; } = false;
    }
}
