using System.ComponentModel.DataAnnotations;

namespace WebApit4s.DTO.Auth
{
    public class LoginRequestDto
    {
        [Required, EmailAddress] public string Email { get; set; } = string.Empty;
        [Required] public string Password { get; set; } = string.Empty;
    }
}
