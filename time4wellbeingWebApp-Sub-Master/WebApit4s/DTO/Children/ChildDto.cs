using WebApit4s.Models;

namespace WebApit4s.DTO.Children
{
    public sealed class ChildDto
    {
        public int Id { get; set; }
        public Guid ChildGuid { get; set; }
        public string ChildName { get; set; } = string.Empty;
        public Gender Gender { get; set; }
        public DateTime DateOfBirth { get; set; }

        public int TotalPoints { get; set; }
        public int Level { get; set; }
        public string? AvatarUrl { get; set; }
        public EngagementStatus EngagementStatus { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // convenience for clients
        public int Age { get; set; }
    }
}
