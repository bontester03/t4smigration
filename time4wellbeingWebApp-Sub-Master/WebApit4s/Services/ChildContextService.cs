using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebApit4s.DAL;
using WebApit4s.Identity;
using WebApit4s.Models;
using WebApit4s.Services;

public class ChildContextService : IChildContextService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly TimeContext _context;

    public ChildContextService(
        IHttpContextAccessor httpContextAccessor,
        UserManager<ApplicationUser> userManager,
        TimeContext context)
    {
        _httpContextAccessor = httpContextAccessor;
        _userManager = userManager;
        _context = context;
    }

    public async Task<string> GetUserIdAsync()
    {
        var user = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext!.User);
        return user?.Id ?? throw new UnauthorizedAccessException("User not logged in.");
    }

    public async Task<int?> GetActiveChildIdAsync()
    {
        var session = _httpContextAccessor.HttpContext?.Session;
        return session?.GetInt32("ActiveChildId");
    }

    public async Task<Child?> GetActiveChildAsync()
    {
        var userId = await GetUserIdAsync();
        var childId = await GetActiveChildIdAsync();

        if (childId == null)
            return null;

        return await _context.Children
            .Where(c => c.Id == childId && c.UserId == userId)
            .Include(c => c.HealthScores)
            .Include(c => c.WeeklyMeasurements)
            .FirstOrDefaultAsync();
    }
}
