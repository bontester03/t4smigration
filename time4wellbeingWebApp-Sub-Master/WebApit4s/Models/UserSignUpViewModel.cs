using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApit4s.Models
{
    public class UserSignUpViewModel
    {
        //public User User { get; set; } = new User();

        [Required(ErrorMessage = "Please confirm your password.")]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        public List<SelectListItem> ReferralTypes { get; set; } = new List<SelectListItem>();

        //[NotMapped]
        //public string Password => User?.Password ?? string.Empty;
    }
}
