using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebApit4s.DAL;
using WebApit4s.Models;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using WebApit4s.Identity;
using Microsoft.AspNetCore.Authorization;
using WebApit4s.ViewModels;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using WebApit4s.Utilities;






namespace WebApit4s.Controllers
{
    [Authorize(Roles = "Parent")]
    public class DashboardController : Controller
    {
        private readonly TimeContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IChildContextService _childContext;
        public DashboardController(TimeContext context, UserManager<ApplicationUser> userManager, IChildContextService childContext)
        {
            _context = context;
            _userManager = userManager;
            _childContext = childContext;
        }

     
        private static string? ExtractYouTubeId(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;
            try
            {
                var u = new Uri(url);
                var host = u.Host.ToLowerInvariant();
                if (host.Contains("youtube.com"))
                {
                    var query = System.Web.HttpUtility.ParseQueryString(u.Query);
                    var v = query["v"];
                    if (!string.IsNullOrWhiteSpace(v)) return v;
                    // /embed/VIDEOID or /shorts/VIDEOID
                    var parts = u.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 1 && (parts[0].Equals("embed", StringComparison.OrdinalIgnoreCase) ||
                                             parts[0].Equals("shorts", StringComparison.OrdinalIgnoreCase)))
                    {
                        return parts[1];
                    }
                }
                if (host.Contains("youtu.be"))
                {
                    var parts = u.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 0) return parts[^1];
                }
            }
            catch
            {
                // fall through
            }
            // bare id fallback
            var trimmed = url.Trim();
            return System.Text.RegularExpressions.Regex.IsMatch(trimmed, "^[a-zA-Z0-9_-]{6,}$") ? trimmed : null;
        }

        private static string YouTubeThumb(string youTubeUrl)
        {
            var id = ExtractYouTubeId(youTubeUrl);
            return id is null ? "" : $"https://img.youtube.com/vi/{id}/hqdefault.jpg";
        }

        private static IEnumerable<WebApit4s.Models.VideoCategory> MapActivitiesToCategories(IEnumerable<string> lowestActivities)
        {
            // Normalise to lower for matching
            var set = new HashSet<string>(lowestActivities.Select(a => a?.Trim().ToLowerInvariant() ?? ""), StringComparer.OrdinalIgnoreCase);

            // Tweak mappings to your questionnaire wording
            var result = new HashSet<WebApit4s.Models.VideoCategory>();

            foreach (var a in set)
            {
                if (a.Contains("physical") || a.Contains("exercise") || a.Contains("activity") || a.Contains("active"))
                    result.Add(WebApit4s.Models.VideoCategory.PhysicalActivity);

                if (a.Contains("breakfast"))
                    result.Add(WebApit4s.Models.VideoCategory.Breakfast);

                if (a.Contains("fruit") || a.Contains("veg") || a.Contains("vegetable"))
                    result.Add(WebApit4s.Models.VideoCategory.FruitVeg);

                if (a.Contains("sweet") || a.Contains("snack") || a.Contains("sweets"))
                    result.Add(WebApit4s.Models.VideoCategory.SweetSnacks);

                if (a.Contains("fat") || a.Contains("fatty") || a.Contains("fried") || a.Contains("takeaway"))
                    result.Add(WebApit4s.Models.VideoCategory.FattyFoods);
            }

            if (result.Count == 0)
                result.Add(WebApit4s.Models.VideoCategory.Miscellaneous);

            return result;
        }

        private static string CategoryDisplayName(WebApit4s.Models.VideoCategory c)
        {
            var mem = typeof(WebApit4s.Models.VideoCategory).GetMember(c.ToString()).FirstOrDefault();
            var attr = mem?.GetCustomAttribute<DisplayAttribute>();
            return attr?.GetName() ?? c.ToString();
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
                               : "/images/default-child-avatar.png";  // ? added avatar
            }
            else
            {
                ViewBag.UserEmail = "Guest";
                ViewBag.ChildName = "Guest";
                ViewBag.ChildAvatar = "/images/default-child-avatar.png"; // ? added avatar for guest
            }

            base.OnActionExecuting(context);
        }


        public async Task<IActionResult> LandingPage()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var userId = user.Id;

            // Check if ActiveChildId is not already in session
            var activeChildId = HttpContext.Session.GetInt32("ActiveChildId");

            if (activeChildId == null)
            {
                // Get the first registered child for this user (ordered by registration or Id)
                var firstChild = await _context.Children
                    .Where(c => c.UserId == userId && !c.IsDeleted)
                    .OrderBy(c => c.DateOfBirth) // or .OrderBy(c => c.Id) depending on logic
                    .FirstOrDefaultAsync();

                if (firstChild != null)
                {
                    activeChildId = firstChild.Id;
                    HttpContext.Session.SetInt32("ActiveChildId", firstChild.Id);
                }
            }

            var activeChild = await _context.Children.FirstOrDefaultAsync(c => c.Id == activeChildId);
            ViewBag.ActiveChildName = activeChild?.ChildName ?? "Your Child";


            // ? Fetch required consent logic
            var requiredQuestions = await _context.ConsentQuestions.Where(q => q.IsRequired).ToListAsync();
            var answeredCount = await _context.ConsentAnswers
                .Where(a => a.UserId == userId && requiredQuestions.Select(q => q.Id).Contains(a.ConsentQuestionId))
                .CountAsync();

            ViewBag.ShowConsentPopup = answeredCount < requiredQuestions.Count;
            ViewBag.ConsentQuestions = requiredQuestions;

            // ? Get all children
            var children = await _context.Children
                .Where(c => c.UserId == userId && !c.IsDeleted)
                .ToListAsync();

            // ? Redirect if no children
            if (!children.Any())
            {
                return RedirectToAction("Create", "Child");
            }

            // ? Set ActiveChildId in session if not already set
            if (!activeChildId.HasValue || children.All(c => c.Id != activeChildId.Value))
            {
                activeChildId = children.First().Id;
                HttpContext.Session.SetInt32("ActiveChildId", activeChildId.Value);
            }

            // ? Fetch dashboard data
            var healthScores = await _context.HealthScores
        .Where(h => h.UserId == userId && h.ChildId == activeChildId)
        .ToListAsync();

            var measurements = await _context.WeeklyMeasurements
                .Where(m => m.UserId == userId && m.ChildId == activeChildId)
                .ToListAsync();

            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            // fetch tasks with GameTask included
            var activeTasks = (await _context.ChildGameTasks
                .Include(cgt => cgt.GameTask)
                .Where(cgt => cgt.ChildId == activeChildId && cgt.GameTask.IsActive)
                .ToListAsync())
                .Where(cgt => !cgt.IsCompleted) // filter in memory
                .ToList();

            int activeTasksCount = activeTasks.Count;


            // ? Fetch available rewards
            var now = DateTime.UtcNow;
            var rewards = await _context.ParentRewards
                .Where(r =>
                    r.ParentUserId == userId &&
                    r.IsActive &&
                    (!r.ValidFromUtc.HasValue || r.ValidFromUtc <= now) &&
                    (!r.ValidToUtc.HasValue || r.ValidToUtc >= now) &&
                    (r.IsCommon || r.ChildId == activeChildId) &&
                    !_context.ParentRewardRedemptions.Any(x =>
                        x.ParentRewardId == r.Id &&
                        x.ChildId == activeChildId &&
                        x.Status != ParentReward.ParentRewardRedemptionStatus.Rejected &&
                        x.Status != ParentReward.ParentRewardRedemptionStatus.Cancelled
                    )
                )
                .OrderBy(r => r.CoinCost)
                .ToListAsync();

            var rewardsVm = new AvailableRewardsViewModel
            {
                ChildId = activeChildId.Value,
                ChildCoins = activeChild?.TotalPoints ?? 0,
                Rewards = rewards
            };


            // ? Build timeline activities
            var activities = new List<TimelineActivityViewModel>
    {
        new TimelineActivityViewModel
        {
            Timestamp = user.RegistrationDate.ToString("dd MMM yyyy HH:mm"),
            Activity = $"{user.Email} registered",
            Color = "bg-green"
        }
    };

            activities.AddRange(healthScores.Select(score => new TimelineActivityViewModel
            {
                Timestamp = score.DateRecorded.ToString("dd MMM yyyy HH:mm"),
                Activity = $"{user.Email} updated health score",
                Color = "bg-blue"
            }));

            activities.AddRange(measurements.Select(m => new TimelineActivityViewModel
            {
                Timestamp = m.DateRecorded.ToString("dd MMM yyyy HH:mm"),
                Activity = $"{user.Email} logged weight and height",
                Color = "bg-yellow"
            }));

            activities = activities.OrderByDescending(a => DateTime.Parse(a.Timestamp)).ToList();

            // ? Video of the Day
            var allVideos = GetAllVideoResources();
            var videoOfTheDay = allVideos[DateTime.Now.DayOfYear % allVideos.Count];
            int dayIndex = DateTime.UtcNow.Day % HealthQuotes.Count;
            ViewBag.QuoteOfTheDay = HealthQuotes[dayIndex];
            ViewBag.VideoOfTheDay = videoOfTheDay;
            ViewBag.TotalChildren = children.Count;
            ViewBag.TotalHealthScores = healthScores.Count;
            ViewBag.TotalWeightLogs = measurements.Count;
           

            // ? Return dashboard model
            var model = new UserDashboardViewModel
            {
                UserName = user.Email,
                RegistrationDate = user.RegistrationDate,
                Children = children,
                Measurements = measurements,
                HealthScores = healthScores,
                RecentActivities = activities,
                Notifications = notifications,
                AvailableRewards = rewardsVm ,// ?? attach rewards
                   ActiveTasksCount = activeTasksCount
            };

           

            return View(model);
        }


        private List<ResourceViewModel> GetAllVideoResources()
        {
            return new List<ResourceViewModel>
            {
                new ResourceViewModel { Title = "5 Ways to Wellbeing", Url = "https://www.youtube.com/watch?v=WHcLb0h9FmI", Image = "wellbeingways.png" },
                new ResourceViewModel { Title = "Overcoming Barriers Part 1", Url = "https://www.youtube.com/watch?v=acxBR2K6dhs", Image = "wellbeingways.png" },
                new ResourceViewModel { Title = "Fats and Sugars Part 1", Url = "https://www.youtube.com/watch?v=R0O51IUtmH4", Image = "wellbeingways.png" },
                new ResourceViewModel { Title = "Healthy Eating Recipes Lunch", Url = "https://www.youtube.com/watch?v=j4RFxo1sXbU", Image = "wellbeingways.png" },
                new ResourceViewModel { Title = "Making Sense Of Labels", Url = "https://www.youtube.com/watch?v=NRkmaL7mULE", Image = "wellbeingways.png" }
            };
        }


        [HttpGet]
        public async Task<IActionResult> Notifications()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return RedirectToAction("Login", "Home");
            }

            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new UserNotificationViewModel
                {
                    Id = n.Id,
                    Message = string.IsNullOrEmpty(n.Message) ? "No message available" : n.Message,
                    CreatedAt = n.CreatedAt,
                    IsRead = n.IsRead
                })
                .ToListAsync();

            return View(notifications);
        }



        // ? Mark Notification as Read (User)
        [HttpPost]
        [ValidateAntiForgeryToken] // ? CSRF Protection
        public async Task<IActionResult> MarkNotificationAsRead(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return RedirectToAction("Login", "Home");
            }

            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

            if (notification != null)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Notifications");
        }


        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            int? activeChildId = HttpContext.Session.GetInt32("ActiveChildId");

            Child? child = null;
            if (activeChildId.HasValue)
            {
                child = await _context.Children
                    .FirstOrDefaultAsync(c => c.Id == activeChildId.Value && c.UserId == user.Id && !c.IsDeleted);
            }

            // Fallback to latest child if no session set
            if (child == null)
            {
                child = await _context.Children
                    .Where(c => c.UserId == user.Id && !c.IsDeleted)
                    .OrderByDescending(c => c.CreatedAt)
                    .FirstOrDefaultAsync();
            }

            var personalDetails = await _context.PersonalDetails
                .FirstOrDefaultAsync(p => p.UserId == user.Id);

            var referralType = await _context.ReferralTypes
                .FirstOrDefaultAsync(r => r.Id == user.ReferralTypeId);

            var model = new DashboardViewModel
            {
                ChildData = child,
                PersonalDetailsData = personalDetails,
                HasChildData = child != null,
                HasPersonalDetails = personalDetails != null,
                IsSelfReferral = referralType?.Category == ReferralCategory.SelfReferral,
                Schools = await _context.Schools.ToListAsync(),
                Classes = await _context.Classes.ToListAsync()
            };

            return View(model);
        }



        [HttpGet]
        public async Task<IActionResult> SelectChild()
        {
            var userId = _userManager.GetUserId(User);
            var children = await _context.Children
                .Where(c => c.UserId == userId)
                .ToListAsync();

            return View(children); // Pass to SelectChild.cshtml
        }


        [HttpPost]
        public IActionResult SetActiveChild(int childId)
        {
            HttpContext.Session.SetInt32("ActiveChildId", childId);
            return RedirectToAction("LandingPage", "Dashboard");
        }


        //addmultiplechild
        public IActionResult AddChildWithScore()
        {
            var vm = new ChildWithScoreViewModel();

            // Default all dropdowns to -1 ? "Select..."
            vm.HealthScore.PhysicalActivityScore = -1;
            vm.HealthScore.BreakfastScore = -1;
            vm.HealthScore.FruitVegScore = -1;
            vm.HealthScore.SweetSnacksScore = -1;
            vm.HealthScore.FattyFoodsScore = -1;

            return View(vm);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddChildWithScore(ChildWithScoreViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            // Save child
            var child = model.Child;
            child.UserId = user.Id;
            child.DateOfBirth = DateTimeUtils.EnsureUtc(child.DateOfBirth);
            child.LastLogin = DateTime.UtcNow;
            child.CreatedAt = DateTime.UtcNow;
            child.UpdatedAt = DateTime.UtcNow;
            child.ChildGuid = child.ChildGuid == Guid.Empty ? Guid.NewGuid() : child.ChildGuid;
            child.IsDeleted = false;
            _context.Children.Add(child);
            await _context.SaveChangesAsync();

            // Save health score
            var score = model.HealthScore;
            score.ChildId = child.Id;
            score.UserId = user.Id;

            // Scoring logic
            int physical = score.PhysicalActivityScore switch { 0 => 1, 1 => 2, 2 => 3, 3 => 4, 4 => 5, _ => 0 };
            int breakfast = score.BreakfastScore switch { 0 => 1, 1 => 2, 2 => 3, 3 => 4, 4 => 5, _ => 0 };
            int fruitVeg = score.FruitVegScore switch { 0 => 1, 1 => 2, 2 => 3, 3 => 4, 4 => 5, _ => 0 };
            int sweet = score.SweetSnacksScore switch { 0 => 1, 1 => 2, 2 => 3, 3 => 4, 4 => 5, _ => 0 };
            int fatty = score.FattyFoodsScore switch { 0 => 1, 1 => 2, 2 => 3, 3 => 4, 4 => 5, _ => 0 };

            score.TotalScore = physical + breakfast + fruitVeg + sweet + fatty;
            score.HealthClassification = score.TotalScore >= 15 ? "Healthy" : "Unhealthy";

            _context.HealthScores.Add(score);
            await _context.SaveChangesAsync();

            HttpContext.Session.SetInt32("ActiveChildId", child.Id);

            return RedirectToAction("LandingPage", "Dashboard");
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateChild(Child model)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return RedirectToAction("Login", "Home");
            }

            if (!ModelState.IsValid)
            {
                TempData["ChildCreateError"] = "Invalid child details submitted.";
                return RedirectToAction("Index");
            }

            var exists = await _context.Children.AnyAsync(c => c.UserId == userId);
            if (exists)
            {
                TempData["ChildCreateError"] = "Child details already exist for this user.";
                return RedirectToAction("Index");
            }

            model.UserId = userId;
            model.DateOfBirth = DateTimeUtils.EnsureUtc(model.DateOfBirth);
            model.LastLogin = DateTime.UtcNow;
            model.CreatedAt = DateTime.UtcNow;
            model.UpdatedAt = DateTime.UtcNow;
            model.ChildGuid = model.ChildGuid == Guid.Empty ? Guid.NewGuid() : model.ChildGuid;
            model.IsDeleted = false;
            _context.Children.Add(model);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePersonalDetails(PersonalDetails model)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return RedirectToAction("Login", "Home");
            }

            if (!ModelState.IsValid)
            {
                TempData["PersonalDetailsCreateError"] = "Invalid personal details submitted.";
                return RedirectToAction("Index");
            }

            // Assign Identity userId to model
            model.UserId = userId;

            var exists = await _context.PersonalDetails.AnyAsync(p => p.UserId == userId);
            if (exists)
            {
                return RedirectToAction("Index");
            }

            _context.PersonalDetails.Add(model);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }





        // Generate suggestions based on historical data
        private Dictionary<string, string> GenerateTrackingGoals(HealthScore latestScore)
        {
            var goals = new Dictionary<string, string>();

            if (latestScore.PhysicalActivityScore < 4)
            {
                goals["Physical Activity"] = "Increase your physical activity to at least 420+ minutes per week.";
            }

            if (latestScore.BreakfastScore < 4)
            {
                goals["Breakfast"] = "Try to have breakfast every day of the week.";
            }

            if (latestScore.FruitVegScore < 4)
            {
                goals["Fruit/Veg Intake"] = "Aim to eat at least 5 portions of fruit and vegetables daily.";
            }

            if (latestScore.SweetSnacksScore < 4)
            {
                goals["Sweet Snacks"] = "Reduce sweet snacks to less than once per week.";
            }

            if (latestScore.FattyFoodsScore < 4)
            {
                goals["Fatty Foods"] = "Limit fatty foods to less than once per week.";
            }

            return goals;
        }



        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Goal()
        {
            var userId = _userManager.GetUserId(User);
            var activeChildId = HttpContext.Session.GetInt32("ActiveChildId");

            if (string.IsNullOrEmpty(userId) || activeChildId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            ViewData["ActivePage"] = "Goal";

            // Fetch all health scores for the active child of the logged-in user
            var userScores = await _context.HealthScores
                                           .Where(h => h.UserId == userId && h.ChildId == activeChildId)
                                           .OrderByDescending(h => h.DateRecorded)
                                           .ToListAsync();

            if (userScores == null || userScores.Count == 0)
            {
                ViewBag.Message = "No health scores found. Please complete a health questionnaire to start tracking your goals.";
                return View(null);
            }

            // Get the latest entry
            var latestScore = userScores.First();

            // Generate tracking goals based on the latest entry
            var goals = GenerateTrackingGoals(latestScore);

            ViewBag.UserScores = userScores;

            return View(goals);
        }




        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Questionnaire()
        {
            var userId = _userManager.GetUserId(User);
            var activeChildId = HttpContext.Session.GetInt32("ActiveChildId");

            if (string.IsNullOrEmpty(userId) || activeChildId == null)
            {
                return RedirectToAction("Login", "Account"); // Or handle missing child context
            }

            var submittedScores = await _context.HealthScores
                .Where(h => h.UserId == userId && h.ChildId == activeChildId.Value)
                .OrderByDescending(h => h.DateRecorded)
                .ToListAsync();

            var viewModel = new HealthScoreViewModel
            {
                NewScore = new HealthScore
                {
                    UserId = userId,
                    ChildId = activeChildId.Value,
                    PhysicalActivityScore = -1,
                    BreakfastScore = -1,
                    FruitVegScore = -1,
                    SweetSnacksScore = -1,
                    FattyFoodsScore = -1
                },
                SubmittedScores = submittedScores // ? Set this!
            };

            return View(viewModel);
        }




        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> CreateQuestionnaire(HealthScoreViewModel viewModel)
        {
            var userId = _userManager.GetUserId(User);
            var activeChildId = HttpContext.Session.GetInt32("ActiveChildId");

            if (string.IsNullOrEmpty(userId) || activeChildId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            viewModel.NewScore.UserId = userId;
            viewModel.NewScore.ChildId = activeChildId.Value;
            ModelState.Remove("NewScore.Child");

            if (ModelState.IsValid)
            {
                // --- CHECK WEEKLY SUBMISSION ---
                var today = DateTime.UtcNow.Date;
                var startOfWeek = today.AddDays(-(int)today.DayOfWeek); // Sunday start
                var endOfWeek = startOfWeek.AddDays(7);

                var alreadySubmitted = await _context.HealthScores
                    .AnyAsync(h => h.ChildId == activeChildId.Value
                                && h.DateRecorded >= startOfWeek
                                && h.DateRecorded < endOfWeek);

                if (alreadySubmitted)
                {
                    TempData["PopupMessage"] = "You are not allowed to submit again this week. Please wait until next week.";
                    return RedirectToAction(nameof(Questionnaire));
                }


                // --- CALCULATE SCORES ---
                int physicalActivityScore = viewModel.NewScore.PhysicalActivityScore switch { 0 => 1, 1 => 2, 2 => 3, 3 => 4, 4 => 5, _ => 0 };
                int breakfastScore = viewModel.NewScore.BreakfastScore switch { 0 => 1, 1 => 2, 2 => 3, 3 => 4, 4 => 5, _ => 0 };
                int fruitVegScore = viewModel.NewScore.FruitVegScore switch { 0 => 1, 1 => 2, 2 => 3, 3 => 4, 4 => 5, _ => 0 };
                int sweetSnacksScore = viewModel.NewScore.SweetSnacksScore switch { 0 => 1, 1 => 2, 2 => 3, 3 => 4, 4 => 5, _ => 0 };
                int fattyFoodsScore = viewModel.NewScore.FattyFoodsScore switch { 0 => 1, 1 => 2, 2 => 3, 3 => 4, 4 => 5, _ => 0 };

                viewModel.NewScore.TotalScore = physicalActivityScore + breakfastScore + fruitVegScore + sweetSnacksScore + fattyFoodsScore;
                viewModel.NewScore.HealthClassification = viewModel.NewScore.TotalScore >= 15 ? "Healthy" : "Unhealthy";
                viewModel.NewScore.DateRecorded = today; // ensure consistent date

                _context.HealthScores.Add(viewModel.NewScore);
                await _context.SaveChangesAsync();

                // --- REWARD LOGIC ---
                const int rewardCoins = 50;
                var child = await _context.Children.FirstOrDefaultAsync(c => c.Id == activeChildId.Value);

                if (child != null)
                {
                    child.TotalPoints += rewardCoins;
                    _context.Children.Update(child);
                    await _context.SaveChangesAsync();

                    _context.UserPointHistories.Add(new UserPointHistory
                    {
                        UserId = userId,
                        ChildId = child.Id,
                        Delta = rewardCoins,
                        BalanceAfter = child.TotalPoints,
                        Reason = PointChangeReason.QuizCompleted, // or QuestionnaireCompleted if you add
                        SourceType = "HealthScore",
                        SourceId = viewModel.NewScore.Id,
                        Notes = "Weekly Questionnaire Reward",
                        CreatedUtc = DateTime.UtcNow
                    });
                    await _context.SaveChangesAsync();
                }

                return RedirectToAction(nameof(Questionnaire));
            }

            // Load only scores for selected child
            viewModel.SubmittedScores = await _context.HealthScores
                .Where(h => h.UserId == userId && h.ChildId == activeChildId.Value)
                .ToListAsync();

            return View("Questionnaire", viewModel);
        }





        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Measurement()
        {
            ViewData["ActivePage"] = "Measurement";

            string? userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _userManager.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.ReferralTypeId = user.ReferralTypeId;

            // ? Get ActiveChildId from session
            int? activeChildId = HttpContext.Session.GetInt32("ActiveChildId");
            if (activeChildId == null)
            {
                TempData["Error"] = "Please select a child first.";
                return RedirectToAction("LandingPage");
            }

            // ? Decide whether to render NHS BMI widget based on ReferralType.Category
            bool canUseNhsBmi = true; // default: show
            if (user.ReferralTypeId.HasValue)
            {
                var category = await _context.ReferralTypes
                    .AsNoTracking()
                    .Where(rt => rt.Id == user.ReferralTypeId.Value)
                    .Select(rt => (ReferralCategory?)rt.Category)
                    .FirstOrDefaultAsync();

                // Hide if School, show otherwise (including null/not found)
                canUseNhsBmi = category != ReferralCategory.School;
            }
            ViewBag.CanUseNhsBmi = canUseNhsBmi;

            var twelveWeeksAgo = DateTime.Today.AddDays(-84);

            // ? Filter by ChildId and last 12 weeks
            var measurements = await _context.WeeklyMeasurements
                .AsNoTracking()
                .Where(w => w.UserId == userId &&
                            w.ChildId == activeChildId &&
                            w.DateRecorded >= twelveWeeksAgo)
                .OrderBy(w => w.DateRecorded)
                .ToListAsync();

            // ? Chart data
            var chartData = measurements.Select(m => new
            {
                date = m.DateRecorded.ToString("dd MMM"),
                height = m.Height,
                weight = m.Weight,
                centileScore = m.CentileScore
            });

            ViewBag.ChartData = JsonSerializer.Serialize(chartData);

            return View(measurements);
        }




        public ActionResult Notification()
        {
            ViewData["ActivePage"] = "Notification";
            return View();
        }

  
public async Task<IActionResult> Suggestions()
    {
        var userId = _userManager.GetUserId(User);
        var activeChildId = HttpContext.Session.GetInt32("ActiveChildId");

        if (string.IsNullOrEmpty(userId) || activeChildId == null)
        {
            return RedirectToAction("Login", "Home");
        }

        ViewData["ActivePage"] = "Suggestions";

        // Latest score for the selected child
        var latestScore = await _context.HealthScores
            .Where(h => h.UserId == userId && h.ChildId == activeChildId.Value)
            .OrderByDescending(h => h.DateRecorded)
            .FirstOrDefaultAsync();

        if (latestScore == null)
        {
            ViewBag.Message = "No health scores found. Complete a questionnaire to get suggestions.";
            return View(new List<VideoSuggestionItemVM>());
        }

        // Your existing logic
        var lowestActivities = GetLowestScoringActivities(latestScore); // returns List<string>
        var categories = MapActivitiesToCategories(lowestActivities).ToList();

        // Pull active rewards that match those categories
        // You can tune ordering: by CoinValue desc, newest first, etc.
        var videosQuery = _context.VideoRewards
            .AsNoTracking()
            .Where(v => v.IsActive && categories.Contains(v.Category));

        // De-dupe and limit (e.g., up to 12 suggestions)
        var videos = await videosQuery
            .OrderByDescending(v => v.CoinValue)
            .ThenByDescending(v => v.Id)
            .Take(12)
            .ToListAsync();

        // Build VM for the view
        var vm = videos.Select(v => new VideoSuggestionItemVM
        {
            Id = v.Id,
            Title = v.Title,
            YouTubeUrl = v.YouTubeUrl,
            ThumbnailUrl = YouTubeThumb(v.YouTubeUrl),
            CoinValue = v.CoinValue,
            Category = v.Category
        }).ToList();

        ViewBag.LowestActivities = string.Join(", ", lowestActivities);
        return View(vm);
    }



        private List<string> GetLowestScoringActivities(HealthScore score)
        {
            // Create a dictionary to store activity names and their scores
            var scores = new Dictionary<string, int>
    {
        { "Physical Activity", score.PhysicalActivityScore },
        { "Breakfast", score.BreakfastScore },
        { "Fruit and Veg", score.FruitVegScore },
        { "Sweet Snacks", score.SweetSnacksScore },
        { "Fatty Foods", score.FattyFoodsScore }
    };

            // Find the minimum score
            int lowestScore = scores.Values.Min();

            // Return all activities with the lowest score
            return scores.Where(s => s.Value == lowestScore).Select(s => s.Key).ToList();
        }


        private List<ResourceViewModel> GetResourcesForActivity(string activity)
        {
            return activity switch
            {
                "Physical Activity" => new List<ResourceViewModel>
        {
            new ResourceViewModel { Title = "How Active Have You Been?", Url = "<iframe width=\"560\" height=\"315\" src=\"https://www.youtube.com/embed/nTrfGo3sIiU\" title=\"Time4Sport App Walkthrough Video3 How active have you been SUBTITLES\" frameborder=\"0\" allow=\"accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share\" referrerpolicy=\"strict-origin-when-cross-origin\" allowfullscreen></iframe>", Image = "wellbeingways.png" }
        },
                "Breakfast" => new List<ResourceViewModel>
        {
            new ResourceViewModel { Title = "Breakfast Days", Url = "<iframe width=\"560\" height=\"315\" src=\"https://www.youtube.com/embed/7JYkJ-rGxiQ\" title=\"Time4Sport App Walkthrough Video6 Breakfast days SUBTITLES\" frameborder=\"0\" allow=\"accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share\" referrerpolicy=\"strict-origin-when-cross-origin\" allowfullscreen></iframe>", Image = "wellbeingways.png" }
        },
                "Fruit and Veg" => new List<ResourceViewModel>
        {
            new ResourceViewModel { Title = "Healthy Eating Recipes Lunch", Url = "<iframe width=\"560\" height=\"315\" src=\"https://www.youtube.com/embed/j4RFxo1sXbU\" title=\"Time4Sport App Healthy Eating Recipes Lunch V1 SUBTITLED\" frameborder=\"0\" allow=\"accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share\" referrerpolicy=\"strict-origin-when-cross-origin\" allowfullscreen></iframe>", Image = "wellbeingways.png" }
        },
                "Sweet Snacks" => new List<ResourceViewModel>
        {
            new ResourceViewModel { Title = "Reduce Unhealthy Snacks", Url = "<iframe width=\"560\" height=\"315\" src=\"https://www.youtube.com/embed/-rTCF4tGBzQ\" title=\"Time4Sport App Walkthrough Video2 Reduce unhealthy snacks SUBTITLES\" frameborder=\"0\" allow=\"accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share\" referrerpolicy=\"strict-origin-when-cross-origin\" allowfullscreen></iframe>", Image = "wellbeingways.png" }
        },
                "Fatty Foods" => new List<ResourceViewModel>
        {
            new ResourceViewModel { Title = "Fats and Sugars Part 1", Url = "<iframe width=\"560\" height=\"315\" src=\"https://www.youtube.com/embed/R0O51IUtmH4\" title=\"Time4Sport App Videos Fats And Sugars pt1 V1 SUBTITLED\" frameborder=\"0\" allow=\"accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share\" referrerpolicy=\"strict-origin-when-cross-origin\" allowfullscreen></iframe>", Image = "wellbeingways.png" }
        },
                _ => new List<ResourceViewModel>()
            };
        }



        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetUserNotifications()
        {
            string? userId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "User not logged in." });
            }

            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .OrderByDescending(n => n.CreatedAt)
                .Take(5)
                .Select(n => new
                {
                    id = n.Id,
                    message = string.IsNullOrWhiteSpace(n.Message) ? "No message available" : n.Message,
                    createdAt = n.CreatedAt.ToString("dd MMM yyyy HH:mm"),
                    isRead = n.IsRead
                })
                .ToListAsync();

            return Json(new { success = true, notifications });
        }



        [Authorize]
        public async Task<IActionResult> MyCompletionStatus()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var appUser = await _context.Users
                .Include(u => u.ReferralType)
                .FirstOrDefaultAsync(u => u.Id == user.Id);

            if (appUser == null) return NotFound();

            var children = await _context.Children
                .Where(c => c.UserId == user.Id && !c.IsDeleted)
                .ToListAsync();

            var perChildStatuses = new List<ChildCompletionStatusViewModel>();
            bool hasPersonalDetails = await _context.PersonalDetails
    .AnyAsync(p => p.UserId == appUser.Id);


            foreach (var child in children)
            {
                var childStatus = new ChildCompletionStatusViewModel
                {
                    ChildName = child.ChildName,
                    DateOfBirth = child.DateOfBirth,
                    HasPersonalDetails = hasPersonalDetails, // ? same for all children of this user
                    HasHealthScores = await _context.HealthScores.AnyAsync(h => h.ChildId == child.Id),
                    HasMeasurements = await _context.WeeklyMeasurements.AnyAsync(m => m.ChildId == child.Id)
                };

                perChildStatuses.Add(childStatus);
            }


            var viewModel = new AdminUserStatusViewModel
            {
                UserId = appUser.Id,
                Email = appUser.Email,
                ReferralType = appUser.ReferralType?.Name ?? "N/A",
                RegistrationDate = appUser.RegistrationDate,
                ChildStatuses = perChildStatuses
            };

            return View(viewModel);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitConsentFromPopup(ConsentFormViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            if (model.Responses == null || model.Responses.Any(r => string.IsNullOrEmpty(r.Answer)))
            {
                TempData["Error"] = "Please answer all required questions.";
                return RedirectToAction("LandingPage");
            }

            foreach (var response in model.Responses)
            {
                _context.ConsentAnswers.Add(new ConsentAnswer
                {
                    UserId = user.Id,
                    ConsentQuestionId = response.QuestionId,
                    Answer = response.Answer,
                    SubmittedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("LandingPage");
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Feedback()
        {

            ViewData["ActivePage"] = "Feedback";
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            ViewBag.Categories = new List<string> { "Registration", "Staff", "Overall", "Usability", "Support" };

            var feedbacks = await _context.Feedbacks
                .Where(f => f.UserId == user.Id)
                .OrderByDescending(f => f.SubmittedAt)
                .ToListAsync();

            return View(feedbacks); // Pass list to the view
        }


        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Feedback(Feedback model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            // Manually assign user ID before validation
            model.UserId = user.Id;

            ModelState.Remove("UserId"); // prevent model binding error
            ModelState.Remove("User");


            if (!ModelState.IsValid)
            {
                ViewBag.Categories = new List<string> { "Registration", "Staff", "Overall", "Usability", "Support" };
                return View(model);
            }

            _context.Feedbacks.Add(model);
            await _context.SaveChangesAsync();

            TempData["FeedbackSuccess"] = "Thank you! Your feedback has been submitted.";
            return RedirectToAction("Feedback");
        }

        [Authorize]
        public async Task<IActionResult> MedicalHistory()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var activeChildId = HttpContext.Session.GetInt32("ActiveChildId");
            if (activeChildId == null)
            {
                TempData["Error"] = "No child selected. Please complete child registration.";
                return RedirectToAction("LandingPage");
            }

            var record = await _context.MedicalRecords
                .Include(m => m.Child)
                .FirstOrDefaultAsync(m => m.ChildId == activeChildId.Value);

            if (record == null)
            {
                TempData["Info"] = "No medical record found for the selected child.";
                return RedirectToAction("CreateMedicalRecord");
            }

            return View(record);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateMedicalRecord(MedicalRecord model)
        {
            if (!ModelState.IsValid)
            {
                model.Child = await _context.Children.FindAsync(model.ChildId);
                return View("MedicalHistory", model);
            }

            var record = await _context.MedicalRecords.FindAsync(model.Id);
            if (record == null) return NotFound();

            record.GPPracticeName = model.GPPracticeName;
            record.GPContactNumber = model.GPContactNumber;
            record.MedicalConditions = model.MedicalConditions;
            record.Allergies = model.Allergies;
            record.Medications = model.Medications;
            record.AdditionalNotes = model.AdditionalNotes;
            record.IsSensitive = model.IsSensitive;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Medical record updated.";
            return RedirectToAction("MedicalHistory");
        }

        [HttpGet]
        public async Task<IActionResult> CreateMedicalRecord()
        {
            var user = await _userManager.GetUserAsync(User);
            var childId = HttpContext.Session.GetInt32("ActiveChildId");

            if (user == null || childId == null)
                return RedirectToAction("Index", "Dashboard");

            var existingRecord = await _context.MedicalRecords.FirstOrDefaultAsync(m => m.ChildId == childId);
            if (existingRecord != null)
                return RedirectToAction("MedicalHistory"); // Already exists

            var child = await _context.Children.FindAsync(childId);
            if (child == null) return NotFound();

            var model = new MedicalRecord
            {
                ChildId = child.Id,
                Child = child
            };

            ViewBag.ChildName = child.ChildName;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateMedicalRecord(MedicalRecord model)
        {
            if (!ModelState.IsValid)
            {
                model.Child = await _context.Children.FindAsync(model.ChildId);
                ViewBag.ChildName = model.Child?.ChildName ?? "Child";
                return View(model);
            }

            model.CreatedAt = DateTime.UtcNow;
            _context.MedicalRecords.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Medical record added successfully.";
            return RedirectToAction("MedicalHistory");
        }


        private static readonly List<string> HealthQuotes = new()
{
    "Take care of your body. It's the only place you have to live. – Jim Rohn",
    "Health is a state of complete harmony of the body, mind, and spirit. – B.K.S. Iyengar",
    "A healthy outside starts from the inside. – Robert Urich",
    "Your body deserves the best. – Anonymous",
    "It’s not about being the best. It’s about being better than you were yesterday. – Anonymous",
    "To keep the body in good health is a duty. – Buddha",
    "Eat well, move daily, hydrate often, sleep lots, love your body. – Anonymous"
};


        [HttpGet]
        public async Task<IActionResult> AvailableRewards()
        {
            var activeChildId = HttpContext.Session.GetInt32("ActiveChildId");
            if (activeChildId == null)
            {
                TempData["Error"] = "No active child selected.";
                return RedirectToAction("Index", "Dashboard");
            }

            var child = await _context.Children
                .FirstOrDefaultAsync(c => c.Id == activeChildId.Value);

            if (child == null)
            {
                TempData["Error"] = "Child not found.";
                return RedirectToAction("Index", "Dashboard");
            }

            var now = DateTime.UtcNow;

            var rewards = await _context.ParentRewards
                .Where(r =>
                    r.ParentUserId == child.UserId &&
                    r.IsActive &&
                    (!r.ValidFromUtc.HasValue || r.ValidFromUtc <= now) &&
                    (!r.ValidToUtc.HasValue || r.ValidToUtc >= now) &&
                    (r.IsCommon || r.ChildId == child.Id) &&

                    // ?? Hide rewards this child already has an active/past-positive redemption for
                    !_context.ParentRewardRedemptions.Any(x =>
                        x.ParentRewardId == r.Id &&
                        x.ChildId == child.Id &&
                        x.Status != ParentReward.ParentRewardRedemptionStatus.Rejected &&
                        x.Status != ParentReward.ParentRewardRedemptionStatus.Cancelled
                    )
                )
                .OrderBy(r => r.CoinCost)
                .ToListAsync();

            var vm = new AvailableRewardsViewModel
            {
                ChildId = child.Id,
                ChildName = child.ChildName,   // ?? pass name
                ChildCoins = child.TotalPoints,
                Rewards = rewards
            };



            ViewData["ActivePage"] = "AvailableRewards";
            return View(vm);
        }




        // POST: /Dashboard/RedeemReward/5
        // POST: /Dashboard/RedeemReward
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RedeemReward(int rewardId)
        {
            var childId = HttpContext.Session.GetInt32("ActiveChildId");
            if (!childId.HasValue) { TempData["Error"] = "No active child selected."; return RedirectToAction("Index"); }

            await using var tx = await _context.Database.BeginTransactionAsync();

            var child = await _context.Children.FindAsync(childId.Value);
            if (child == null) { TempData["Error"] = "Child not found."; return RedirectToAction("Index"); }

            var reward = await _context.ParentRewards.FirstOrDefaultAsync(r => r.Id == rewardId && r.IsActive);
            if (reward == null) { TempData["Error"] = "Reward not found or inactive."; return RedirectToAction(nameof(AvailableRewards)); }

            // Ensure reward belongs to child's parent and is visible to this child
            if (reward.ParentUserId != child.UserId || (!reward.IsCommon && reward.ChildId != child.Id))
            {
                TempData["Error"] = "You cannot redeem this reward.";
                return RedirectToAction(nameof(AvailableRewards));
            }

            // ? New check: Prevent duplicate requests
            var hasActiveReq = await _context.ParentRewardRedemptions.AnyAsync(x =>
                x.ParentRewardId == reward.Id &&
                x.ChildId == child.Id &&
                x.Status != ParentReward.ParentRewardRedemptionStatus.Rejected &&
                x.Status != ParentReward.ParentRewardRedemptionStatus.Cancelled
            );
            if (hasActiveReq)
            {
                TempData["Error"] = "You already requested or redeemed this reward.";
                return RedirectToAction(nameof(MyRedemptions));
            }

            var now = DateTime.UtcNow;

            if (reward.RequiresParentApproval)
            {
                // ? No coin deduction yet
                var redemption = new ParentRewardRedemption
                {
                    ChildId = child.Id,
                    ParentRewardId = reward.Id,
                    CoinCostAtPurchase = reward.CoinCost,
                    RequestedUtc = now,
                    Status = ParentReward.ParentRewardRedemptionStatus.Requested,
                    CoinsDeducted = false,
                    Notes = ""
                };
                _context.ParentRewardRedemptions.Add(redemption);

                // Optional: if you previously deactivated on request, keep it active so it remains available if later rejected.
                // If you still want to hide it until decision, you can set IsActive = false here and re-activate on reject.

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                TempData["Success"] = "Request sent! Waiting for parent approval.";
                return RedirectToAction(nameof(MyRedemptions));
            }
            else
            {
                // Instant purchase (no approval): deduct coins now
                if (child.TotalPoints < reward.CoinCost)
                {
                    TempData["Error"] = "Not enough coins to redeem this reward.";
                    return RedirectToAction(nameof(AvailableRewards));
                }

                child.TotalPoints -= reward.CoinCost;

                var redemption = new ParentRewardRedemption
                {
                    ChildId = child.Id,
                    ParentRewardId = reward.Id,
                    CoinCostAtPurchase = reward.CoinCost,
                    RequestedUtc = now,
                    ApprovedUtc = now,
                    Status = ParentReward.ParentRewardRedemptionStatus.Approved,
                    CoinsDeducted = true,
                    Notes = ""
                };
                _context.ParentRewardRedemptions.Add(redemption);

                // Deactivate only child-specific rewards so they don't show again
                if (!reward.IsCommon)
                {
                    reward.IsActive = false;
                    _context.ParentRewards.Update(reward);
                }

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                TempData["Success"] = "Reward redeemed!";
                return RedirectToAction(nameof(MyRedemptions));
            }
        }



        // GET: /Dashboard/MyRedemptions
        public async Task<IActionResult> MyRedemptions()
        {
            var childId = HttpContext.Session.GetInt32("ActiveChildId");
            if (!childId.HasValue)
            {
                TempData["Error"] = "No active child selected.";
                return RedirectToAction("Index");
            }

            var redemptions = await _context.ParentRewardRedemptions
                .Include(r => r.ParentReward)
                .Where(r => r.ChildId == childId.Value)
                .OrderByDescending(r => r.RequestedUtc)
                .ToListAsync();

            // ? Calculate total coins redeemed
            var totalRedeemed = redemptions
                .Where(r => r.Status == ParentReward.ParentRewardRedemptionStatus.Approved
                            && r.CoinsDeducted)
                .Sum(r => r.CoinCostAtPurchase);

            ViewBag.TotalRedeemed = totalRedeemed;

            return View(redemptions);
        }
    }
}



