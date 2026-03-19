using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using WebApit4s.DAL;
using WebApit4s.DTO.GuestRegistration;
using WebApit4s.Identity;
using WebApit4s.Models;

namespace WebApit4s.Services.GuestRegistration
{
    public class GuestRegistrationService : IGuestRegistrationService
    {
        private readonly TimeContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<GuestRegistrationService> _logger;

        public GuestRegistrationService(
            TimeContext context,
            UserManager<ApplicationUser> userManager,
            IEmailSender emailSender,
            ILogger<GuestRegistrationService> logger)
        {
            _context = context;
            _userManager = userManager;
            _emailSender = emailSender;
            _logger = logger;
        }

        public async Task<GuestRegistrationContextDto> GetContextAsync(string code, CancellationToken cancellationToken = default)
        {
            var nowUtc = DateTime.UtcNow;

            var link = await _context.GuestRegistrationLinks
                .Include(x => x.School)
                .Include(x => x.Class)
                .FirstOrDefaultAsync(x => x.UniqueCode == code, cancellationToken);

            if (link == null)
            {
                return new GuestRegistrationContextDto
                {
                    Code = code,
                    IsValid = false,
                    InvalidReason = "Link not found."
                };
            }

            var isExpired = link.ExpiryDate.HasValue && link.ExpiryDate.Value <= nowUtc;
            var isDisabled = link.IsDisabled;

            return new GuestRegistrationContextDto
            {
                Code = code,
                SchoolName = link.School?.Name,
                ClassName = link.Class?.Name,
                ExpiryDateUtc = link.ExpiryDate,
                IsDisabled = isDisabled,
                IsExpired = isExpired,
                IsValid = !isExpired && !isDisabled,
                InvalidReason = isDisabled ? "Link is disabled." : isExpired ? "Link has expired." : null
            };
        }

        public async Task<IReadOnlyList<GuestConsentQuestionDto>> GetConsentQuestionsAsync(CancellationToken cancellationToken = default)
        {
            return await _context.ConsentQuestions
                .OrderBy(x => x.Id)
                .Select(x => new GuestConsentQuestionDto
                {
                    ConsentQuestionId = x.Id,
                    QuestionText = x.Text
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<GuestRegistrationResultDto> SubmitAsync(GuestRegistrationSubmitDto request, CancellationToken cancellationToken = default)
        {
            var context = await GetContextAsync(request.Code, cancellationToken);
            if (!context.IsValid)
            {
                return new GuestRegistrationResultDto
                {
                    Success = false,
                    Message = context.InvalidReason ?? "Guest registration link is invalid."
                };
            }

            if (request.Parent == null || string.IsNullOrWhiteSpace(request.Parent.Email))
            {
                return new GuestRegistrationResultDto
                {
                    Success = false,
                    Message = "Parent details are required."
                };
            }

            if (request.Children == null || request.Children.Count == 0)
            {
                return new GuestRegistrationResultDto
                {
                    Success = false,
                    Message = "At least one child must be provided."
                };
            }

            var link = await _context.GuestRegistrationLinks
                .Include(x => x.School)
                .Include(x => x.Class)
                .FirstAsync(x => x.UniqueCode == request.Code, cancellationToken);

            var user = await _userManager.FindByEmailAsync(request.Parent.Email);
            if (user == null)
            {
                var schoolReferralTypeId = await _context.ReferralTypes
                    .Where(x => x.Category == ReferralCategory.School && x.IsActive)
                    .Select(x => (int?)x.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                user = new ApplicationUser
                {
                    Email = request.Parent.Email,
                    UserName = request.Parent.Email,
                    IsGuestUser = true,
                    IsLoginEnabled = false,
                    IsApprovedByAdmin = true,
                    UserType = UserType.Guest,
                    RegistrationDate = DateTime.UtcNow,
                    ReferralTypeId = schoolReferralTypeId
                };

                var createResult = await _userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                {
                    return new GuestRegistrationResultDto
                    {
                        Success = false,
                        Message = string.Join("; ", createResult.Errors.Select(x => x.Description))
                    };
                }
            }

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            var existingPersonal = await _context.PersonalDetails
                .FirstOrDefaultAsync(p => p.UserId == user.Id, cancellationToken);

            if (existingPersonal == null)
            {
                _context.PersonalDetails.Add(new PersonalDetails
                {
                    UserId = user.Id,
                    ParentGuardianName = request.Parent.ParentName,
                    TeleNumber = request.Parent.PhoneNumber,
                    Email = user.Email ?? request.Parent.Email,
                    Postcode = request.Parent.Postcode,
                    RelationshipToChild = request.Parent.Relationship
                });
            }
            else if (string.IsNullOrWhiteSpace(existingPersonal.RelationshipToChild))
            {
                existingPersonal.RelationshipToChild = request.Parent.Relationship;
                _context.PersonalDetails.Update(existingPersonal);
            }

            foreach (var childRequest in request.Children)
            {
                var child = new Child
                {
                    UserId = user.Id,
                    ChildName = childRequest.ChildName,
                    DateOfBirth = NormalizeToUtc(childRequest.DateOfBirthUtc),
                    Gender = childRequest.Gender,
                    School = link.School?.Name,
                    Class = link.Class?.Name,
                    LastLogin = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    ChildGuid = Guid.NewGuid(),
                    IsDeleted = false,
                    Level = 1,
                    TotalPoints = 0
                };

                _context.Children.Add(child);
                await _context.SaveChangesAsync(cancellationToken);

                if (childRequest.Score != null)
                {
                    var total = childRequest.Score.PhysicalActivityScore
                        + childRequest.Score.BreakfastScore
                        + childRequest.Score.FruitVegScore
                        + childRequest.Score.SweetSnacksScore
                        + childRequest.Score.FattyFoodsScore
                        + 5;

                    _context.HealthScores.Add(new HealthScore
                    {
                        ChildId = child.Id,
                        PhysicalActivityScore = childRequest.Score.PhysicalActivityScore,
                        BreakfastScore = childRequest.Score.BreakfastScore,
                        FruitVegScore = childRequest.Score.FruitVegScore,
                        SweetSnacksScore = childRequest.Score.SweetSnacksScore,
                        FattyFoodsScore = childRequest.Score.FattyFoodsScore,
                        TotalScore = total,
                        HealthClassification = total >= 15 ? "Healthy" : "Unhealthy",
                        DateRecorded = DateTime.UtcNow,
                        Source = "AngularGuestWizard"
                    });
                }

                if (childRequest.Medical != null)
                {
                    _context.MedicalRecords.Add(new MedicalRecord
                    {
                        ChildId = child.Id,
                        GPPracticeName = childRequest.Medical.GPPracticeName.Trim(),
                        GPContactNumber = childRequest.Medical.GPContactNumber.Trim(),
                        MedicalConditions = childRequest.Medical.MedicalConditions,
                        Allergies = childRequest.Medical.Allergies,
                        Medications = childRequest.Medical.Medications,
                        AdditionalNotes = childRequest.Medical.AdditionalNotes,
                        CreatedAt = DateTime.UtcNow,
                        IsSensitive = childRequest.Medical.IsSensitive
                    });
                }
            }

            foreach (var answer in request.ConsentAnswers)
            {
                _context.ConsentAnswers.Add(new ConsentAnswer
                {
                    UserId = user.Id,
                    ConsentQuestionId = answer.ConsentQuestionId,
                    Answer = answer.Answer,
                    SubmittedAt = DateTime.UtcNow
                });
            }

            link.Uses += 1;
            _context.GuestRegistrationLinks.Update(link);

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            await TrySendThankYouEmailAsync(request, context, cancellationToken);

            return new GuestRegistrationResultDto
            {
                Success = true,
                Message = "Registration completed successfully.",
                RedirectUrl = $"/guest-registration/{request.Code}/thank-you",
                UserId = user.Id
            };
        }

        private async Task TrySendThankYouEmailAsync(
            GuestRegistrationSubmitDto request,
            GuestRegistrationContextDto context,
            CancellationToken cancellationToken)
        {
            try
            {
                var childrenListHtml = string.Join(string.Empty,
                    request.Children.Select(c =>
                        $"<li style='margin:4px 0'>{System.Net.WebUtility.HtmlEncode(c.ChildName)}</li>"));

                var replacements = new Dictionary<string, string>
                {
                    ["ParentName"] = request.Parent.ParentName,
                    ["School"] = context.SchoolName ?? "-",
                    ["Class"] = context.ClassName ?? "-",
                    ["ChildrenList"] = childrenListHtml
                };

                string html;
                if (_emailSender is EmailSender concreteSender)
                {
                    html = concreteSender.LoadTemplate("RegistrationThankYou.html", replacements);
                }
                else
                {
                    html = $@"
<p>Hi {replacements["ParentName"]},</p>
<p>Thanks for registering. School: {replacements["School"]}, Class: {replacements["Class"]}</p>
<ul>{replacements["ChildrenList"]}</ul>";
                }

                await _emailSender.SendEmailAsync(
                    request.Parent.Email,
                    "Thanks for registering – Time4Wellbeing",
                    html);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Registration thank-you email failed for {Email}", request.Parent.Email);
            }
        }

        private static DateTime NormalizeToUtc(DateTime value)
        {
            return value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
            };
        }
    }
}
