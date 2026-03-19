using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using WebApit4s.DAL;
using WebApit4s.DAL;
using WebApit4s.Identity;
using WebApit4s.Models;
using WebApit4s.Services;

public class AIGoalController : Controller
{
    private readonly TimeContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly GenAiGoalService _goalService;

    public AIGoalController(TimeContext context, UserManager<ApplicationUser> userManager, GenAiGoalService goalService)
    {
        _context = context; // ✅ Correct assignment
        _userManager = userManager;
        _goalService = goalService;
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
    public async Task<IActionResult> Generate()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return RedirectToAction("Login", "Account");

        // ✅ Check if AI Goals are enabled for this user
        if (!user.EnableAIGoals)
            return View("Disabled");

        var activeChildId = HttpContext.Session.GetInt32("ActiveChildId");

        if (activeChildId == null)
        {
            TempData["Error"] = "No active child selected. Please register or select a child first.";
            return RedirectToAction("LandingPage", "Dashboard");
        }

        var latestScore = await _context.HealthScores
            .Where(h => h.ChildId == activeChildId.Value)
            .OrderByDescending(h => h.DateRecorded)
            .FirstOrDefaultAsync();

        if (latestScore == null)
        {
            ViewBag.Message = "No Health Score found for the selected child.";
            return View("Error");
        }

        if (latestScore == null)
        {
            ViewBag.Message = "No Health Score found for the child.";
            return View("Error");
        }

        var categoryMap = new Dictionary<string, int>
    {
        { "Physical Activity", latestScore.PhysicalActivityScore },
        { "Breakfast", latestScore.BreakfastScore },
        { "Fruit and Vegetables", latestScore.FruitVegScore },
        { "Sweet Snacks", latestScore.SweetSnacksScore },
        { "Fatty Foods", latestScore.FattyFoodsScore }
    };

        var lowest = categoryMap.OrderBy(kv => kv.Value).First();
        var category = lowest.Key;
        var score = lowest.Value;
        var issue = category switch
        {
            "Physical Activity" => "Low physical activity",
            "Breakfast" => "Not having regular breakfast",
            "Fruit and Vegetables" => "Low fruit/vegetable intake",
            "Sweet Snacks" => "Frequent sugary snacks",
            "Fatty Foods" => "Frequent fatty food consumption",
            _ => "Needs improvement"
        };

        var child = await _context.Children.FindAsync(activeChildId.Value);

        if (child == null)
        {
            ViewBag.Message = "Child not found.";
            return View("Error");
        }

        int age = child.Age; // ✅ Assumes Age is a [NotMapped] computed property
        var goal = await _goalService.GeneratePersonalisedGoalAsync(category, issue, score, age);

        ViewBag.Goal = goal;
        ViewBag.Category = category;
        ViewBag.ChildName = child.ChildName;

        return View("Goal");
    }

}
