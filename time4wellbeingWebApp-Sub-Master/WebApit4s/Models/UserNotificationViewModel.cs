namespace WebApit4s.Models
{
    public class UserNotificationViewModel
    {
        public int Id { get; set; }
        public string Message { get; set; }
        public bool IsRead { get; set; } // ✅ Ensure this is a boolean
        public DateTime CreatedAt { get; set; }
    }
}
