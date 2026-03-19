using WebApit4s.Identity;

namespace WebApit4s.Models
{
    public class CustomGoal
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Points { get; set; }
        public DateTime AssignedDate { get; set; }
        public DateTime DueDate { get; set; }
        public string AssignedById { get; set; } = null!;

        public ApplicationUser AssignedBy { get; set; } = null!;
        public int AssignedToChildId { get; set; }
        public Child AssignedToChild { get; set; } = null!;
        public bool IsCompleted { get; set; }
        public DateTime? CompletedDate { get; set; }
    }



}
