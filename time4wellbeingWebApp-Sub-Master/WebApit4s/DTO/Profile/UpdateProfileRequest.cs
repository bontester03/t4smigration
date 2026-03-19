using WebApit4s.DTO;
using WebApit4s.DTO.Children;


namespace WebApit4s.DTO.Profile
{
    public sealed class UpdateProfileRequest
    {
        // Reuse your existing update contracts
        public int ChildId { get; set; }
        public UpdateChildDto? Child { get; set; }
        public PersonalDetailsDto? ParentInfo { get; set; }
    }
}
