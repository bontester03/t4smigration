using WebApit4s.Identity;

namespace WebApit4s.ViewModels
{
    public class UserListItemViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public UserType UserType { get; set; }
        public DateTime RegistrationDate { get; set; }
        public bool IsApproved { get; set; }
    }
}
