using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebApit4s.DAL;
using WebApit4s.Identity;
using WebApit4s.Models;

public class AdminChildGameTaskController : Controller
{
    private readonly TimeContext _context;

    private readonly UserManager<ApplicationUser> _userManager;

    public AdminChildGameTaskController(TimeContext context, UserManager<ApplicationUser> userManager)
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
        }
        else
        {
            ViewBag.UserEmail = "Guest";
            ViewBag.ChildName = "Guest";
        }

        base.OnActionExecuting(context);
    }
    public async Task<IActionResult> Index()
    {
        


        var tasks = await _context.ChildGameTasks
            .Include(c => c.Child)
            .Include(g => g.GameTask)
            .ToListAsync();
        return View(tasks);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int childId, int? editTaskId = null)
    {
        var child = await _context.Children
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.Id == childId);

        if (child == null) return NotFound();

        // ✅ For showing child name, age, total points
        ViewBag.Child = child;

        // ✅ For task dropdown
        var gameTasks = await _context.GameTasks.ToListAsync();
        ViewBag.GameTasks = new SelectList(gameTasks, "Id", "Title");

        // ✅ For assigned tasks table
        var assignedTasks = await _context.ChildGameTasks
            .Include(x => x.GameTask)
            .Where(x => x.ChildId == childId)
            .OrderByDescending(x => x.AssignedDate)
            .ToListAsync();
        ViewBag.AssignedTasks = assignedTasks;

        // ✅ Determine Create vs Edit mode
        ViewBag.IsEditMode = editTaskId.HasValue;

        ChildGameTask model = new ChildGameTask
        {
            ChildId = childId,
            AssignedDate = DateTime.UtcNow
        };

        if (editTaskId.HasValue)
        {
            var existing = await _context.ChildGameTasks.FindAsync(editTaskId.Value);
            if (existing == null) return NotFound();
            model = existing;
        }

       

        return View(model);
    }





    [HttpPost]
    public async Task<IActionResult> Create(ChildGameTask model)
    {
        ModelState.Remove("Child");
        ModelState.Remove("GameTask");
        if (!ModelState.IsValid)
        {
            ViewBag.GameTasks = new SelectList(_context.GameTasks, "Id", "Title", model.GameTaskId);
            ViewBag.Child = await _context.Children.Include(c => c.User).FirstOrDefaultAsync(c => c.Id == model.ChildId);
            ViewBag.AssignedTasks = await _context.ChildGameTasks
                .Include(x => x.GameTask)
                .Where(x => x.ChildId == model.ChildId)
                .OrderByDescending(x => x.AssignedDate)
                .ToListAsync();
            ViewBag.IsEditMode = model.Id > 0;
            return View(model);
        }

        // ✅ Set ExpiryDate before saving
        if (model.IsRecurringDaily)
        {
            //model.ExpiryDate = DateTime.UtcNow.AddMinutes(1); // expires in 1 minutes

            model.ExpiryDate = model.AssignedDate.Date.AddDays(1).AddTicks(-1); // expires at 23:59:59 today
        }
        else
        {
            model.ExpiryDate = model.AssignedDate.AddDays(7); // or any default
        }

        model.IsGenerated = false; // Always false for manually assigned tasks


        if (model.Id > 0)
        {
            _context.Update(model);
        }
        else
        {
            _context.Add(model);
        }

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Create), new { childId = model.ChildId });
    }





    public async Task<IActionResult> Edit(int id)
    {

        var task = await _context.ChildGameTasks.FindAsync(id);
        if (task == null) return NotFound();
        ModelState.Remove("Child");
        ModelState.Remove("GameTask");
        ViewData["Children"] = new SelectList(_context.Children, "Id", "FullName", task.ChildId);
        ViewData["GameTasks"] = new SelectList(_context.GameTasks, "Id", "Title", task.GameTaskId);
        return View(task);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(ChildGameTask model)
    {
        if (ModelState.IsValid)
        {
            _context.ChildGameTasks.Update(model);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        ViewData["Children"] = new SelectList(_context.Children, "Id", "FullName", model.ChildId);
        ViewData["GameTasks"] = new SelectList(_context.GameTasks, "Id", "Title", model.GameTaskId);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, int childId)
    {
        var task = await _context.ChildGameTasks.FindAsync(id);
        if (task == null) return NotFound();

        _context.ChildGameTasks.Remove(task);
        await _context.SaveChangesAsync();

        return RedirectToAction("Create", new { childId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkDelete(string taskIdsCsv, int childId)
    {
        if (string.IsNullOrWhiteSpace(taskIdsCsv))
            return RedirectToAction("Create", new { childId });

        await _context.Database.ExecuteSqlRawAsync(
            "EXEC BulkDeleteChildGameTasks @ChildId = {0}, @TaskIds = {1}",
            childId,
            taskIdsCsv
        );

        return RedirectToAction("Create", new { childId });
    }



}
