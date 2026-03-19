using System.ComponentModel.DataAnnotations;
using WebApit4s.Models;

namespace WebApit4s.ViewModels
{
    public class CreateChildViewModel
    {
        [Required]
        public string ParentId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Child's name is required.")]
        [Display(Name = "Child Name")]
        public string ChildName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Gender is required.")]
        public Gender Gender { get; set; }

        [Required(ErrorMessage = "Date of birth is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime DateOfBirth { get; set; }
    }



}
