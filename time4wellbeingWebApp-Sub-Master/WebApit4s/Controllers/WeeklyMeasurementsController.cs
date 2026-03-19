using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using WebApit4s.DAL;
using WebApit4s.Identity;
using WebApit4s.Models;

namespace WebApit4s.Controllers
{
    [Authorize]
    public class WeeklyMeasurementsController : Controller
    {
        private readonly TimeContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public WeeklyMeasurementsController(TimeContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: WeeklyMeasurements/Index
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var activeChildId = HttpContext.Session.GetInt32("ActiveChildId");

            if (string.IsNullOrEmpty(userId) || activeChildId == null)
                return RedirectToAction("Login", "Account");

            var user = await _context.Users
                .Include(u => u.ReferralType)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return RedirectToAction("Login", "Account");

            var measurements = await _context.WeeklyMeasurements
                .Where(w => w.UserId == userId && w.ChildId == activeChildId)
                .OrderByDescending(w => w.DateRecorded)
                .ToListAsync();

            ViewBag.ReferralTypeId = user.ReferralTypeId;
            return View(measurements);
        }

        // GET: WeeklyMeasurements/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var userId = _userManager.GetUserId(User);
            var activeChildId = HttpContext.Session.GetInt32("ActiveChildId");

            if (string.IsNullOrEmpty(userId) || activeChildId == null)
                return RedirectToAction("Login", "Account");

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return RedirectToAction("Login", "Account");

            ViewBag.ReferralTypeId = user.ReferralTypeId;

            // Pass ChildId via ViewBag if needed in form
            ViewBag.ChildId = activeChildId;

            return View(new WeeklyMeasurements
            {
                UserId = userId,
                ChildId = activeChildId.Value,
                DateRecorded = DateTime.Now
            });
        }

        // POST: WeeklyMeasurements/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(WeeklyMeasurements measurement)
        {
            var userId = _userManager.GetUserId(User);
            var activeChildId = HttpContext.Session.GetInt32("ActiveChildId");

            if (string.IsNullOrEmpty(userId) || activeChildId == null)
                return RedirectToAction("Login", "Account");

            measurement.UserId = userId;
            measurement.ChildId = activeChildId.Value;

            ModelState.Remove("Child");

            if (ModelState.IsValid)
            {
                _context.Add(measurement);
                await _context.SaveChangesAsync();
                return RedirectToAction("Measurement", "Dashboard");
            }

            // ❗ Prepare chart and measurement history if validation fails
            var measurements = await _context.WeeklyMeasurements
                .Where(m => m.ChildId == activeChildId)
                .OrderByDescending(m => m.DateRecorded)
                .ToListAsync();

            ViewBag.ChartData = JsonConvert.SerializeObject(measurements.Select(m => new
            {
                date = m.DateRecorded.ToString("dd MMM"),
                height = m.Height,
                weight = m.Weight,
                centileScore = m.CentileScore
            }));

            ViewBag.ReferralTypeId = (await _context.Users.FindAsync(userId))?.ReferralTypeId;

            return View("~/Views/Dashboard/Measurement.cshtml", measurements); // 👈 returns the whole dashboard view with validation errors
        }

    }
}
