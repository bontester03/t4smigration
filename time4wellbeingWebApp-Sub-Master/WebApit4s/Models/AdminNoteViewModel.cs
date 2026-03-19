namespace WebApit4s.Models
{
    public class AdminNoteViewModel
    {
        public int Id { get; set; }
        public int ChildId { get; set; }
        public string NoteText { get; set; }
        public DateTime CreatedAt { get; set; }
        
    }
}
