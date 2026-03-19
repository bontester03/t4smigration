namespace WebApit4s.ViewModels
{
    public class AvatarPickerViewModel
    {

        public int ChildId { get; set; }
        public string? CurrentAvatarUrl { get; set; }
        public List<string> Avatars { get; set; } = new();
        public string? SelectedAvatarUrl { get; set; }
        // Default back to Dashboard
        public string? ReturnAction { get; set; } = "Index";
        public string? ReturnController { get; set; } = "Dashboard";
    }
}
