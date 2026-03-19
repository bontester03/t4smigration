using WebApit4s.Models;

namespace WebApit4s.ViewModels
{
    public class SchoolRecordsViewModel
    {
        public string SchoolName { get; set; } = string.Empty;
        public List<Child> Children { get; set; } = new();
        public List<PersonalDetails> PersonalDetails { get; set; } = new();
        public List<HealthScore> HealthScores { get; set; } = new();
        public List<WeeklyMeasurements> Measurements { get; set; } = new();
        public int TotalChildrenRecords { get; set; }
        public int TotalPersonalDetailsRecords { get; set; }
        public int TotalHealthScores { get; set; }
        public int TotalMeasurements { get; set; }
    }
}
