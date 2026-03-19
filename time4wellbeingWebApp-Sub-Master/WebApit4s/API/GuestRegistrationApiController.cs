using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApit4s.DTO.GuestRegistration;
using WebApit4s.Services.GuestRegistration;

namespace WebApit4s.API
{
    [ApiController]
    [AllowAnonymous]
    [Route("api/guest-registration")]
    public class GuestRegistrationApiController : ControllerBase
    {
        private readonly IGuestRegistrationService _guestRegistrationService;

        public GuestRegistrationApiController(IGuestRegistrationService guestRegistrationService)
        {
            _guestRegistrationService = guestRegistrationService;
        }

        [HttpGet("{code}/context")]
        public async Task<ActionResult<GuestRegistrationContextDto>> GetContext(string code, CancellationToken cancellationToken)
        {
            var context = await _guestRegistrationService.GetContextAsync(code, cancellationToken);
            if (!context.IsValid)
                return NotFound(context);

            return Ok(context);
        }

        [HttpGet("consent-questions")]
        public async Task<ActionResult<IReadOnlyList<GuestConsentQuestionDto>>> GetConsentQuestions(CancellationToken cancellationToken)
        {
            var questions = await _guestRegistrationService.GetConsentQuestionsAsync(cancellationToken);
            return Ok(questions);
        }

        [HttpPost("submit")]
        public async Task<ActionResult<GuestRegistrationResultDto>> Submit([FromBody] GuestRegistrationSubmitDto request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var result = await _guestRegistrationService.SubmitAsync(request, cancellationToken);
            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }
    }
}
