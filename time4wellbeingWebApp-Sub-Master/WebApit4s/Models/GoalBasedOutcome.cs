using Microsoft.EntityFrameworkCore;
using WebApit4s.Models;

namespace WebApit4s.Models
{
    public class GoalBasedOutcome
    {
        public int Id { get; set; }
        public int ChildId { get; set; }
        public Child Child { get; set; }

        public bool AchievedHealthyDiet { get; set; }
        public bool AchievedPhysicalActivity { get; set; }
        public bool ImprovedConfidence { get; set; }
        public bool ImprovedMotivation { get; set; }

        public bool IsCompleted { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int YearCompleted => EndDate?.Year ?? 0;
    }
}

//// Get completed outcomes for the selected year
//var outcomes = await _context.GoalBasedOutcomes
//    .Where(g => g.IsCompleted && g.EndDate != null && g.EndDate.Value.Year == year)
//    .ToListAsync();

//int totalCompleted = outcomes.Count;

//// FWM 9 - Healthy Dietary Behaviours
//int dietAchieved = outcomes.Count(g => g.AchievedHealthyDiet);
//kpis.Add(new KpiReport
//{
//    ContractRef = "FWM 9",
//    Measure = "Goal: Healthy dietary behaviours achieved",
//    Target = "80%",
//    InformationRequired = "Users who achieved goals around healthy dietary behaviours",
//    Q1 = dietAchieved,
//    PreviousYear = totalCompleted,
//    ReportingYear = year
//});

//// FWM 10 - Physical Activity Goals
//int activityAchieved = outcomes.Count(g => g.AchievedPhysicalActivity);
//kpis.Add(new KpiReport
//{
//    ContractRef = "FWM 10",
//    Measure = "Goal: Physical activity achieved",
//    Target = "80%",
//    InformationRequired = "Users who achieved physical activity goals",
//    Q1 = activityAchieved,
//    PreviousYear = totalCompleted,
//    ReportingYear = year
//});

//// FWM 11 - Confidence & Self-Esteem
//int confidenceImproved = outcomes.Count(g => g.ImprovedConfidence);
//kpis.Add(new KpiReport
//{
//    ContractRef = "FWM 11",
//    Measure = "Goal: Improved confidence & self-esteem",
//    Target = "80%",
//    InformationRequired = "Users reporting improved confidence/self-esteem",
//    Q1 = confidenceImproved,
//    PreviousYear = totalCompleted,
//    ReportingYear = year
//});

//// FWM 12 - Motivation
//int motivationImproved = outcomes.Count(g => g.ImprovedMotivation);
//kpis.Add(new KpiReport
//{
//    ContractRef = "FWM 12",
//    Measure = "Goal: Improved motivation",
//    Target = "80%",
//    InformationRequired = "Users reporting improved motivation",
//    Q1 = motivationImproved,
//    PreviousYear = totalCompleted,
//    ReportingYear = year
//});
