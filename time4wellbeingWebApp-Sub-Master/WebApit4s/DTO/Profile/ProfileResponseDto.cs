using WebApit4s.DTO;
using WebApit4s.DTO.Children;


namespace WebApit4s.DTO.Profile
{
    public sealed class ProfileResponseDto
    {
        public ChildDto? Child { get; set; }
        public PersonalDetailsDto? ParentInfo { get; set; }
    }
}
