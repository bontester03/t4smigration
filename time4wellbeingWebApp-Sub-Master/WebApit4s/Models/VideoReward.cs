using System.ComponentModel.DataAnnotations;

namespace WebApit4s.Models
{
    public class VideoReward
    {
        public int Id { get; set; }
        [Required] public string Title { get; set; }
        [Required, Url] public string YouTubeUrl { get; set; }

        public int CoinValue { get; set; }
        public bool IsActive { get; set; } = true;

        // Award rules
        public int MinWatchPercent { get; set; } = 80;     // e.g., must watch 80%+
        public int CooldownHoursPerChild { get; set; } = 0; // 0 = no cooldown
        public int MaxAwardsPerChild { get; set; } = 1;

        // ✅ New: Category
        [Required]
        public VideoCategory Category { get; set; }
    }

    public enum VideoCategory
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
        FattyFoods,

        [Display(Name = "Miscellaneous")]
        Miscellaneous
    }

}
