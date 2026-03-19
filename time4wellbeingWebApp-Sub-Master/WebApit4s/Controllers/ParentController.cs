using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebApit4s.DAL;
using WebApit4s.Identity;
using WebApit4s.Models;
using WebApit4s.ViewModels;

namespace WebApit4s.Controllers
{
    [Authorize(Roles = "Parent")]
    public class ParentController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly TimeContext _context;
        private readonly IEmailSender _emailSender;

        public ParentController(TimeContext context, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IEmailSender emailSender)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
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
        // Dashboard showing all children
        public async Task<IActionResult> Dashboard()
        {
            ViewData["ActivePage"] = "ParentDashboard";

            var userId = _userManager.GetUserId(User);
            var children = await _context.Children
                .Where(c => c.UserId == userId)
                .ToListAsync();

            return View(children); // Show cards, "Switch to Child", "View All Stats"
        }

        public async Task<IActionResult> ViewAllStats()
        {
            var userId = _userManager.GetUserId(User);
            var children = await _context.Children
                .Where(c => c.UserId == userId)
                .Include(c => c.WeeklyMeasurements)
                .Include(c => c.HealthScores)
                .ToListAsync();

            return View(children); // Chart stats per child or aggregated
        }

        public async Task<IActionResult> AssignTask(int childId)
        {
            var userId = _userManager.GetUserId(User);

            // 1) Ensure the child is registered under this parent
            //    NOTE: adjust "UserId" to your actual FK on Child if named differently (e.g., ParentId)
            var child = await _context.Children
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == childId && c.UserId == userId);

            if (child is null)
            {
                // Child not found or not owned by this parent
                return NotFound();
                // or: return Forbid();
            }

            // 2) Only the parent's own tasks (and active)
            var gameTasks = await _context.GameTasks
                .AsNoTracking()
                .Where(g => g.IsActive && g.CreatedByUserId == userId)
                .OrderByDescending(g => g.CreatedAt)
                .Select(g => new { g.Id, g.Title })
                .ToListAsync();

            // If you ALSO want to exclude tasks already assigned to this child, use this instead:
            // var assignedIds = _context.ChildGameTasks
            //     .Where(t => t.ChildId == childId)
            //     .Select(t => t.GameTaskId);
            // var gameTasks = await _context.GameTasks
            //     .AsNoTracking()
            //     .Where(g => g.IsActive && g.CreatedByUserId == userId && !assignedIds.Contains(g.Id))
            //     .OrderByDescending(g => g.CreatedAt)
            //     .Select(g => new { g.Id, g.Title })
            //     .ToListAsync();

            // 3) Existing assignments (for display in the view, if you show them)
            var assignedTasks = await _context.ChildGameTasks
                .AsNoTracking()
                .Include(t => t.GameTask)
                .Where(t => t.ChildId == childId)
                .ToListAsync();

            ViewBag.GameTasks = new SelectList(gameTasks, "Id", "Title");
            ViewBag.AssignedTasks = assignedTasks;
            ViewBag.Child = child;

            return View(new ChildGameTask
            {
                ChildId = childId,
                AssignedDate = DateTime.UtcNow // your view uses datetime-local; adjust if you prefer local time
            });
        }



        [HttpPost]
        public async Task<IActionResult> AssignTask(ChildGameTask model)
        {
            ModelState.Remove("Child");
            ModelState.Remove("GameTask");
            if (!ModelState.IsValid)
            {
                var gameTasks = await _context.GameTasks
                    .Where(g => g.IsActive)
                    .ToListAsync();

                var child = await _context.Children.FindAsync(model.ChildId);
                var assignedTasks = await _context.ChildGameTasks
                    .Include(t => t.GameTask)
                    .Where(t => t.ChildId == model.ChildId)
                    .ToListAsync();

                ViewBag.GameTasks = new SelectList(gameTasks, "Id", "Title", model.GameTaskId);
                ViewBag.Child = child;
                ViewBag.AssignedTasks = assignedTasks;

                return View(model); // Return the form with validation errors
            }

            model.ExpiryDate = model.IsRecurringDaily
                ? model.AssignedDate.Date.AddDays(1).AddTicks(-1)
                : model.AssignedDate.AddDays(7);

            _context.Add(model);
            await _context.SaveChangesAsync();

            // ✅ Proper redirect to avoid blank page
            return RedirectToAction(nameof(AssignTask), new { childId = model.ChildId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTask(int id, int childId)
        {
            var task = await _context.ChildGameTasks.FindAsync(id);
            if (task == null) return NotFound();

            _context.ChildGameTasks.Remove(task);
            await _context.SaveChangesAsync();

            return RedirectToAction("AssignTask", new { childId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkDeleteTasks(int childId, int[] taskIds)
        {
            if (taskIds == null || taskIds.Length == 0)
            {
                TempData["Info"] = "No tasks selected.";
                return RedirectToAction(nameof(AssignTask), new { childId });
            }

            var tasks = await _context.ChildGameTasks
                .Where(t => t.ChildId == childId && taskIds.Contains(t.Id))
                .ToListAsync();

            _context.ChildGameTasks.RemoveRange(tasks);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"{tasks.Count} task(s) deleted.";
            return RedirectToAction(nameof(AssignTask), new { childId });
        }


        // GET: Parent/CreateReward
        // GET: /Parent/CreateReward?childId=123 (optional)
        [HttpGet]
        public async Task<IActionResult> CreateReward(int? childId)
        {
            var parent = await _userManager.GetUserAsync(User);
            if (parent == null) return Unauthorized();

            var kids = await _context.Children
                .Where(c => c.UserId == parent.Id)
                .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.ChildName })
                .ToListAsync();

            var vm = new CreateParentRewardViewModel
            {
                Children = kids,
                ChildId = childId,
                IsCommon = !childId.HasValue
            };

            return View(vm);
        }

        // POST: /Parent/CreateReward
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateReward(CreateParentRewardViewModel vm)
        {
            var parent = await _userManager.GetUserAsync(User);
            if (parent == null) return Unauthorized();

            if (!vm.IsCommon && vm.ChildId == null)
                ModelState.AddModelError(nameof(vm.ChildId), "Please select a child or mark as common.");

            if (!ModelState.IsValid)
            {
                vm.Children = await _context.Children
                    .Where(c => c.UserId == parent.Id)
                    .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.ChildName })
                    .ToListAsync();

                return View(vm);
            }

            var reward = new ParentReward
            {
                ParentUserId = parent.Id,
                Title = vm.Title,
                Description = vm.Description,
                CoinCost = vm.CoinCost,
                RequiresParentApproval = vm.RequiresParentApproval,
                ValidFromUtc = vm.ValidFromUtc,
                ValidToUtc = vm.ValidToUtc,
                CooldownDaysPerChild = vm.CooldownDaysPerChild,
                IsActive = true,
                IsCommon = vm.IsCommon,
                ChildId = vm.IsCommon ? null : vm.ChildId
            };

            _context.ParentRewards.Add(reward);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Reward created.";
            return RedirectToAction(nameof(MyRewards));
        }


        // GET: /Parent/MyRewards
        [HttpGet]
        public async Task<IActionResult> MyRewards()
        {
            var parent = await _userManager.GetUserAsync(User);
            if (parent == null) return Unauthorized();

            var items = await _context.ParentRewards
                .Where(r => r.ParentUserId == parent.Id)
                .OrderByDescending(r => r.ValidFromUtc ?? DateTime.MinValue)
                .ToListAsync();

            // Build lookup of ChildId -> ChildName
            var childLookup = await _context.Children
                .Where(c => c.UserId == parent.Id)
                .ToDictionaryAsync(c => c.Id, c => c.ChildName);

            ViewBag.ChildLookup = childLookup;

            return View(items);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleRewardActive(int id)
        {
            var parent = await _userManager.GetUserAsync(User);
            var r = await _context.ParentRewards.FirstOrDefaultAsync(x => x.Id == id && x.ParentUserId == parent.Id);
            if (r == null) return NotFound();
            r.IsActive = !r.IsActive;
            await _context.SaveChangesAsync();
            TempData["Success"] = r.IsActive ? "Reward activated." : "Reward deactivated.";
            return RedirectToAction(nameof(MyRewards));
        }

        // GET: /Parent/EditReward/5
        [HttpGet]
        public async Task<IActionResult> EditReward(int id)
        {
            var parent = await _userManager.GetUserAsync(User);
            if (parent == null) return Unauthorized();

            var reward = await _context.ParentRewards
                .FirstOrDefaultAsync(r => r.Id == id && r.ParentUserId == parent.Id);

            if (reward == null) return NotFound();

            var kids = await _context.Children
                .Where(c => c.UserId == parent.Id)
                .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.ChildName })
                .ToListAsync();

            var vm = new CreateParentRewardViewModel
            {
                Title = reward.Title,
                Description = reward.Description,
                CoinCost = reward.CoinCost,
                RequiresParentApproval = reward.RequiresParentApproval,
                ValidFromUtc = reward.ValidFromUtc,
                ValidToUtc = reward.ValidToUtc,
                CooldownDaysPerChild = reward.CooldownDaysPerChild,
                IsCommon = reward.IsCommon,
                ChildId = reward.ChildId,
                Children = kids
            };

            ViewBag.RewardId = reward.Id; // For form action
            return View(vm);
        }

        // POST: /Parent/EditReward/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditReward(int id, CreateParentRewardViewModel vm)
        {
            var parent = await _userManager.GetUserAsync(User);
            if (parent == null) return Unauthorized();

            var reward = await _context.ParentRewards
                .FirstOrDefaultAsync(r => r.Id == id && r.ParentUserId == parent.Id);

            if (reward == null) return NotFound();

            if (!vm.IsCommon && vm.ChildId == null)
                ModelState.AddModelError(nameof(vm.ChildId), "Please select a child or mark as common.");

            if (!ModelState.IsValid)
            {
                vm.Children = await _context.Children
                    .Where(c => c.UserId == parent.Id)
                    .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.ChildName })
                    .ToListAsync();
                ViewBag.RewardId = id;
                return View(vm);
            }

            // Update fields
            reward.Title = vm.Title;
            reward.Description = vm.Description;
            reward.CoinCost = vm.CoinCost;
            reward.RequiresParentApproval = vm.RequiresParentApproval;
            reward.ValidFromUtc = vm.ValidFromUtc;
            reward.ValidToUtc = vm.ValidToUtc;
            reward.CooldownDaysPerChild = vm.CooldownDaysPerChild;
            reward.IsCommon = vm.IsCommon;
            reward.ChildId = vm.IsCommon ? null : vm.ChildId;

            _context.ParentRewards.Update(reward);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Reward updated.";
            return RedirectToAction(nameof(MyRewards));
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteReward(int id)
        {
            var parent = await _userManager.GetUserAsync(User);
            var r = await _context.ParentRewards.FirstOrDefaultAsync(x => x.Id == id && x.ParentUserId == parent.Id);
            if (r == null) return NotFound();
            _context.ParentRewards.Remove(r);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Reward deleted.";
            return RedirectToAction(nameof(MyRewards));
        }

        [HttpGet]
        public async Task<IActionResult> ApproveRedemptions(int? childId = null)
        {
            var parent = await _userManager.GetUserAsync(User);
            if (parent == null) return Unauthorized();

            var baseQuery = _context.ParentRewardRedemptions
                .Include(r => r.Child)
                .Include(r => r.ParentReward)
                .Where(r => r.ParentReward.ParentUserId == parent.Id);

            if (childId.HasValue)
            {
                baseQuery = baseQuery.Where(r => r.ChildId == childId.Value);
                var c = await _context.Children.AsNoTracking().FirstOrDefaultAsync(x => x.Id == childId.Value);
                ViewBag.ChildName = c?.ChildName;
            }

            var requested = await baseQuery
                .Where(r => r.Status == ParentReward.ParentRewardRedemptionStatus.Requested)
                .OrderBy(r => r.RequestedUtc)
                .ToListAsync();

            var approved = await baseQuery
                .Where(r => r.Status == ParentReward.ParentRewardRedemptionStatus.Approved)
                .OrderBy(r => r.RequestedUtc)
                .ToListAsync();

            var vm = new ParentRedemptionQueueVm
            {
                Requested = requested,
                Approved = approved,
                ChildName = ViewBag.ChildName as string
            };

            ViewData["ActivePage"] = "ApproveRedemptions";
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveRedemption(int id)
        {
            var parent = await _userManager.GetUserAsync(User);
            var redemption = await _context.ParentRewardRedemptions
                .Include(r => r.Child)
                .Include(r => r.ParentReward)
                .FirstOrDefaultAsync(r => r.Id == id && r.ParentReward.ParentUserId == parent.Id);

            if (redemption == null) return NotFound();
            if (redemption.Status != ParentReward.ParentRewardRedemptionStatus.Requested)
            {
                TempData["Error"] = "Only requested redemptions can be approved.";
                return RedirectToAction(nameof(ApproveRedemptions));
            }

            await using var tx = await _context.Database.BeginTransactionAsync();

            if (!redemption.CoinsDeducted)
            {
                if (redemption.Child.TotalPoints < redemption.CoinCostAtPurchase)
                {
                    TempData["Error"] = "Child doesn't have enough coins anymore.";
                    return RedirectToAction(nameof(ApproveRedemptions));
                }

                redemption.Child.TotalPoints -= redemption.CoinCostAtPurchase;
                redemption.CoinsDeducted = true;
            }

            redemption.Status = ParentReward.ParentRewardRedemptionStatus.Approved;
            redemption.ApprovedUtc = DateTime.UtcNow;

            if (!redemption.ParentReward.IsCommon && redemption.ParentReward.IsActive)
            {
                redemption.ParentReward.IsActive = false;
                _context.ParentRewards.Update(redemption.ParentReward);
            }

            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            TempData["Success"] = "Redemption approved.";
            return RedirectToAction(nameof(ApproveRedemptions));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkRedeemed(int id)
        {
            var parent = await _userManager.GetUserAsync(User);

            var redemption = await _context.ParentRewardRedemptions
                .Include(r => r.Child)
                .Include(r => r.ParentReward)
                .FirstOrDefaultAsync(r => r.Id == id && r.ParentReward.ParentUserId == parent.Id);

            if (redemption == null) return NotFound();

            if (redemption.Status != ParentReward.ParentRewardRedemptionStatus.Approved)
            {
                TempData["Error"] = "Only approved redemptions can be marked as redeemed.";
                return RedirectToAction(nameof(ApproveRedemptions));
            }

            redemption.Status = ParentReward.ParentRewardRedemptionStatus.Redeemed;
            redemption.RedeemedUtc = DateTime.UtcNow;

            _context.ParentRewardRedemptions.Update(redemption);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Reward marked as redeemed.";
            return RedirectToAction(nameof(ApproveRedemptions));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectRedemption(int id)
        {
            var parent = await _userManager.GetUserAsync(User);

            var redemption = await _context.ParentRewardRedemptions
                .Include(r => r.Child)
                .Include(r => r.ParentReward)
                .FirstOrDefaultAsync(r => r.Id == id && r.ParentReward.ParentUserId == parent.Id);

            if (redemption == null) return NotFound();

            if (redemption.Status != ParentReward.ParentRewardRedemptionStatus.Requested &&
                redemption.Status != ParentReward.ParentRewardRedemptionStatus.Approved)
            {
                TempData["Error"] = "Only requested or approved redemptions can be rejected.";
                return RedirectToAction(nameof(ApproveRedemptions));
            }

            // refund if coins were deducted
            if (redemption.CoinsDeducted && redemption.Child != null)
            {
                redemption.Child.TotalPoints += redemption.CoinCostAtPurchase;
                redemption.CoinsDeducted = false;

                _context.UserPointHistories.Add(new UserPointHistory
                {
                    UserId = redemption.Child.UserId,
                    ChildId = redemption.ChildId,
                    Delta = +redemption.CoinCostAtPurchase,
                    BalanceAfter = redemption.Child.TotalPoints,
                    Reason = PointChangeReason.RewardRejectedRefund,
                    SourceType = "ParentRewardRedemption",
                    SourceId = redemption.Id,
                    Notes = $"Refund for rejected reward: {redemption.ParentReward?.Title}"
                });
            }

            redemption.Status = ParentReward.ParentRewardRedemptionStatus.Rejected;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Redemption rejected.";
            return RedirectToAction(nameof(ApproveRedemptions));
        }


        // GET: /Parent/MyTasks
        [HttpGet]
        public async Task<IActionResult> MyTasks()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized();

            var items = await _context.GameTasks
                .Where(g => g.CreatedByUserId == user.Id)
                .OrderByDescending(g => g.CreatedAt)
                .ToListAsync();

            return View(items);
        }

        // GET: /Parent/TaskDetails/5  (optional details page)
        [HttpGet]
        public async Task<IActionResult> TaskDetails(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized();

            var task = await _context.GameTasks
                .FirstOrDefaultAsync(g => g.Id == id && g.CreatedByUserId == user.Id);

            if (task is null) return NotFound();

            return View(task);
        }

        [HttpGet]
        public IActionResult AddTask()
        {
            var vm = new CreateGameTaskViewModel
            {
                AllowCommonToggle = false,
                AllowActiveToggle = false
            };
            return View(vm);
        }

        // POST: /Parent/AddTask
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTask(CreateGameTaskViewModel vm)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized();

            if (!ModelState.IsValid)
                return View(vm);

            var entity = new GameTask
            {
                Title = vm.Title,
                Description = vm.Description,
                TargetArea = vm.TargetArea,
                SuggestedWeek = vm.SuggestedWeek,
                Points = vm.Points,
                MediaLink = vm.MediaLink,
                IsActive = true,               // parents can't deactivate here
                IsCommonTask = false,          // parent-created -> personal/custom
                CreatedByUserId = user.Id      // audit
                // CreatedAt set by DB default (GETUTCDATE)
            };

            _context.GameTasks.Add(entity);
            await _context.SaveChangesAsync();

            // Optional: notify parent (uncomment if you want confirmation emails)
            // if (!string.IsNullOrWhiteSpace(user.Email))
            // {
            //     await _emailSender.SendEmailAsync(
            //         user.Email!,
            //         "Task Created",
            //         $"Hi {user.UserName},<br/>Your task <b>{entity.Title}</b> has been created.");
            // }

            TempData["Success"] = "Task created.";
            return RedirectToAction(nameof(AddTask));

        }

            [HttpPost]
            [ValidateAntiForgeryToken]
            public async Task<IActionResult> DeleteGameTask(int id)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user is null) return Unauthorized();

                var task = await _context.GameTasks
                    .FirstOrDefaultAsync(g => g.Id == id && g.CreatedByUserId == user.Id);

                if (task is null)
                {
                    TempData["Error"] = "Task not found or you do not have permission to delete it.";
                    return RedirectToAction(nameof(MyTasks));
                }

                try
                {
                    _context.GameTasks.Remove(task);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Task deleted.";
                }
                catch (DbUpdateException)
                {
                    // Likely FK constraint if the task is already assigned elsewhere
                    TempData["Error"] = "This task is in use and cannot be deleted. Unassign it first.";
                }

                return RedirectToAction(nameof(MyTasks));
            }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteChild(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (userId is null) return Unauthorized();

            // 1) Load the child and make sure it belongs to this parent
            var child = await _context.Children
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

            if (child is null)
            {
                TempData["Error"] = "Child not found or you don't have permission to delete this child.";
                return RedirectToAction("Dashboard");
            }

            try
            {
                // 2) Delete dependent data that references this child (adjust to your schema)
                var tasks = _context.ChildGameTasks.Where(t => t.ChildId == id);
                _context.ChildGameTasks.RemoveRange(tasks);

                // If you have other child-scoped data, delete it here similarly, e.g.:
                // var notes = _context.AdminNotes.Where(n => n.ChildId == id);
                // _context.AdminNotes.RemoveRange(notes);
                //
                // var measurements = _context.WeeklyMeasurements.Where(m => m.ChildId == id);
                // _context.WeeklyMeasurements.RemoveRange(measurements);
                //
                // var healthScores = _context.HealthScores.Where(h => h.ChildId == id);
                // _context.HealthScores.RemoveRange(healthScores);

                // 3) Delete the child
                _context.Children.Remove(child);

                await _context.SaveChangesAsync();
                TempData["Success"] = $"Child '{child.ChildName}' was deleted.";
            }
            catch (DbUpdateException)
            {
                // FK still blocked by another related table
                TempData["Error"] = "This child has related records and cannot be deleted yet. Remove related data first.";
            }

            return RedirectToAction("Dashboard");
        }

    }
}
