namespace WebApit4s.Models
{
    public class Classes
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        // ✅ Determines if the school is currently in use
        public bool IsActive { get; set; } = true;
    }
}