namespace WebApit4s.Models
{
    public class NotificationViewModel
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public string Message { get; set; }
        public string IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
