namespace WebApit4s.Models
{
    public class ChildGameTask
    {
        public int Id { get; set; }
        public int? ChildId { get; set; }
        public Child Child { get; set; } = null!;
        public int GameTaskId { get; set; }
        public GameTask GameTask { get; set; } = null!;
        public DateTime AssignedDate { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedDate { get; set; }

        public bool IsCommonTask { get; set; } = false; // <-- new
        public bool IsCompleted => CompletedDate != null;

        public bool IsRecurringDaily { get; set; } = false;
        public DateTime ExpiryDate { get; set; }  // set when task is created

        public bool IsExpired => !IsCompleted && DateTime.UtcNow > ExpiryDate;

        public bool IsGenerated { get; set; } = false; // ✅ new field


    }


}
