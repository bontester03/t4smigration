using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using WebApit4s.DAL;
using WebApit4s.Identity;
using WebApit4s.Models;
using WebApit4s.ViewModels;

namespace WebApit4s.Controllers
{
    public class ChildGameTaskController : Controller
    {
        private readonly TimeContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ChildGameTaskController(TimeContext context, UserManager<ApplicationUser> userManager)
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

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            ViewData["ActivePage"] = "GameTask";


            var activeChildId = HttpContext.Session.GetInt32("ActiveChildId");

            if (activeChildId == null)
            {
                TempData["Error"] = "No active child selected.";
                return RedirectToAction("Index", "Dashboard");
            }

            var child = await _context.Children.FirstOrDefaultAsync(c => c.Id == activeChildId);
            if (child == null)
            {
                TempData["Error"] = "Child not found.";
                return RedirectToAction("Index", "Dashboard");
            }

            // ✅ Fetch existing assigned tasks
            var assignedTasks = await _context.ChildGameTasks
                .Include(cgt => cgt.GameTask)
                .Where(cgt => cgt.ChildId == activeChildId)
                .ToListAsync();

            var assignedGameTaskIds = assignedTasks.Select(t => t.GameTaskId).ToList();

            var commonTasks = await _context.GameTasks
                .Where(gt => gt.IsCommonTask && gt.IsActive && !assignedGameTaskIds.Contains(gt.Id))
                .ToListAsync();

            var virtualCommonTasks = commonTasks.Select(gt => new ChildGameTask
            {
                ChildId = child.Id,
                Child = child,
                GameTaskId = gt.Id,
                GameTask = gt,
                AssignedDate = DateTime.UtcNow
            }).ToList();

            var allTasks = assignedTasks.Concat(virtualCommonTasks).ToList();

            // ✅ Fetch point history
            var pointHistory = await _context.UserPointHistories
                .Include(p => p.Child)
                .Where(p => p.ChildId == activeChildId && p.UserId == child.UserId)
                .OrderByDescending(p => p.CreatedUtc)
                .ToListAsync();

            // ✅ Fetch available rewards (your code goes here)
            var now = DateTime.UtcNow;
            var rewards = await _context.ParentRewards
                .Where(r =>
                    r.ParentUserId == child.UserId &&
                    r.IsActive &&
                    (!r.ValidFromUtc.HasValue || r.ValidFromUtc <= now) &&
                    (!r.ValidToUtc.HasValue || r.ValidToUtc >= now) &&
                    (r.IsCommon || r.ChildId == child.Id) &&
                    !_context.ParentRewardRedemptions.Any(x =>
                        x.ParentRewardId == r.Id &&
                        x.ChildId == child.Id &&
                        x.Status != ParentReward.ParentRewardRedemptionStatus.Rejected &&
                        x.Status != ParentReward.ParentRewardRedemptionStatus.Cancelled
                    )
                )
                .OrderBy(r => r.CoinCost)
                .ToListAsync();

            var rewardsVm = new AvailableRewardsViewModel
            {
                ChildId = child.Id,
                ChildCoins = child.TotalPoints,
                Rewards = rewards
            };

            // ✅ Push everything to ViewBag
            ViewBag.ActiveChild = child;
            ViewBag.PointHistory = pointHistory;
            ViewBag.AvailableRewards = rewardsVm;

            return View(allTasks);
        }






        // Mark as completed
        // Mark as completed
        [HttpPost]
        public async Task<IActionResult> Complete(int id)
        {
            var task = await _context.ChildGameTasks
                .Include(t => t.GameTask)
                .Include(t => t.Child)
                .ThenInclude(c => c.User)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null)
                return NotFound();

            if (task.CompletedDate != null)
                return RedirectToAction(nameof(Index)); // Already completed

            task.CompletedDate = DateTime.UtcNow;

            // ✅ Update child's total points
            task.Child.TotalPoints += task.GameTask.Points;

            // ✅ Add points to history
            var history = new UserPointHistory
            {
                UserId = task.Child.UserId,          // Parent/owner
                ChildId = task.ChildId,
                Delta = task.GameTask.Points,        // +earned
                BalanceAfter = task.Child.TotalPoints,
                Reason = PointChangeReason.TaskCompleted,
                SourceType = "GameTask",
                SourceId = task.GameTask.Id,
                Notes = $"Completed task: {task.GameTask.Title}",
                CreatedUtc = DateTime.UtcNow
            };

            _context.UserPointHistories.Add(history);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }



    }

}
