using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebApit4s.DAL;
using WebApit4s.Identity;
using WebApit4s.Models;


namespace WebApit4s.Controllers
{
    public class HealthScoresController : Controller
    {
        private readonly TimeContext _context;
        private readonly IEmailSender _emailSender;
        private readonly UserManager<ApplicationUser> _userManager;

        public HealthScoresController(
            TimeContext context,
            UserManager<ApplicationUser> userManager,
            IEmailSender emailSender)
        {
            _context = context;
            _userManager = userManager;
            _emailSender = emailSender;
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
        // GET: HealthScores
        public async Task<IActionResult> Index()
        {
            var healthScores = await _context.HealthScores
                                             .Include(h => h.Child)
                                             .ThenInclude(c => c.User)
                                             .ToListAsync();

            return View(healthScores);
        }


        // GET: HealthScores/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var healthScore = await _context.HealthScores
                .Include(h => h.Child)
                    .ThenInclude(c => c.User) // ✅ Include the parent user
                .FirstOrDefaultAsync(m => m.Id == id);

            if (healthScore == null)
            {
                return NotFound();
            }

            return View(healthScore);
        }


        // GET: HealthScores/Create
        public IActionResult Create()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            var activeChildId = HttpContext.Session.GetInt32("ActiveChildId");

            if (activeChildId == null)
            {
                // Optional: only enforce if user lands directly on this page
                TempData["Error"] = "Please complete registration steps in order.";
                return RedirectToAction("LandingPage", "Dashboard");
            }

            return View();
        }



        // POST: HealthScores/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PhysicalActivityScore,BreakfastScore,FruitVegScore,SweetSnacksScore,FattyFoodsScore,DateRecorded")] HealthScore healthScore)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var activeChildId = HttpContext.Session.GetInt32("ActiveChildId");

            if (activeChildId == null)
            {
                ModelState.AddModelError(string.Empty, "No child selected. Please complete child registration first.");
                return View(healthScore);
            }

            // Assign child and user ID
            healthScore.ChildId = activeChildId.Value;
            healthScore.UserId = user.Id;
            ModelState.Remove("Child");
            if (!ModelState.IsValid)
                return View(healthScore);

            // ✅ Score mapping
            int physical = healthScore.PhysicalActivityScore switch { 0 => 1, 1 => 2, 2 => 3, 3 => 4, 4 => 5, _ => 0 };
            int breakfast = healthScore.BreakfastScore switch { 0 => 1, 1 => 2, 2 => 3, 3 => 4, 4 => 5, _ => 0 };
            int fruitVeg = healthScore.FruitVegScore switch { 0 => 1, 1 => 2, 2 => 3, 3 => 4, 4 => 5, _ => 0 };
            int sweet = healthScore.SweetSnacksScore switch { 0 => 1, 1 => 2, 2 => 3, 3 => 4, 4 => 5, _ => 0 };
            int fatty = healthScore.FattyFoodsScore switch { 0 => 1, 1 => 2, 2 => 3, 3 => 4, 4 => 5, _ => 0 };

            healthScore.TotalScore = physical + breakfast + fruitVeg + sweet + fatty;
            healthScore.HealthClassification = healthScore.TotalScore >= 15 ? "Healthy" : "Unhealthy";

            _context.HealthScores.Add(healthScore);
            await _context.SaveChangesAsync();

            // ✅ Send confirmation email
            if (!string.IsNullOrEmpty(user.Email))
            {
                var emailBody = $@"
<h2>Welcome to Time4Wellbeing!</h2>
<p>Dear {user.UserName},</p>
<p>Thank you for completing your profile with Time4Wellbeing. We are excited to support you and your family on your journey to a healthier lifestyle!</p>
<p style='font-size: 0.9em; color: #888; margin-top: 20px;'>
This is a computer-generated email. Please do not reply directly to this address.
For assistance, email <a href='mailto:info@time4sportuk.com'>info@time4sportuk.com</a> or visit <a href='https://www.time4sportuk.com/t4w'>https://www.time4sportuk.com/t4w</a>.
</p>
<p>Best regards,<br>Time4Wellbeing Team</p>";

                await _emailSender.SendEmailAsync(
                    user.Email,
                    "Thank you for registering with Time4Wellbeing",
                    emailBody);
            }

            TempData["Success"] = "Registration complete! Welcome to your dashboard.";
            return RedirectToAction("LandingPage", "Dashboard");
        }




        // GET: HealthScores/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var healthScore = await _context.HealthScores.FindAsync(id);
            if (healthScore == null)
            {
                return NotFound();
            }
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id", healthScore.UserId);
            return View(healthScore);
        }

        // POST: HealthScores/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,PhysicalActivityScore,BreakfastScore,FruitVegScore,SweetSnacksScore,FattyFoodsScore,TotalScore,HealthClassification,DateRecorded,UserId")] HealthScore healthScore)
        {
            if (id != healthScore.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(healthScore);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!HealthScoreExists(healthScore.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id", healthScore.UserId);
            return View(healthScore);
        }

        // GET: HealthScores/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var healthScore = await _context.HealthScores
                .Include(h => h.Child)
                    .ThenInclude(c => c.User) // ✅ Load the parent user
                .FirstOrDefaultAsync(m => m.Id == id);

            if (healthScore == null)
            {
                return NotFound();
            }

            return View(healthScore);
        }


        // POST: HealthScores/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var healthScore = await _context.HealthScores.FindAsync(id);
            if (healthScore != null)
            {
                _context.HealthScores.Remove(healthScore);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool HealthScoreExists(int id)
        {
            return _context.HealthScores.Any(e => e.Id == id);
        }


    }
}
