using WebApit4s.ViewModels;

namespace WebApit4s.Models
{
    public class AdminUserStatusViewModel
    {

        public string UserId { get; set; }
        public string Email { get; set; }
        public string ReferralType { get; set; }
        public DateTime RegistrationDate { get; set; }

        public bool HasChildDetails { get; set; }
        public bool HasPersonalDetails { get; set; }  // Add this to AdminUserStatusViewModel

        public bool HasHealthScores { get; set; }
        public bool HasMeasurements { get; set; }

        public List<ChildCompletionStatusViewModel> ChildStatuses { get; set; } = new();
    }
}
