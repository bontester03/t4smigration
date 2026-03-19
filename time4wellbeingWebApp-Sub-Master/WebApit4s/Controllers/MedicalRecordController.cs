using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using WebApit4s.DAL;
using WebApit4s.Identity;
using WebApit4s.Models;

namespace WebApit4s.Controllers
{
    [Authorize]
    public class MedicalRecordController : Controller
    {
        private readonly TimeContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public MedicalRecordController(TimeContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private async Task<ApplicationUser?> GetCurrentUserAsync() => await _userManager.GetUserAsync(User);

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var user = GetCurrentUserAsync().Result;

            if (user != null)
            {
                // Try to get ActiveChildId from session
                var activeChildId = context.HttpContext.Session.GetInt32("ActiveChildId");

                Child child = null;
                if (activeChildId.HasValue)
                {
                    // Prefer active child from session
                    child = _context.Children.FirstOrDefault(c => c.Id == activeChildId.Value && c.UserId == user.Id);
                }

                // Fallback: first child if no active session child found
                if (child == null)
                {
                    child = _context.Children.FirstOrDefault(c => c.UserId == user.Id);
                }

                ViewBag.UserEmail = user.Email ?? "No Email Found";
                ViewBag.ChildName = child?.ChildName ?? "No Child Assigned";
                ViewBag.ChildAvatar = !string.IsNullOrEmpty(child?.AvatarUrl)
                               ? child.AvatarUrl
                               : "/images/default-child-avatar.png";  // ✅ added avatar
            }
            else
            {
                ViewBag.UserEmail = "Guest";
                ViewBag.ChildName = "Guest";
                ViewBag.ChildAvatar = "/images/default-child-avatar.png"; // ✅ added avatar for guest
            }

            base.OnActionExecuting(context);
        }
        // GET: MedicalRecord/Create
        public async Task<IActionResult> Create(int childId)
        {
            var child = await _context.Children.FindAsync(childId);
            if (child == null) return NotFound();

            var model = new MedicalRecord
            {
                ChildId = childId,
                Child = child
            };

            ViewBag.ChildName = child.ChildName;
            return View(model);
        }

        // POST: MedicalRecord/Create
        // POST: MedicalRecord/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MedicalRecord model)
        {

            ModelState.Remove("Child"); // ✅ Ignore navigation property

            if (!ModelState.IsValid)
            {
                var childFromDb = await _context.Children.FindAsync(model.ChildId);
                ViewBag.ChildName = childFromDb?.ChildName ?? "Child";
                return View(model);
            }

            model.CreatedAt = DateTime.UtcNow;
            _context.MedicalRecords.Add(model);
            await _context.SaveChangesAsync();

            var associatedChild = await _context.Children.FirstOrDefaultAsync(c => c.Id == model.ChildId);
            return RedirectToAction("Create", "HealthScores");

        }

    }
}
