using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WebApit4s.Identity;

namespace WebApit4s.Models
{
    public class GameTask
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required.")]
        [StringLength(100, ErrorMessage = "Title cannot exceed 100 characters.")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required.")]
        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Target Area is required.")]
        public HealthArea TargetArea { get; set; }

        [Range(1, 12, ErrorMessage = "Suggested Week must be between 1 and 12.")]
        public int SuggestedWeek { get; set; }

        [Range(1, 100, ErrorMessage = "Points must be between 1 and 100.")]
        public int Points { get; set; } = 5;

        [Url(ErrorMessage = "Media Link must be a valid URL.")]
        public string? MediaLink { get; set; }

        public bool IsActive { get; set; } = true;
        public bool IsCommonTask { get; set; } = false;

        // --- Audit fields ---
        // GameTask.cs
        public DateTime CreatedAt { get; set; }            // DB default fills this
        public string? CreatedByUserId { get; set; }       // MUST be nullable
        public ApplicationUser? CreatedByUser { get; set; } // single nav to user

    }

    public enum HealthArea
    {
        [Display(Name = "Physical Activity")]
        PhysicalActivity,

        [Display(Name = "Breakfast")]
        Breakfast,

        [Display(Name = "Fruit & Veg")]
        FruitVeg,

        [Display(Name = "Sweet Snacks")]
        SweetSnacks,

        [Display(Name = "Fatty Foods")]
        FattyFoods
    }
}
