namespace WebApit4s.Models
{
    public class DashboardViewModel
    {

        public Child? ChildData { get; set; }
        public PersonalDetails PersonalDetailsData { get; set; }


        public bool HasChildData { get; set; }
        public bool HasPersonalDetails { get; set; }

        public bool IsSelfReferral { get; set; }

        public List<Schools> Schools { get; set; } = new();
        public List<Classes> Classes { get; set; } = new();
    }
}
