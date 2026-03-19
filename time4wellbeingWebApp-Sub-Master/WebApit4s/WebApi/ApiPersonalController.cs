using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApit4s.DAL;
using WebApit4s.DTO;

namespace WebApit4s.API
{
    [ApiController]
    [Route("api/[controller]")]
    public class ApiPersonalController : ControllerBase
    {
        private readonly TimeContext _context;

        public ApiPersonalController(TimeContext context)
        {
            _context = context;
        }

        [HttpGet("{userId}")]
        public async Task<ActionResult<PersonalDetailsDto>> GetPersonalDetails(int userId)
        {
            var uid = userId.ToString();
            var details = await _context.PersonalDetails
                .Where(p => p.UserId == uid)
                .Select(p => new PersonalDetailsDto
                {
                    School = _context.Children
                        .Where(c => c.UserId == uid && !c.IsDeleted)
                        .OrderByDescending(c => c.CreatedAt)
                        .Select(c => c.School)
                        .FirstOrDefault() ?? string.Empty,
                    Class = _context.Children
                        .Where(c => c.UserId == uid && !c.IsDeleted)
                        .OrderByDescending(c => c.CreatedAt)
                        .Select(c => c.Class)
                        .FirstOrDefault() ?? string.Empty,
                    ParentGuardianName = p.ParentGuardianName,
                    RelationshipToChild = p.RelationshipToChild,
                    TeleNumber = p.TeleNumber,
                    Email = p.Email,
                    Postcode = p.Postcode,
                })
                .FirstOrDefaultAsync();

            if (details == null)
                return NotFound("Personal details not found.");

            return Ok(details);
        }
    }
}
