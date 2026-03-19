namespace WebApit4s.ViewModels
{
    public class ChildCompletionStatusViewModel
    {
        // Existing
        public string ChildName { get; set; }
        public DateTime DateOfBirth { get; set; }
        public bool HasPersonalDetails { get; set; }
        public bool HasHealthScores { get; set; }
        public bool HasMeasurements { get; set; }

        // New for Admin Report
        public string UserId { get; set; }
        public string Email { get; set; }
        public string ReferralType { get; set; }
        public DateTime RegistrationDate { get; set; }
    }
}
