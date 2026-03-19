using WebApit4s.Models;

namespace WebApit4s.ViewModels
{
    public class ChildWithScoreViewModel
    {
        public Child Child { get; set; } = new Child();
        public HealthScore HealthScore { get; set; } = new HealthScore();
    }
}
