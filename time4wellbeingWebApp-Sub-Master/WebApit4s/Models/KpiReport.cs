namespace WebApit4s.Models
{
    public class KpiReport
    {
        public int Id { get; set; }
        public string ContractRef { get; set; }
        public string Measure { get; set; }
        public string Target { get; set; }
        public string InformationRequired { get; set; }

        public int ReportingYear { get; set; } // 🔥 Add this line
        public int? PreviousYear { get; set; }

        public int? Q1 { get; set; }
        public int? Q2 { get; set; }
        public int? Q3 { get; set; }



        public int? Q4 { get; set; }

        public int YTD => (Q1 ?? 0) + (Q2 ?? 0) + (Q3 ?? 0) + (Q4 ?? 0);
    }

}
