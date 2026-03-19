using WebApit4s.Models;

namespace WebApit4s.DTO.Children;

public static class ChildMappings
{
    public static ChildDto ToDto(this Child c)
    {
        var today = DateTime.Today;
        var age = today.Year - c.DateOfBirth.Year;
        if (c.DateOfBirth.Date > today.AddYears(-age)) age--;

        return new ChildDto
        {
            Id = c.Id,
            ChildGuid = c.ChildGuid,
            ChildName = c.ChildName,
            Gender = c.Gender,
            DateOfBirth = c.DateOfBirth,
            TotalPoints = c.TotalPoints,
            Level = c.Level,
            AvatarUrl = c.AvatarUrl,
            EngagementStatus = c.EngagementStatus,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt,
            Age = age
        };
    }
}
