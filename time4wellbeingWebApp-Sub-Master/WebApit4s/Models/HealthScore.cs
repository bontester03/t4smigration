using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WebApit4s.Identity;

namespace WebApit4s.Models
{
    public class HealthScore
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Physical Activity Score is required.")]
        [Range(0, 4, ErrorMessage = "Must Select from Dropdown Menu.")]
        public int PhysicalActivityScore { get; set; }

        [Required(ErrorMessage = "Breakfast Score is required.")]
        [Range(0, 4, ErrorMessage = "Must Select from Dropdown Menu.")]
        public int BreakfastScore { get; set; }

        [Required(ErrorMessage = "Fruit/Veg Score is required.")]
        [Range(0, 4, ErrorMessage = "Must Select from Dropdown Menu.")]
        public int FruitVegScore { get; set; }

        [Required(ErrorMessage = "Sweet Snacks Score is required.")]
        [Range(0, 4, ErrorMessage = "Must Select from Dropdown Menu.")]
        public int SweetSnacksScore { get; set; }

        [Required(ErrorMessage = "Fatty Foods Score is required.")]
        [Range(0, 4, ErrorMessage = "Must Select from Dropdown Menu.")]
        public int FattyFoodsScore { get; set; }

        public int? TotalScore { get; set; }

        public string? HealthClassification { get; set; }

        [Required(ErrorMessage = "Date Recorded is required.")]
        public DateTime DateRecorded { get; set; } = DateTime.Now;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // 🧭 Source tracking (who/what submitted the score)
        [StringLength(50)]
        public string? Source { get; set; } // e.g., "Parent", "Admin", "QuizEngine"

        // 🚫 Soft delete flag
        public bool IsDeleted { get; set; } = false;

        // 🏷️ Optional admin/moderator notes
        [StringLength(100)]
        public string? Notes { get; set; }

        // 🔗 Link to specific child (multi-child support)
       
        public int ChildId { get; set; }

        [BindNever]
        public Child Child { get; set; } = null!;

        [Obsolete("Use ChildId instead of UserId.")]
        public string? UserId { get; set; }
    }
}