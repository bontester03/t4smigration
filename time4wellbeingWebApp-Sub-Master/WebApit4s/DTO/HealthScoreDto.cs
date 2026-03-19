namespace WebApit4s.DTO
{
    public class HealthScoreDto
    {
        public int Id { get; set; }
        public DateTime DateRecorded { get; set; }
        public int PhysicalActivityScore { get; set; }
        public int BreakfastScore { get; set; }
        public int FruitVegScore { get; set; }
        public int SweetSnacksScore { get; set; }
        public int FattyFoodsScore { get; set; }
        public int? TotalScore { get; set; }
        public string HealthClassification { get; set; }
    }
}

