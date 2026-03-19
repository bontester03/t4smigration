using System.ComponentModel.DataAnnotations;

namespace WebApit4s.ViewModels
{
    public class ForgotPasswordViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
