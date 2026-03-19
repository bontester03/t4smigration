using System.ComponentModel.DataAnnotations;

namespace WebApit4s.DTO.Children;

public sealed class UpdateAvatarDto
{
    [Required]
    public string AvatarUrl { get; set; } = string.Empty;
}
