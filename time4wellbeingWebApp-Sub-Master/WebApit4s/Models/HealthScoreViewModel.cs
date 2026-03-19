namespace WebApit4s.Models
{
    public class HealthScoreViewModel
    {
        public IEnumerable<HealthScore>? SubmittedScores { get; set; }
        public HealthScore NewScore { get; set; }
    }
}
