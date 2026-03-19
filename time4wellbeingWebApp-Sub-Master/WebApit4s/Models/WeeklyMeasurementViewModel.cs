namespace WebApit4s.Models
{
    public class WeeklyMeasurementViewModel
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public string ChildName { get; set; }
        public int Age { get; set; }
        public decimal Height { get; set; }
        public decimal Weight { get; set; }
        public int CentileScore { get; set; }
        public string HealthRange { get; set; }
        public DateTime DateRecorded { get; set; }
    }
}
