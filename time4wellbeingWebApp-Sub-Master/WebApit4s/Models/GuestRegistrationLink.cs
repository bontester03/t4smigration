namespace WebApit4s.Models
{
    public class GuestRegistrationLink
    {
        public int Id { get; set; }
        public string UniqueCode { get; set; } = Guid.NewGuid().ToString();
        public int SchoolId { get; set; }
        public int ClassId { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public int? MaxUses { get; set; }

        public int Uses { get; set; } = 0;
        public bool IsDisabled { get; set; } = false;

        public Schools School { get; set; } = null!;
        public Classes Class { get; set; } = null!;
    }
}
