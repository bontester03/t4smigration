namespace WebApit4s.DTO.Registration
{
    public class RegistrationResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? RedirectUrl { get; set; }
        public string? UserId { get; set; }
    }
}
