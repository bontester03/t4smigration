using WebApit4s.DTO.Registration;

namespace WebApit4s.Services.Registration
{
    public interface IRegistrationService
    {
        Task<RegistrationOptionsDto> GetOptionsAsync(CancellationToken cancellationToken = default);
        Task<RegistrationResultDto> SubmitAsync(RegistrationSubmitDto request, CancellationToken cancellationToken = default);
    }
}
