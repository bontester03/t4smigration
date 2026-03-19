using System.ComponentModel.DataAnnotations;

namespace WebApit4s.DTO.Auth
{
    public class PasswordResetByDobRequest
    {
        [Required, EmailAddress] public string Email { get; set; } = string.Empty;
        [Required] public DateTime DateOfBirth { get; set; }
        [Required, MinLength(6)] public string NewPassword { get; set; } = string.Empty;
    }
}
