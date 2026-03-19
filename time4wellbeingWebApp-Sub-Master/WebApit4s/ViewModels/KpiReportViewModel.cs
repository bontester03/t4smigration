using WebApit4s.Models;

namespace WebApit4s.ViewModels
{
    public class KpiReportViewModel
    {
        public int Year { get; set; }
        public List<KpiReport> KeyPerformanceIndicators { get; set; } = new();
        public ServiceLevelData ServiceLevel { get; set; } = new();
        public List<ChecklistItem> Checklist { get; set; } = new();
    }

    public class ServiceLevelData
    {
        public string ContractRef { get; set; } = "SL 1";
        public string Measure { get; set; } = "Number of CYP supported";
        public string Target { get; set; } = "604";
        public string InformationRequired { get; set; } = "Total children and young people supported by the service";
        public int PreviousYear { get; set; }
        public bool Q1 { get; set; } = true;
        public bool Q2 { get; set; } = true;
        public bool Q3 { get; set; } = true;
        public bool Q4 { get; set; } = true;
    }

    public class ChecklistItem
    {
        public string Description { get; set; } = string.Empty;
        public bool Q1 { get; set; } = true;
        public bool Q2 { get; set; } = true;
        public bool Q3 { get; set; } = true;
        public bool Q4 { get; set; } = true;
    }
}
