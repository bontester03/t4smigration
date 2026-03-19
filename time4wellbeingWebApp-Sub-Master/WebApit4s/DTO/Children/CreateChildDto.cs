using System.ComponentModel.DataAnnotations;
using WebApit4s.Models;

namespace WebApit4s.DTO.Children;

public sealed class CreateChildDto
{
    [Required, StringLength(100)]
    public string ChildName { get; set; } = string.Empty;

    [Required]
    public Gender Gender { get; set; }

    [Required]
    public DateTime DateOfBirth { get; set; }

    public string? AvatarUrl { get; set; }

    // optional: defaults to Engaged if omitted
    public EngagementStatus? EngagementStatus { get; set; }
}
