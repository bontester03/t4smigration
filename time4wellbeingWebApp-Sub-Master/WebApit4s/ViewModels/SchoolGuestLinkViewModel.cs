namespace WebApit4s.ViewModels
{
    public class SchoolGuestLinkViewModel
    {
        public int SchoolId { get; set; }
        public string SchoolName { get; set; } = string.Empty;
        public List<ClassLinkInfo> ClassLinks { get; set; } = new();
    }

    public class ClassLinkInfo
    {
        public int ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public string RegistrationUrl { get; set; } = string.Empty;
    }
}
