using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebApit4s.DAL;
using WebApit4s.DTO;
using WebApit4s.DTO.Children;
using WebApit4s.DTO.Profile;
using WebApit4s.Identity;
using WebApit4s.Models;
using WebApit4s.TagHelpers;
using WebAPIts.Services.Interfaces; // ✅ Add this using statement

namespace WebApit4s.Services
{
    public sealed class ProfileService : IProfileService
    {
        private readonly IHttpContextAccessor _http;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly TimeContext _context;

        public ProfileService(
            IHttpContextAccessor httpContextAccessor,
            UserManager<ApplicationUser> userManager,
            TimeContext context)
        {
            _http = httpContextAccessor;
            _userManager = userManager;
            _context = context;
        }

        public async Task<ProfileResponseDto?> GetProfileAsync(
         string userId,
         int? activeChildId = null,
         CancellationToken ct = default)
        {
            var user = await _context.Users
                .Include(u => u.PersonalDetails)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId, ct);

            if (user == null)
                return null;

            Child? childEntity = null; // ⚠️ Use different variable name

            if (activeChildId.HasValue)
            {
                Console.WriteLine($"🔍 API: Looking for child with ID: {activeChildId.Value}");

                // ✅ Get the Child entity first (not ChildDto)
                childEntity = await _context.Children
                    .AsNoTracking()
                    .Where(c => c.UserId == userId && c.Id == activeChildId.Value && !c.IsDeleted)
                    .FirstOrDefaultAsync(ct);

                if (childEntity != null)
                {
                    Console.WriteLine($"✅ API: Found child: {childEntity.ChildName} (ID: {childEntity.Id})");
                    Console.WriteLine($"📝 Raw AvatarUrl from DB: '{childEntity.AvatarUrl}'");
                }
            }

            if (childEntity == null)
            {
                Console.WriteLine($"🔄 API: Falling back to most recent child");

                childEntity = await _context.Children
                    .AsNoTracking()
                    .Where(c => c.UserId == userId && !c.IsDeleted)
                    .OrderByDescending(c => c.CreatedAt)
                    .FirstOrDefaultAsync(ct);
            }

            if (childEntity == null)
            {
                Console.WriteLine($"❌ API: No children found for user {userId}");
                return null;
            }

            // ✅ Convert avatar URL using UrlHelper
            var processedAvatarUrl = UrlHelper.GetAvatarUrl(
                childEntity.AvatarUrl,
                _http.HttpContext?.Request
            );

            Console.WriteLine($"🖼️ Final avatar URL: '{processedAvatarUrl}'");

            // ✅ Map to ChildDto
            var childDto = new ChildDto
            {
                Id = childEntity.Id,
                ChildGuid = childEntity.ChildGuid,
                ChildName = childEntity.ChildName,
                Gender = childEntity.Gender,
                DateOfBirth = childEntity.DateOfBirth,
                TotalPoints = childEntity.TotalPoints,
                Level = childEntity.Level,
                AvatarUrl = processedAvatarUrl, // ✅ Use the processed URL
                EngagementStatus = childEntity.EngagementStatus,
                CreatedAt = childEntity.CreatedAt,
                UpdatedAt = childEntity.UpdatedAt,
                Age = DateTime.UtcNow.Year - childEntity.DateOfBirth.Year
            };

            var pd = user.PersonalDetails;
            var parentInfo = new PersonalDetailsDto
            {
                School = childEntity.School ?? string.Empty,
                Class = childEntity.Class ?? string.Empty,
                ParentGuardianName = pd?.ParentGuardianName ?? string.Empty,
                RelationshipToChild = pd?.RelationshipToChild ?? string.Empty,
                TeleNumber = pd?.TeleNumber ?? string.Empty,
                Email = user.Email ?? string.Empty,
                Postcode = pd?.Postcode ?? string.Empty
            };

            Console.WriteLine($"📤 API: Returning profile with avatar: '{childDto.AvatarUrl}'");

            return new ProfileResponseDto
            {
                Child = childDto,
                ParentInfo = parentInfo
            };
        }

        public async Task<bool> UpdateProfileAsync(string userId, UpdateProfileRequest request, CancellationToken ct = default)
        {
            Console.WriteLine("=== UpdateProfileAsync called ===");
            Console.WriteLine($"📥 UserId: {userId}");
            Console.WriteLine($"📥 request.ChildId: {request.ChildId}");
            Console.WriteLine($"📥 request.Child.ChildName: {request.Child?.ChildName}");

            var user = await _context.Users
                .Include(u => u.PersonalDetails)
                .FirstOrDefaultAsync(u => u.Id == userId, ct);

            if (user == null)
            {
                Console.WriteLine("❌ User not found");
                throw new KeyNotFoundException("User not found.");
            }

            Child? child = null;

            // ✅ FIX: Use the ChildId from the request
            if (request.ChildId > 0)
            {
                Console.WriteLine($"🔍 Looking for child with ID: {request.ChildId}");
                child = await _context.Children
                    .FirstOrDefaultAsync(c => c.Id == request.ChildId && c.UserId == userId && !c.IsDeleted, ct);

                if (child != null)
                {
                    Console.WriteLine($"✅ Found child: {child.ChildName} (ID: {child.Id})");
                }
                else
                {
                    Console.WriteLine($"❌ Child with ID {request.ChildId} not found!");
                }
            }
            else
            {
                Console.WriteLine($"⚠️ WARNING: request.ChildId is 0 or missing! Falling back to first child.");
            }

            // Fallback to first child only if ChildId was not provided or child not found
            if (child == null)
            {
                Console.WriteLine("🔄 Falling back to first child for user");
                child = await _context.Children
                    .FirstOrDefaultAsync(c => c.UserId == userId && !c.IsDeleted, ct);
            }

            if (child == null)
            {
                Console.WriteLine("❌ No child found at all!");
                throw new KeyNotFoundException($"Child not found.");
            }

            Console.WriteLine($"💾 Updating child: ID={child.Id}, Name={child.ChildName}");

            // --- update child info ---
            if (request.Child != null)
            {
                var oldName = child.ChildName;
                child.ChildName = request.Child.ChildName ?? child.ChildName;
                child.Gender = request.Child.Gender;
                child.DateOfBirth = request.Child.DateOfBirth;
                child.UpdatedAt = DateTime.UtcNow;

                Console.WriteLine($"📝 Updated: {oldName} → {child.ChildName}");
            }

            // --- update personal details ---
            if (request.ParentInfo != null)
            {
                if (user.PersonalDetails == null)
                {
                    user.PersonalDetails = new PersonalDetails();
                    Console.WriteLine("📝 Created new PersonalDetails");
                }

                user.PersonalDetails.ParentGuardianName = request.ParentInfo.ParentGuardianName;
                user.PersonalDetails.RelationshipToChild = request.ParentInfo.RelationshipToChild;
                user.PersonalDetails.TeleNumber = request.ParentInfo.TeleNumber;
                user.PersonalDetails.Email = request.ParentInfo.Email;
                user.PersonalDetails.Postcode = request.ParentInfo.Postcode;

                // School/class are child-scoped now.
                child.School = request.ParentInfo.School;
                child.Class = request.ParentInfo.Class;
            }

            await _context.SaveChangesAsync(ct);

            Console.WriteLine($"✅ Successfully saved changes for child ID {child.Id}");
            Console.WriteLine("=== UpdateProfileAsync completed ===");

            return true;
        }

        public async Task<bool> UpdateAvatarAsync(string userId, UpdateAvatarDto request, CancellationToken ct = default)
        {
            var child = await _context.Children
                .FirstOrDefaultAsync(c => c.UserId == userId && !c.IsDeleted, ct);

            if (child == null)
                throw new KeyNotFoundException("Child not found.");

            if (!string.IsNullOrWhiteSpace(request.AvatarUrl))
                child.AvatarUrl = request.AvatarUrl;

            await _context.SaveChangesAsync(ct);
            return true;
        }
    }
}
