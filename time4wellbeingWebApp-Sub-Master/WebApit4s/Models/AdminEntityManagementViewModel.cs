namespace WebApit4s.Models
{
    public class AdminEntityManagementViewModel
    {
        public List<Schools> Schools { get; set; } = new();
        public List<Classes> Classes { get; set; } = new();
        public List<ReferralType> ReferralTypes { get; set; } = new();
    }
}
