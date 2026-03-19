using System.ComponentModel.DataAnnotations;

namespace WebApit4s.DTO.Auth
{
    public class ResetPasswordRequest
    {
        [Required, EmailAddress] public string Email { get; set; } = string.Empty;
        [Required] public string Token { get; set; } = string.Empty;
        [Required, MinLength(6)] public string NewPassword { get; set; } = string.Empty;
    }
}
