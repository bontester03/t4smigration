using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using WebApit4s.Models;
using WebApit4s.DAL;
using WebApit4s.ViewModels;

using Microsoft.AspNetCore.Identity;
using WebApit4s.Identity;
using Microsoft.EntityFrameworkCore;

namespace WebApit4s.Controllers
{
    [Authorize(Roles = "Admin,Employee")]
    public class GuestLinkController : Controller
    {
        private readonly TimeContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public GuestLinkController(TimeContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }


        [HttpGet]
        public IActionResult Create()
        {
            var vm = new GuestLinkCreateViewModel
            {
                Schools = _context.Schools.Select(x => new SelectListItem { Value = x.Id.ToString(), Text = x.Name }).ToList(),
                Classes = _context.Classes.Select(x => new SelectListItem { Value = x.Id.ToString(), Text = x.Name }).ToList()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(GuestLinkCreateViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                vm.Schools = _context.Schools.Select(x => new SelectListItem { Value = x.Id.ToString(), Text = x.Name }).ToList();
                vm.Classes = _context.Classes.Select(x => new SelectListItem { Value = x.Id.ToString(), Text = x.Name }).ToList();
                return View(vm);
            }

            var code = Guid.NewGuid().ToString("N");

            var guestLink = new GuestRegistrationLink
            {
                SchoolId = vm.SchoolId,
                ClassId = vm.ClassId,
                ExpiryDate = NormalizeToUtc(vm.ExpiryDate),
                MaxUses = vm.MaxUses,
                UniqueCode = code,
                Uses = 0,
                IsDisabled = false
            };

            _context.GuestRegistrationLinks.Add(guestLink);
            _context.SaveChanges();

            ViewBag.GeneratedLink = BuildAngularGuestRegistrationUrl(code);
            return View("LinkGenerated");
        }


        [HttpGet]
        [Authorize(Roles = "Admin,Employee")]
        public IActionResult Index(int? schoolId, DateTime? fromDate, DateTime? toDate)
        {
            var fromDateUtc = NormalizeToUtc(fromDate);
            var toDateUtc = NormalizeToUtc(toDate);

            var query = _context.GuestRegistrationLinks
                .Include(g => g.School)
                .Include(g => g.Class)
                .AsQueryable();

            if (schoolId.HasValue)
                query = query.Where(x => x.SchoolId == schoolId.Value);

            if (fromDateUtc.HasValue)
                query = query.Where(x => x.ExpiryDate >= fromDateUtc.Value);

            if (toDateUtc.HasValue)
                query = query.Where(x => x.ExpiryDate <= toDateUtc.Value);

            var model = new GuestLinkListViewModel
            {
                SchoolId = schoolId,
                FromDate = fromDateUtc,
                ToDate = toDateUtc,
                Schools = _context.Schools
                    .Select(x => new SelectListItem { Value = x.Id.ToString(), Text = x.Name })
                    .ToList(),
                GuestLinks = query.OrderByDescending(x => x.ExpiryDate).ToList()
            };

            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult Delete(int id)
        {
            var link = _context.GuestRegistrationLinks.Find(id);
            if (link != null)
            {
                _context.GuestRegistrationLinks.Remove(link);
                _context.SaveChanges();
            }

            return RedirectToAction("Index");
        }

        [Authorize(Roles = "Admin,Employee")]
        [HttpGet]
        public IActionResult SchoolLinksIndex()
        {
            var schools = _context.Schools.ToList();

            var model = schools.Select(s => new SchoolLinkIndexViewModel
            {
                SchoolId = s.Id,
                SchoolName = s.Name,
                // Generate link to the school page that shows all its GuestRegistrationLinks
                SchoolPageUrl = Url.Action("School", "GuestLink", new { schoolId = s.Id }, Request.Scheme)
            }).ToList();

            return View(model);
        }



        [AllowAnonymous]
        [HttpGet("GuestLink/School/{schoolId:int}")]
        public IActionResult School(int schoolId)
        {
            var school = _context.Schools.FirstOrDefault(s => s.Id == schoolId);
            if (school == null)
                return NotFound("School not found.");

            var currentUtc = DateTime.UtcNow;

            var links = _context.GuestRegistrationLinks
                .Include(g => g.Class)
                .Where(g => g.SchoolId == schoolId && !g.IsDisabled &&
                            (!g.ExpiryDate.HasValue || g.ExpiryDate > currentUtc))
                .ToList();

            var vm = new SchoolGuestLinkViewModel
            {
                SchoolId = school.Id,
                SchoolName = school.Name,
                ClassLinks = links.Select(x => new ClassLinkInfo
                {
                    ClassId = x.ClassId,
                    ClassName = x.Class.Name,
                    RegistrationUrl = BuildAngularGuestRegistrationUrl(x.UniqueCode)
                }).ToList()
            };

            return View(vm);
        }

        private static DateTime? NormalizeToUtc(DateTime? value)
        {
            if (!value.HasValue)
                return null;

            return value.Value.Kind switch
            {
                DateTimeKind.Utc => value.Value,
                DateTimeKind.Local => value.Value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
            };
        }

        private string BuildAngularGuestRegistrationUrl(string code)
        {
            var relativePath = Url.Content($"~/guest-registration/{Uri.EscapeDataString(code)}/step-1");
            return $"{Request.Scheme}://{Request.Host}{relativePath}";
        }

    }

}
