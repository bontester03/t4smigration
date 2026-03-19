namespace WebApit4s.DTO.Registration
{
    public class RegistrationOptionsDto
    {
        public IReadOnlyList<RegistrationReferralTypeDto> ReferralTypes { get; set; } = [];
        public IReadOnlyList<RegistrationOptionDto> Schools { get; set; } = [];
        public IReadOnlyList<RegistrationOptionDto> Classes { get; set; } = [];
        public IReadOnlyList<string> Avatars { get; set; } = [];
        public IReadOnlyList<string> Relationships { get; set; } = [];
        public IReadOnlyList<string> Genders { get; set; } = [];
    }

    public class RegistrationReferralTypeDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public bool RequiresSchoolSelection { get; set; }
    }

    public class RegistrationOptionDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
