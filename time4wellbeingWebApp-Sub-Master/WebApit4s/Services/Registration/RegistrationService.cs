using System.ComponentModel.DataAnnotations;
using System.IO;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using WebApit4s.DAL;
using WebApit4s.DTO.Registration;
using WebApit4s.Identity;
using WebApit4s.Models;

namespace WebApit4s.Services.Registration
{
    public class RegistrationService : IRegistrationService
    {
        private static readonly string[] RelationshipOptions = ["Father", "Mother", "Guardian", "Other"];

        private readonly TimeContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<RegistrationService> _logger;
        private readonly IWebHostEnvironment _environment;

        public RegistrationService(
            TimeContext context,
            UserManager<ApplicationUser> userManager,
            IEmailSender emailSender,
            ILogger<RegistrationService> logger,
            IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _emailSender = emailSender;
            _logger = logger;
            _environment = environment;
        }

        public async Task<RegistrationOptionsDto> GetOptionsAsync(CancellationToken cancellationToken = default)
        {
            await EnsureSelfReferralExistsAndActiveAsync(cancellationToken);

            return new RegistrationOptionsDto
            {
                ReferralTypes = await _context.ReferralTypes
                    .Where(r => r.IsActive)
                    .OrderBy(r => r.Category == ReferralCategory.SelfReferral ? 0 : 1)
                    .ThenBy(r => r.Name)
                    .Select(r => new RegistrationReferralTypeDto
                    {
                        Id = r.Id,
                        Name = r.Name,
                        Category = r.Category.ToString(),
                        RequiresSchoolSelection = r.RequiresSchoolSelection
                    })
                    .ToListAsync(cancellationToken),
                Schools = await _context.Schools
                    .Where(s => s.IsActive)
                    .OrderBy(s => s.Name)
                    .Select(s => new RegistrationOptionDto
                    {
                        Id = s.Id,
                        Name = s.Name
                    })
                    .ToListAsync(cancellationToken),
                Classes = await _context.Classes
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.Name)
                    .Select(c => new RegistrationOptionDto
                    {
                        Id = c.Id,
                        Name = c.Name
                    })
                    .ToListAsync(cancellationToken),
                Avatars = LoadAvatars(),
                Relationships = RelationshipOptions,
                Genders = Enum.GetNames<Gender>()
            };
        }

        public async Task<RegistrationResultDto> SubmitAsync(RegistrationSubmitDto request, CancellationToken cancellationToken = default)
        {
            await EnsureSelfReferralExistsAndActiveAsync(cancellationToken);

            var referralType = await _context.ReferralTypes
                .FirstOrDefaultAsync(r => r.Id == request.Account.ReferralTypeId && r.IsActive, cancellationToken);

            if (referralType == null)
            {
                return new RegistrationResultDto
                {
                    Success = false,
                    Message = "Please select a valid referral type."
                };
            }

            if (!string.Equals(request.Account.Email.Trim(), request.Parent.Email.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return new RegistrationResultDto
                {
                    Success = false,
                    Message = "Account email and parent email must match."
                };
            }

            if (string.IsNullOrWhiteSpace(request.Child.School) || string.IsNullOrWhiteSpace(request.Child.Class))
            {
                return new RegistrationResultDto
                {
                    Success = false,
                    Message = "School and class are required."
                };
            }

            if (string.IsNullOrWhiteSpace(request.Child.AvatarUrl))
            {
                return new RegistrationResultDto
                {
                    Success = false,
                    Message = "Please select a child avatar."
                };
            }

            var normalizedDob = NormalizeToUtc(request.Child.DateOfBirthUtc);
            var avatarUrl = request.Child.AvatarUrl.Trim();
            var availableAvatars = LoadAvatars();
            if (availableAvatars.Count > 0 && !availableAvatars.Contains(avatarUrl, StringComparer.OrdinalIgnoreCase))
            {
                return new RegistrationResultDto
                {
                    Success = false,
                    Message = "Please select a valid child avatar."
                };
            }

            var ageValidation = Child.ValidateAge(normalizedDob, new ValidationContext(new Child()));
            if (ageValidation != ValidationResult.Success)
            {
                return new RegistrationResultDto
                {
                    Success = false,
                    Message = ageValidation?.ErrorMessage ?? "Child age is invalid."
                };
            }

            var existingUser = await _userManager.FindByEmailAsync(request.Account.Email.Trim());
            if (existingUser != null)
            {
                return new RegistrationResultDto
                {
                    Success = false,
                    Message = "An account with this email already exists. Please sign in instead."
                };
            }

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var now = DateTime.UtcNow;
                var user = new ApplicationUser
                {
                    UserName = request.Account.Email.Trim(),
                    Email = request.Account.Email.Trim(),
                    PhoneNumber = request.Parent.TeleNumber.Trim(),
                    ReferralTypeId = referralType.Id,
                    UserType = UserType.Parent,
                    EmailConfirmed = true,
                    RegistrationDate = now,
                    IsGuestUser = false,
                    IsLoginEnabled = true,
                    IsApprovedByAdmin = true
                };

                var createResult = await _userManager.CreateAsync(user, request.Account.Password);
                if (!createResult.Succeeded)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return new RegistrationResultDto
                    {
                        Success = false,
                        Message = string.Join("; ", createResult.Errors.Select(e => e.Description))
                    };
                }

                var roleResult = await _userManager.AddToRoleAsync(user, "Parent");
                if (!roleResult.Succeeded)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return new RegistrationResultDto
                    {
                        Success = false,
                        Message = string.Join("; ", roleResult.Errors.Select(e => e.Description))
                    };
                }

                _context.PersonalDetails.Add(new PersonalDetails
                {
                    UserId = user.Id,
                    ParentGuardianName = request.Parent.ParentGuardianName.Trim(),
                    RelationshipToChild = request.Parent.RelationshipToChild.Trim(),
                    TeleNumber = request.Parent.TeleNumber.Trim(),
                    Email = request.Parent.Email.Trim(),
                    Postcode = request.Parent.Postcode.Trim()
                });

                var child = new Child
                {
                    UserId = user.Id,
                    ChildName = request.Child.ChildName.Trim(),
                    Gender = request.Child.Gender,
                    DateOfBirth = normalizedDob,
                    School = request.Child.School.Trim(),
                    Class = request.Child.Class.Trim(),
                    AvatarUrl = avatarUrl,
                    LastLogin = now,
                    CreatedAt = now,
                    UpdatedAt = now,
                    IsDeleted = false,
                    Level = 1,
                    TotalPoints = 0,
                    EngagementStatus = EngagementStatus.Engaged,
                    ChildGuid = Guid.NewGuid()
                };

                _context.Children.Add(child);
                await _context.SaveChangesAsync(cancellationToken);

                _context.MedicalRecords.Add(new MedicalRecord
                {
                    ChildId = child.Id,
                    GPPracticeName = request.Medical.GPPracticeName.Trim(),
                    GPContactNumber = request.Medical.GPContactNumber.Trim(),
                    MedicalConditions = TrimOrNull(request.Medical.MedicalConditions),
                    Allergies = TrimOrNull(request.Medical.Allergies),
                    Medications = TrimOrNull(request.Medical.Medications),
                    AdditionalNotes = TrimOrNull(request.Medical.AdditionalNotes),
                    CreatedAt = now,
                    IsSensitive = false
                });

                var rawScoreTotal = request.HealthScore.PhysicalActivityScore
                    + request.HealthScore.BreakfastScore
                    + request.HealthScore.FruitVegScore
                    + request.HealthScore.SweetSnacksScore
                    + request.HealthScore.FattyFoodsScore;
                var totalScore = rawScoreTotal + 5;

                _context.HealthScores.Add(new HealthScore
                {
                    ChildId = child.Id,
                    UserId = user.Id,
                    PhysicalActivityScore = request.HealthScore.PhysicalActivityScore,
                    BreakfastScore = request.HealthScore.BreakfastScore,
                    FruitVegScore = request.HealthScore.FruitVegScore,
                    SweetSnacksScore = request.HealthScore.SweetSnacksScore,
                    FattyFoodsScore = request.HealthScore.FattyFoodsScore,
                    TotalScore = totalScore,
                    HealthClassification = totalScore >= 15 ? "Healthy" : "Unhealthy",
                    DateRecorded = now,
                    CreatedAt = now,
                    Source = "AngularRegistrationWizard"
                });

                referralType.UsageCount += 1;

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                await TrySendWelcomeEmailAsync(user, request, cancellationToken);

                return new RegistrationResultDto
                {
                    Success = true,
                    Message = "Registration completed successfully.",
                    RedirectUrl = "/register/success",
                    UserId = user.Id
                };
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        private async Task EnsureSelfReferralExistsAndActiveAsync(CancellationToken cancellationToken)
        {
            var selfReferral = await _context.ReferralTypes
                .FirstOrDefaultAsync(r => r.Category == ReferralCategory.SelfReferral, cancellationToken);

            if (selfReferral == null)
            {
                _context.ReferralTypes.Add(new ReferralType
                {
                    Name = "Self-Referral",
                    Category = ReferralCategory.SelfReferral,
                    RequiresSchoolSelection = false,
                    IsActive = true,
                    UsageCount = 0
                });
                await _context.SaveChangesAsync(cancellationToken);
                return;
            }

            var changed = false;
            if (!selfReferral.IsActive)
            {
                selfReferral.IsActive = true;
                changed = true;
            }

            if (selfReferral.RequiresSchoolSelection)
            {
                selfReferral.RequiresSchoolSelection = false;
                changed = true;
            }

            if (!string.Equals(selfReferral.Name, "Self-Referral", StringComparison.Ordinal))
            {
                selfReferral.Name = "Self-Referral";
                changed = true;
            }

            if (changed)
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        private async Task TrySendWelcomeEmailAsync(ApplicationUser user, RegistrationSubmitDto request, CancellationToken cancellationToken)
        {
            try
            {
                var message = $@"
<h2>Welcome to Time4Wellbeing!</h2>
<p>Dear {System.Net.WebUtility.HtmlEncode(request.Parent.ParentGuardianName)},</p>
<p>Thank you for completing your registration with Time4Wellbeing.</p>
<p>Your child <strong>{System.Net.WebUtility.HtmlEncode(request.Child.ChildName)}</strong> has been registered successfully.</p>
<p>You can now sign in using your email address.</p>";

                await _emailSender.SendEmailAsync(user.Email!, "Welcome to Time4Wellbeing", message);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Registration completed but welcome email could not be sent to {Email}", user.Email);
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

        private static string? TrimOrNull(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private IReadOnlyList<string> LoadAvatars()
        {
            var folder = Path.Combine(_environment.WebRootPath, "images", "Characters");
            if (!Directory.Exists(folder))
            {
                return [];
            }

            return Directory.GetFiles(folder, "*.png")
                .OrderBy(Path.GetFileName)
                .Select(file => $"/images/Characters/{Path.GetFileName(file)}")
                .ToList();
        }
    }
}
