using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using WebApit4s.DTO.Measurements;
using WebApit4s.Services.Interfaces;

namespace WebApit4s.API
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Produces("application/json")]
    public sealed class ApiMeasurementsController : ControllerBase
    {
        private readonly IMeasurementService _service;

        public ApiMeasurementsController(IMeasurementService service)
        {
            _service = service;
        }

        private (string userId, bool isAdmin) GetContext()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
            var isAdmin = User.IsInRole("Admin");
            return (userId, isAdmin);
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<MeasurementDto>>> GetList(
            [FromQuery] int? activeChildId = null,
            [FromQuery] int take = 25,
            [FromQuery] int skip = 0,
            CancellationToken ct = default)
        {
            var (userId, isAdmin) = GetContext();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var list = await _service.GetListAsync(userId, activeChildId, isAdmin, take, skip, ct);
            return Ok(list);
        }

        [HttpGet("latest")]
        public async Task<ActionResult<MeasurementDto?>> GetLatest(
            [FromQuery] int? activeChildId = null,
            CancellationToken ct = default)
        {
            var (userId, isAdmin) = GetContext();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var dto = await _service.GetLatestAsync(userId, activeChildId, isAdmin, ct);
            return dto is null ? NotFound() : Ok(dto);
        }

        [HttpPost]
        public async Task<ActionResult<int>> Upsert([FromBody] UpsertMeasurementRequest request,
            [FromQuery] int? activeChildId = null,
            CancellationToken ct = default)
        {
            var (userId, isAdmin) = GetContext();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var id = await _service.UpsertAsync(userId, request, activeChildId, isAdmin, ct);
            return Ok(id);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct = default)
        {
            var (userId, isAdmin) = GetContext();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            await _service.DeleteAsync(userId, id, isAdmin, ct);
            return NoContent();
        }
    }
}
