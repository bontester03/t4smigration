using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WebApit4s.Identity;

namespace WebApit4s.Models
{
    public class WeeklyMeasurements
    {
        public int Id { get; set; }

        // ✅ Foreign Key: Always required for new entries
        
        public int ChildId { get; set; }

        [BindNever]
        public Child Child { get; set; } = null!;


        [Obsolete("Use ChildId instead of UserId.")]
        public string? UserId { get; set; }
        public ApplicationUser? User { get; set; }

        [Required(ErrorMessage = "Age is required.")]
        [Range(1, 100)]
        public int Age { get; set; }

        [Required(ErrorMessage = "Height is required.")]
        [Range(10, 300)]
        public decimal Height { get; set; }

        [Required(ErrorMessage = "Weight is required.")]
        [Range(1, 150)]
        public decimal Weight { get; set; }

        [Required]
        public int CentileScore { get; set; }

        // ✅ Auto-computed range
        [NotMapped]
        public string HealthRange =>
            CentileScore < 2 ? "Underweight" :
            CentileScore <= 90 ? "Healthy Weight" : "Overweight";

        // ✅ Timestamps for audit and trends
        [Required]
        public DateTime DateRecorded { get; set; } = DateTime.UtcNow;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // ✅ Optional: To trace who entered it — parent, admin, etc.
        [StringLength(50)]
        public string? Source { get; set; }

        // ✅ Optional: Notes from admin/user for special context
        [StringLength(300)]
        public string? Notes { get; set; }

        // ✅ Soft delete support
        public bool IsDeleted { get; set; } = false;
    }
}
