using System;

namespace WebApit4s.DTO.HealthScores
{
    public sealed class HealthScoreDto
    {
        public int Id { get; set; }
        public int ChildId { get; set; }
        public DateTime DateRecorded { get; set; }

        public int PhysicalActivityScore { get; set; }
        public int BreakfastScore { get; set; }
        public int FruitVegScore { get; set; }
        public int SweetSnacksScore { get; set; }
        public int FattyFoodsScore { get; set; }

        // convenience
        public int TotalScore { get; set; }        // 0–20
        public decimal OverallScore10 { get; set; } // 0–10 (Total/2)
    }

    public sealed class UpsertHealthScoreRequest
    {
        public int? Id { get; set; } // null = create
        public int? ChildId { get; set; } // optional; if null use activeChildId
        public DateTime DateRecorded { get; set; }

        public int PhysicalActivityScore { get; set; }
        public int BreakfastScore { get; set; }
        public int FruitVegScore { get; set; }
        public int SweetSnacksScore { get; set; }
        public int FattyFoodsScore { get; set; }
    }
}
