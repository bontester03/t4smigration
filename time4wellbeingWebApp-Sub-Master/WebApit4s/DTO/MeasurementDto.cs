namespace WebApit4s.DTO
{
    public class MeasurementDto
    {
        public int Id { get; set; } // ✅ This fixes the error
        public double Height { get; set; }
        public double Weight { get; set; }
        public double CentileScore { get; set; }
        public DateTime DateRecorded { get; set; }
    }


}
