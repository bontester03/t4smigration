using WebApit4s.Models;

namespace WebApit4s.ViewModels
{
    public class GuestRegistrationViewModel
    {
        // User
        public string Email { get; set; }

        // Personal Details
        public string ParentName { get; set; }
        public string Relationship { get; set; }
        public string Phone { get; set; }
        public string Postcode { get; set; }
        public string GPPractice { get; set; }
        public string School { get; set; }
        public string Class { get; set; }
        public int ReferralTypeId { get; set; }

        // Child
        public string ChildName { get; set; }
        public DateTime ChildDOB { get; set; }
        public Gender Gender { get; set; }

        // Health Score
        public int PhysicalActivityScore { get; set; }
        public int BreakfastScore { get; set; }
        public int FruitVegScore { get; set; }
        public int SweetSnacksScore { get; set; }
        public int FattyFoodsScore { get; set; }
    }

}
