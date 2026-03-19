using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Threading.Tasks;
using WebApit4s.DAL;
using WebApit4s.Identity;
using WebApit4s.Models;
using WebApit4s.Utilities;
using WebApit4s.ViewModels;

namespace WebApit4s.Controllers
{
    public class ChildController : Controller
    {
        private readonly TimeContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _env;

        public ChildController(TimeContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment env)
        {
            _context = context;
            _userManager = userManager;
            _env = env;
        }

        private async Task<ApplicationUser?> GetCurrentUserAsync() => await _userManager.GetUserAsync(User);

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var user = GetCurrentUserAsync().Result;

            if (user != null)
            {
                var activeChildId = context.HttpContext.Session.GetInt32("ActiveChildId");
                Child child = null;

                if (activeChildId.HasValue)
                {
                    child = _context.Children.FirstOrDefault(c => c.Id == activeChildId.Value && c.UserId == user.Id);
                }

                if (child == null)
                {
                    child = _context.Children.FirstOrDefault(c => c.UserId == user.Id);
                }

                ViewBag.UserEmail = user.Email ?? "No Email Found";
                ViewBag.ChildName = child?.ChildName ?? "No Child Assigned";
                ViewBag.ChildAvatar = !string.IsNullOrEmpty(child?.AvatarUrl)
                               ? child.AvatarUrl
                               : "/images/default-child-avatar.png";
            }
            else
            {
                ViewBag.UserEmail = "Guest";
                ViewBag.ChildName = "Guest";
                ViewBag.ChildAvatar = "/images/default-child-avatar.png";
            }

            base.OnActionExecuting(context);
        }

        // GET: Child/Create
        public async Task<IActionResult> Create()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.UserId = user.Id;
            ViewBag.Avatars = LoadAvatars(); // ✅ Use consistent method

            return View(new Child { UserId = user.Id });
        }

        // POST: Child/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Child child)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Avatars = LoadAvatars();
                return View(child);
            }

            child.DateOfBirth = DateTimeUtils.EnsureUtc(child.DateOfBirth);
            child.LastLogin = DateTime.UtcNow;
            child.CreatedAt = DateTime.UtcNow;
            child.UpdatedAt = DateTime.UtcNow;
            child.ChildGuid = child.ChildGuid == Guid.Empty ? Guid.NewGuid() : child.ChildGuid;
            child.IsDeleted = false;
            child.Level = 1;
            child.TotalPoints = 0;

            _context.Children.Add(child);
            await _context.SaveChangesAsync();

            HttpContext.Session.SetInt32("ActiveChildId", child.Id);

            return RedirectToAction("Create", "MedicalRecord", new { childId = child.Id });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateAvatar(int childId, string avatarUrl)
        {
            var child = await _context.Children.FindAsync(childId);
            if (child == null) return NotFound();

            child.AvatarUrl = avatarUrl;
            _context.Update(child);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "Dashboard");
        }

        // ✅ CONSISTENT avatar loading method
        private List<string> LoadAvatars()
        {
            var folder = Path.Combine(_env.WebRootPath, "images", "Characters"); // ✅ Consistent path
            if (!Directory.Exists(folder))
                return new List<string>();

            return Directory.GetFiles(folder, "*.png")
                            .OrderBy(Path.GetFileName)
                            .Select(f => $"/images/Characters/{Path.GetFileName(f)}") // ✅ Consistent URL path
                            .ToList();
        }

        public async Task<IActionResult> ChildProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var activeChildId = HttpContext.Session.GetInt32("ActiveChildId");
            Child? child = null;

            if (activeChildId.HasValue)
                child = await _context.Children.FirstOrDefaultAsync(c => c.Id == activeChildId.Value && c.UserId == user.Id);

            child ??= await _context.Children.FirstOrDefaultAsync(c => c.UserId == user.Id);

            if (child == null)
            {
                TempData["Error"] = "No child profile found. Please add child details.";
                return RedirectToAction("Create");
            }

            ViewBag.Avatars = LoadAvatars(); // ✅ Use consistent method
            return View(child);
        }

        // GET: Child/SelectAvatar
        [HttpGet]
        public async Task<IActionResult> SelectAvatar(int? childId, string? returnAction = "ChildProfile", string? returnController = "Child")
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var activeChildId = childId ?? HttpContext.Session.GetInt32("ActiveChildId");
            var child = activeChildId.HasValue
                ? await _context.Children.FirstOrDefaultAsync(c => c.Id == activeChildId.Value && c.UserId == user.Id)
                : await _context.Children.FirstOrDefaultAsync(c => c.UserId == user.Id);

            if (child == null)
            {
                TempData["Error"] = "No child found.";
                return RedirectToAction("Create");
            }

            var vm = new AvatarPickerViewModel
            {
                ChildId = child.Id,
                CurrentAvatarUrl = child.AvatarUrl,
                Avatars = LoadAvatars(), // ✅ Use consistent method
                ReturnAction = returnAction,
                ReturnController = returnController
            };

            return View(vm);
        }

        // POST: Child/SelectAvatar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SelectAvatar(AvatarPickerViewModel vm)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var child = await _context.Children
                .FirstOrDefaultAsync(c => c.Id == vm.ChildId && c.UserId == user.Id);

            if (child == null)
            {
                TempData["Error"] = "Child not found.";
                return RedirectToAction("Create", "Child");
            }

            if (string.IsNullOrWhiteSpace(vm.SelectedAvatarUrl))
            {
                TempData["Error"] = "Please select an avatar.";
                vm.Avatars = LoadAvatars(); // ✅ Use consistent method
                vm.CurrentAvatarUrl = child.AvatarUrl;
                return View(vm);
            }

            child.AvatarUrl = vm.SelectedAvatarUrl;
            _context.Update(child);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Avatar updated!";

            return RedirectToAction("Index", "Dashboard");
        }
    }
}
