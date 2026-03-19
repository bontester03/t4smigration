namespace WebApit4s.Models
{
    public class ChildViewModel
    {

        public int ChildId { get; set; } 
        public string UserId { get; set; }
        public string ChildName { get; set; }
        public string Email { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Gender { get; set; }
        public DateTime RegistrationDate { get; set; }

        public string ReferralTypeName { get; set; } // ✅ Added
        public string School { get; set; } // ✅ New: for school name display and filtering

    }



}
