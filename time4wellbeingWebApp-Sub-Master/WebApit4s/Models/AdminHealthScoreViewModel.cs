namespace WebApit4s.Models
{
    public class AdminHealthScoreViewModel
    {
        public int Id { get; set; }
        public int ChildId { get; set; } 
        public string UserId { get; set; }
        public string? UserName { get; set; } // ✅ User email
        public int PhysicalActivityScore { get; set; }
        public int BreakfastScore { get; set; }
        public int FruitVegScore { get; set; }
        public int SweetSnacksScore { get; set; }
        public int FattyFoodsScore { get; set; }
        public int? TotalScore { get; set; }
        public string HealthClassification { get; set; }
        public DateTime DateRecorded { get; set; }
    }
}
