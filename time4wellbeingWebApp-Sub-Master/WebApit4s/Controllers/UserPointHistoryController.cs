using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApit4s.DAL;
using WebApit4s.Identity;
using WebApit4s.Models;

namespace WebApit4s.Controllers
{
    [Authorize]
    public class UserPointHistoryController : Controller
    {
        private readonly TimeContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public UserPointHistoryController(TimeContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ✅ Admin View: See all users' point histories
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminIndex()
        {
            var data = await _context.UserPointHistories
                .Include(p => p.User)
                .Include(p => p.Child)
                .OrderByDescending(p => p.CreatedUtc)
                .ToListAsync();

            return View("AdminIndex", data);
        }

        // ✅ User View: See only their children's points
        public async Task<IActionResult> MyPoints()
        {
            var user = await _userManager.GetUserAsync(User);
            var activeChildId = HttpContext.Session.GetInt32("ActiveChildId");

            if (user == null || activeChildId == null)
            {
                TempData["Error"] = "No active child selected or user not found.";
                return RedirectToAction("Index", "Dashboard");
            }

            var data = await _context.UserPointHistories
                .Include(p => p.Child)
                .Where(p => p.UserId == user.Id && p.ChildId == activeChildId)
                .OrderByDescending(p => p.CreatedUtc)
                .ToListAsync();

            return View("MyPoints", data);
        }
    }
}
