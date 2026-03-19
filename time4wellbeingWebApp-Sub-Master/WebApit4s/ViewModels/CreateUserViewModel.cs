using System.ComponentModel.DataAnnotations;
using WebApit4s.Identity;

namespace WebApit4s.ViewModels
{
    public class CreateUserViewModel
    {
        [Required]
        public string FullName { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }

        [Required]
        public UserType SelectedUserType { get; set; }

        public List<UserType> AvailableUserTypes { get; set; } = new();

        public bool IsApproved { get; set; } = true;
    }
}
