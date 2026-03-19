namespace WebApit4s.ViewModels
{
    public class VideoSuggestionItemVM
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string YouTubeUrl { get; set; } = "";
        public string ThumbnailUrl { get; set; } = "";
        public int CoinValue { get; set; }
        public WebApit4s.Models.VideoCategory Category { get; set; }
    }
}
