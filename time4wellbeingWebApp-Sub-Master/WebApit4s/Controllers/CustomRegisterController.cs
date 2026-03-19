using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebApit4s.DAL;
using WebApit4s.Identity;
using WebApit4s.Models;
using WebApit4s.Utilities;
using WebApit4s.ViewModels;

namespace WebApit4s.Controllers
{
    [AllowAnonymous]
    [Route("register/guest")]
    public class CustomRegisterController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly TimeContext _context;

        public CustomRegisterController(UserManager<ApplicationUser> userManager, TimeContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            // Optionally prepopulate schools/classes here
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(GuestRegistrationViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = new ApplicationUser
            {
                Email = model.Email,
                UserName = model.Email,
                IsGuestUser = true,
                IsLoginEnabled = false,
                RegistrationDate = DateTime.UtcNow,
                ReferralTypeId = model.ReferralTypeId, // e.g. "School Referral"
                IsApprovedByAdmin = true
            };

            var result = await _userManager.CreateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);
                return View(model);
            }

            // Save personal details
            var personal = new PersonalDetails
            {
                UserId = user.Id,
                ParentGuardianName = model.ParentName,
                RelationshipToChild = model.Relationship,
                TeleNumber = model.Phone,
                Email = model.Email,
                Postcode = model.Postcode,
                
            };
            _context.PersonalDetails.Add(personal);

            // Save initial child and health score
            var child = new Child
            {
                UserId = user.Id,
                ChildName = model.ChildName,
                DateOfBirth = DateTimeUtils.EnsureUtc(model.ChildDOB),
                Gender = model.Gender,
                School = model.School,
                Class = model.Class,
                LastLogin = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Children.Add(child);
            await _context.SaveChangesAsync();

            var health = new HealthScore
            {
                ChildId = child.Id,
                PhysicalActivityScore = model.PhysicalActivityScore,
                BreakfastScore = model.BreakfastScore,
                FruitVegScore = model.FruitVegScore,
                SweetSnacksScore = model.SweetSnacksScore,
                FattyFoodsScore = model.FattyFoodsScore,
                DateRecorded = DateTime.UtcNow,
                Source = "GuestForm"
            };
            _context.HealthScores.Add(health);

            await _context.SaveChangesAsync();

            return RedirectToAction("ThankYou");
        }

        [HttpGet]
        public IActionResult ThankYou()
        {
            return View();
        }
    }

}
