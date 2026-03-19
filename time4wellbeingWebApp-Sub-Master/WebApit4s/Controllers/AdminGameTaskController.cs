using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApit4s.Models;
using Microsoft.AspNetCore.Identity;
using WebApit4s.DAL;
using WebApit4s.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;

public class AdminGameTaskController : Controller
{
    private readonly TimeContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminGameTaskController(TimeContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var tasks = await _context.GameTasks
    .Include(g => g.CreatedByUser)   // so CreatedByUser.Email shows up
    .OrderByDescending(g => g.CreatedAt)
    .ToListAsync();

        return View(tasks);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(GameTask task)
    {
        if (ModelState.IsValid)
        {
            _context.GameTasks.Add(task);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(task);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var task = await _context.GameTasks.FindAsync(id);
        if (task == null) return NotFound();
        return View(task);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, GameTask updated)
    {
        if (id != updated.Id) return BadRequest();

        if (ModelState.IsValid)
        {
            _context.GameTasks.Update(updated);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        return View(updated);
    }

    public async Task<IActionResult> Delete(int id)
    {
        var task = await _context.GameTasks.FindAsync(id);
        if (task == null) return NotFound();

        _context.GameTasks.Remove(task);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
