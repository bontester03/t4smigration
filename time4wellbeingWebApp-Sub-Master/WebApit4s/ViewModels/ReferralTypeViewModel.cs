using System.ComponentModel.DataAnnotations;
using WebApit4s.Models;

namespace WebApit4s.ViewModels
{
    public class ReferralTypeViewModel
    {
        [Required]
        public string Name { get; set; }

        [Required]
        public ReferralCategory Category { get; set; }

        public bool RequiresSchoolSelection { get; set; }

        public bool IsActive { get; set; }
    }
}
