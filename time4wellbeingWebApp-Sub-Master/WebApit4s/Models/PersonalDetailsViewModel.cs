using Microsoft.AspNetCore.Mvc.Rendering;

namespace WebApit4s.Models
{
    public class PersonalDetailsViewModel
    {
        public PersonalDetails PersonalDetails { get; set; } = new();

        public List<SelectListItem> SchoolList { get; set; } = new();
        public List<SelectListItem> ClassList { get; set; } = new();

        public int ReferralTypeId { get; set; } // pass from session or user context
    }
}
