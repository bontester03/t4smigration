using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.ComponentModel.DataAnnotations;
using WebApit4s.Identity; // assuming ApplicationUser is in this namespace

namespace WebApit4s.Models
{
    public class Feedback
    {
        public int Id { get; set; }

       
        public string UserId { get; set; } = null!;  // FK to AspNetUsers

        [ValidateNever]
        public ApplicationUser User { get; set; } = null!;

        [Required]
        [StringLength(100)]
        public string Category { get; set; } = null!; // e.g., "Registration", "Staff", "Overall"

        [Range(1, 5)]
        public int Rating { get; set; } // 1–5 stars

        [StringLength(500)]
        public string? Comments { get; set; }

        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    }
}
