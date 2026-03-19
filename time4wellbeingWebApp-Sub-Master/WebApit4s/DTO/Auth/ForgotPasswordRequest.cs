using System.ComponentModel.DataAnnotations;

namespace WebApit4s.DTO.Auth
{
    public class ForgotPasswordRequest
    {
        [Required, EmailAddress] public string Email { get; set; } = string.Empty;
        // If you’re sending your own deep link from the mobile app, pass it here to compose:
        public string? ResetCallbackBaseUrl { get; set; } // e.g. https://app.time4wellbeing.org/reset
    }
}
