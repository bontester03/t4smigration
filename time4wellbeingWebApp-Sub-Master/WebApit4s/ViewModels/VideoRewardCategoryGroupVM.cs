using System.Collections.Generic;

namespace WebApit4s.ViewModels
{
    public class VideoRewardCategoryGroupVM
    {
        public WebApit4s.Models.VideoCategory Category { get; set; }
        public string CategoryDisplayName { get; set; } = "";
        public List<VideoSuggestionItemVM> Videos { get; set; } = new();
    }
}