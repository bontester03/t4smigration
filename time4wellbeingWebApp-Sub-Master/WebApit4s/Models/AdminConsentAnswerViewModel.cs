namespace WebApit4s.Models
{
    public class AdminConsentAnswerViewModel
    {
        public string UserEmail { get; set; }
        public string ChildName { get; set; } // ✅ Add this
        public string Question { get; set; }
        public string Answer { get; set; }
        public DateTime SubmittedAt { get; set; }
    }
}
