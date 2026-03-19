using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApit4s.DTO.Registration;
using WebApit4s.Services.Registration;

namespace WebApit4s.API
{
    [ApiController]
    [AllowAnonymous]
    [Route("api/registration")]
    public class RegistrationApiController : ControllerBase
    {
        private readonly IRegistrationService _registrationService;

        public RegistrationApiController(IRegistrationService registrationService)
        {
            _registrationService = registrationService;
        }

        [HttpGet("options")]
        public async Task<ActionResult<RegistrationOptionsDto>> GetOptions(CancellationToken cancellationToken)
        {
            var options = await _registrationService.GetOptionsAsync(cancellationToken);
            return Ok(options);
        }

        [HttpPost("submit")]
        public async Task<ActionResult<RegistrationResultDto>> Submit([FromBody] RegistrationSubmitDto request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var result = await _registrationService.SubmitAsync(request, cancellationToken);
            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
    }
}
