using WebApit4s.DTO.GuestRegistration;

namespace WebApit4s.Services.GuestRegistration
{
    public interface IGuestRegistrationService
    {
        Task<GuestRegistrationContextDto> GetContextAsync(string code, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<GuestConsentQuestionDto>> GetConsentQuestionsAsync(CancellationToken cancellationToken = default);
        Task<GuestRegistrationResultDto> SubmitAsync(GuestRegistrationSubmitDto request, CancellationToken cancellationToken = default);
    }
}
