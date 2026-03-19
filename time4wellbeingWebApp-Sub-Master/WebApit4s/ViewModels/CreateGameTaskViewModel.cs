using System.ComponentModel.DataAnnotations;
using WebApit4s.Models;

namespace WebApit4s.ViewModels
{
    public class CreateGameTaskViewModel
    {
        [Required, StringLength(100)]
        public string Title { get; set; } = string.Empty;

        [Required, StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public HealthArea TargetArea { get; set; }

        [Range(1, 12)]
        public int SuggestedWeek { get; set; }

        [Range(1, 100)]
        public int Points { get; set; } = 5;

        [Url]
        public string? MediaLink { get; set; }

        // Controls which extra fields the form shows (admin vs parent)
        public bool AllowCommonToggle { get; set; } = false;
        public bool AllowActiveToggle { get; set; } = false;

        // Values admins can set via UI; parents’ values are forced server-side
        public bool IsCommonTask { get; set; } = false;
        public bool IsActive { get; set; } = true;
    }
}
