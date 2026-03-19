namespace WebApit4s.DTO.GuestRegistration
{
    public class GuestRegistrationContextDto
    {
        public string Code { get; set; } = string.Empty;
        public string? SchoolName { get; set; }
        public string? ClassName { get; set; }
        public DateTime? ExpiryDateUtc { get; set; }
        public bool IsValid { get; set; }
        public bool IsExpired { get; set; }
        public bool IsDisabled { get; set; }
        public string? InvalidReason { get; set; }
    }
}
