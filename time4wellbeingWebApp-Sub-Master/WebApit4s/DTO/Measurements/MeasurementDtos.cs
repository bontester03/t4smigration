using System;

namespace WebApit4s.DTO.Measurements
{
    public sealed class MeasurementDto
    {
        public int Id { get; set; }
        public int ChildId { get; set; }
        public DateTime DateRecorded { get; set; }

        public decimal Height { get; set; } // cm
        public decimal Weight { get; set; } // kg

        // convenience
        public decimal? BMI { get; set; }
        public string? HealthRange { get; set; } // your centile band / computed field if stored
    }

    public sealed class UpsertMeasurementRequest
    {
        public int? Id { get; set; } // null = create
        public int? ChildId { get; set; } // optional; if null use activeChildId
        public DateTime DateRecorded { get; set; }

        public decimal Height { get; set; } // cm
        public decimal Weight { get; set; } // kg
    }
}
