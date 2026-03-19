using System;
using System.ComponentModel.DataAnnotations;

namespace WebApit4s.Models
{
    public class AdminNote
    {
        public int Id { get; set; }

        [Required]
        public int ChildId { get; set; } // Foreign key to the child

        [Required(ErrorMessage = "Note text is required.")]
        [StringLength(1500, ErrorMessage = "Note cannot exceed 1500 characters.")]
        public string NoteText { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public Child Child { get; set; } = null!;


    }
}
