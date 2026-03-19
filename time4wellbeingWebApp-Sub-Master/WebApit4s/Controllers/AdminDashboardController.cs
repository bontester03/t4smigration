using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using WebApit4s.DAL;
using WebApit4s.Identity;
using WebApit4s.Models;
using WebApit4s.Services;
using WebApit4s.Utilities;
using WebApit4s.ViewModels;
using X.PagedList;
using X.PagedList.Extensions;
using X.PagedList.Mvc.Core;


namespace WebApit4s.Controllers
{
    public class AdminDashboardController : Controller
    {

        private readonly TimeContext _context;
        private readonly NotificationService _notificationService;
        private readonly IEmailSender _emailSender;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminDashboardController(
            TimeContext context,
            NotificationService notificationService,
            IEmailSender emailSender,
            UserManager<ApplicationUser> userManager // ✅ Add this
        )
        {
            _context = context;
            _notificationService = notificationService;
            _emailSender = emailSender;
            _userManager = userManager; // ✅ Assign here
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");

            if (userId.HasValue)
            {
                // Fetch user and child information
                var user = _context.Users.FirstOrDefault(u => u.Id == userId.Value.ToString());
                var child = _context.Children.FirstOrDefault(c => c.UserId == userId.Value.ToString());

                // Pass data to ViewBag
                ViewBag.UserEmail = user?.Email ?? "No Email Found";
                ViewBag.ChildName = child?.ChildName ?? "No Child Assigned";
            }
            else
            {
                ViewBag.UserEmail = "Guest";
                ViewBag.ChildName = "Guest";
            }

            base.OnActionExecuting(context);
        }

       
        [HttpGet]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }
            // Get all users from the old User table (not ApplicationUser)
            var usersQuery = _context.Users.AsQueryable();

            if (startDate.HasValue && endDate.HasValue)
            {
                usersQuery = usersQuery.Where(u => u.RegistrationDate >= startDate && u.RegistrationDate <= endDate);
            }

            var users = await usersQuery.ToListAsync();
            var userIdStrings = users.Select(u => u.Id.ToString()).ToList();

            var totalChildren = await _context.Children.CountAsync(c => userIdStrings.Contains(c.UserId));
            var totalWeightLogs = await _context.WeeklyMeasurements.CountAsync(w => userIdStrings.Contains(w.UserId));
            var totalHealthScores = await _context.HealthScores
    .Where(h => !h.IsDeleted)
    .Join(_context.Children,
        h => h.ChildId,
        c => c.Id,
        (h, c) => new { h, c })
    .CountAsync(x => userIdStrings.Contains(x.c.UserId));

            var healthScores = await _context.HealthScores
                .Where(h => userIdStrings.Contains(h.UserId)).ToListAsync();

            var measurements = await _context.WeeklyMeasurements
                .Where(m => userIdStrings.Contains(m.UserId)).ToListAsync();



            var activities = new List<TimelineActivityViewModel>();

            foreach (var user in users)
            {
                activities.Add(new TimelineActivityViewModel
                {
                    Timestamp = user.RegistrationDate.ToString("dd MMM yyyy HH:mm"),
                    Activity = $"{user.Email} registered",
                    Color = "bg-green"
                });

                var userScores = healthScores.Where(h => h.UserId == user.Id.ToString()).ToList();
                if (userScores.Any())
                {
                    var latestScore = userScores.OrderByDescending(s => s.DateRecorded).First();
                    activities.Add(new TimelineActivityViewModel
                    {
                        Timestamp = latestScore.DateRecorded.ToString("dd MMM yyyy HH:mm"),
                        Activity = $"{user.Email} completed a questionnaire",
                        Color = "bg-blue"
                    });
                }

                var userMeasurements = measurements.Where(m => m.UserId == user.Id.ToString()).ToList();
                if (userMeasurements.Any())
                {
                    var latestMeas = userMeasurements.OrderByDescending(m => m.DateRecorded).First();
                    activities.Add(new TimelineActivityViewModel
                    {
                        Timestamp = latestMeas.DateRecorded.ToString("dd MMM yyyy HH:mm"),
                        Activity = $"{user.Email} logged weight and age",
                        Color = "bg-yellow"
                    });
                }
            }

            var sortedActivities = activities.OrderByDescending(a => DateTime.Parse(a.Timestamp)).Take(9).ToList();

            var model = new AdminDashboardViewModel
            {
                UserCount = users.Count,
                TotalWeightLogs = totalWeightLogs,
                TotalHealthScores = totalHealthScores,
                TotalChildren = totalChildren,
                RecentActivities = sortedActivities
            };

            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");

            return View(model);
        }


        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> RecentActivity()
        {
          
            // Fetch all users, health scores, and weekly measurements
            var users = await _context.Users.ToListAsync();
            var healthScores = await _context.HealthScores.ToListAsync();
            var measurements = await _context.WeeklyMeasurements.ToListAsync();

            // Combine all activities into a single list
            var activities = new List<TimelineActivityViewModel>();

            foreach (var user in users)
            {
                activities.Add(new TimelineActivityViewModel
                {
                    Timestamp = user.RegistrationDate.ToString("dd MMM yyyy HH:mm"),
                    Activity = $"{user.Email} registered",
                    Color = "bg-green"
                });

                var userScores = healthScores.Where(h => h.UserId == user.Id.ToString()).ToList();
                if (userScores.Any())
                {
                    activities.Add(new TimelineActivityViewModel
                    {
                        Timestamp = userScores.First().DateRecorded.ToString("dd MMM yyyy HH:mm"),
                        Activity = $"{user.Email} completed a questionnaire",
                        Color = "bg-blue"
                    });
                }

                var userMeasurements = measurements.Where(m => m.UserId == user.Id.ToString()).ToList();
                if (userMeasurements.Any())
                {
                    activities.Add(new TimelineActivityViewModel
                    {
                        Timestamp = userMeasurements.First().DateRecorded.ToString("dd MMM yyyy HH:mm"),
                        Activity = $"{user.Email} logged weight and age",
                        Color = "bg-yellow"
                    });
                }
            }

            // Ensure sorting in descending order
            activities = activities.OrderByDescending(a => DateTime.Parse(a.Timestamp)).ToList();

            // ✅ Return a **list** of activities
            return View(activities);
        }


        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> ShortRecentActivity()
        {
           
            // Fetch latest user activities
            var users = await _context.Users.ToListAsync();
            var healthScores = await _context.HealthScores.ToListAsync();
            var measurements = await _context.WeeklyMeasurements.ToListAsync();

            var activities = new List<TimelineActivityViewModel>();

            foreach (var user in users)
            {
                activities.Add(new TimelineActivityViewModel
                {
                    Timestamp = user.RegistrationDate.ToString("dd MMM yyyy HH:mm"),
                    Activity = $"{user.Email} registered",
                    Color = "bg-green"
                });

                var userScores = healthScores.Where(h => h.UserId == user.Id.ToString()).ToList();
                if (userScores.Any())
                {
                    activities.Add(new TimelineActivityViewModel
                    {
                        Timestamp = userScores.First().DateRecorded.ToString("dd MMM yyyy HH:mm"),
                        Activity = $"{user.Email} completed a questionnaire",
                        Color = "bg-blue"
                    });
                }

                var userMeasurements = measurements.Where(m => m.UserId == user.Id.ToString()).ToList();
                if (userMeasurements.Any())
                {
                    activities.Add(new TimelineActivityViewModel
                    {
                        Timestamp = userMeasurements.First().DateRecorded.ToString("dd MMM yyyy HH:mm"),
                        Activity = $"{user.Email} logged weight and age",
                        Color = "bg-yellow"
                    });
                }
            }

            // Sort and take only last 6 events
            activities = activities.OrderByDescending(a => DateTime.Parse(a.Timestamp)).Take(6).ToList();

            // ✅ Return the partial view with data
            return PartialView("ShortRecentActivity", activities);
        }

        //Method to get all measurement data
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> WeeklyMeasurementsAdmin(string searchChildName, string searchUserName, int? page)
        {
           
            int pageSize = 8; // Show 8 records per page
            int pageNumber = page ?? 1; // Default to page 1

            var query = _context.WeeklyMeasurements
     .Include(m => m.User)
     .Include(m => m.Child)
     .Select(m => new WeeklyMeasurementViewModel
     {
         Id = m.Id,
         UserName = m.User != null ? m.User.Email : "N/A",
         ChildName = m.Child != null ? m.Child.ChildName : "N/A",
         Age = m.Age,
         Height = m.Height,
         Weight = m.Weight,
         CentileScore = m.CentileScore,
         HealthRange = GetHealthRange(m.CentileScore),
         DateRecorded = m.DateRecorded
     })
     .AsQueryable();


            // ✅ Apply search filters
            if (!string.IsNullOrEmpty(searchChildName))
            {
                query = query.Where(m => m.ChildName.Contains(searchChildName));
            }
            if (!string.IsNullOrEmpty(searchUserName))
            {
                query = query.Where(m => m.UserName.Contains(searchUserName));
            }

            // ✅ Fetch paginated results
            var measurements = query.OrderByDescending(m => m.DateRecorded).ToPagedList(pageNumber, pageSize);

            // ✅ Keep search terms in ViewBag
            ViewBag.SearchChildName = searchChildName;
            ViewBag.SearchUserName = searchUserName;

            return View(measurements);
        }


        // Helper method to determine Health Range
        public static string GetHealthRange(int centileScore)
        {
            if (centileScore < 2)
                return "Underweight";
            else if (centileScore >= 2 && centileScore <= 90)
                return "Healthy Weight";
            else
                return "Overweight";
        }

        // ✅ GET: Load the Send Notification page
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> SendNotification()
        {
         
            var users = await _context.Users
                .Select(u => new SelectListItem
                {
                    Value = u.Id.ToString(),
                    Text = u.Email
                })
                .ToListAsync();

            ViewBag.Users = users;
            return View("SendNotification");
        }

        // ✅ POST: Send the Notification
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> SendNotificationUser(string UserId, string Message)
        {
            if (string.IsNullOrWhiteSpace(UserId) || string.IsNullOrWhiteSpace(Message))
            {
                TempData["ErrorMessage"] = "Please select a user and enter a message.";
                return RedirectToAction("SendNotification");
            }

            var user = await _context.Users.FindAsync(UserId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("SendNotification");
            }

            var notification = new Notification
            {
                UserId = UserId,
                Message = Message,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Notification sent to {user.Email}";
            return RedirectToAction("SendNotification");
        }




        public async Task<IActionResult> ViewNotifications()
        {
            var notifications = await _context.Notifications
                .Include(n => n.User) // Join with Users table to get User Email
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new NotificationViewModel
                {
                    Id = n.Id,
                    UserName = n.User.Email, // Assuming User table has Email
                    Message = n.Message,
                    IsRead = n.IsRead ? "Read" : "Unread",
                    CreatedAt = n.CreatedAt
                })
                .ToListAsync();

            return View(notifications);
        }

        [HttpPost]
        public async Task<IActionResult> MarkNotificationAsRead(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification != null)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("ViewNotifications");
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> AllHealthScores(int? page)
        {
            int pageSize = 10;
            int pageNumber = page ?? 1;

            var healthScoresQuery = _context.HealthScores
                .Include(h => h.Child)
                    .ThenInclude(c => c.User)
                .OrderByDescending(h => h.DateRecorded)
                .Select(h => new AdminHealthScoreViewModel
                {
                    Id = h.Id,
                    UserName = h.Child.User.Email, // ✅ updated from h.User.Email
                    PhysicalActivityScore = h.PhysicalActivityScore,
                    BreakfastScore = h.BreakfastScore,
                    FruitVegScore = h.FruitVegScore,
                    SweetSnacksScore = h.SweetSnacksScore,
                    FattyFoodsScore = h.FattyFoodsScore,
                    TotalScore = h.TotalScore,
                    HealthClassification = h.HealthClassification,
                    DateRecorded = h.DateRecorded
                });

            var healthScoresPaged = await healthScoresQuery.ToListAsync();
            var pagedHealthScores = healthScoresPaged.ToPagedList(pageNumber, pageSize);

            return View(pagedHealthScores);
        }




        // Overall view logic
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> RegisteredChildren(DateTime? startDate, DateTime? endDate, int? referralTypeId, string childName, string schoolName, int? page)
        {
            int pageSize = 10;
            int pageNumber = page ?? 1;

            var childrenQuery = _context.Children
                .Include(c => c.User)
                    .ThenInclude(u => u.ReferralType)
                .AsQueryable();

            if (startDate.HasValue && endDate.HasValue)
            {
                childrenQuery = childrenQuery.Where(c =>
                    c.User.RegistrationDate >= startDate && c.User.RegistrationDate <= endDate);
            }

            if (referralTypeId.HasValue)
            {
                childrenQuery = childrenQuery.Where(c =>
                    c.User.ReferralTypeId == referralTypeId);
            }

            if (!string.IsNullOrWhiteSpace(childName))
            {
                childrenQuery = childrenQuery.Where(c =>
                    c.ChildName.Contains(childName));
            }

            if (!string.IsNullOrWhiteSpace(schoolName))
            {
                childrenQuery = childrenQuery.Where(c =>
                    c.School != null && c.School.Contains(schoolName));
            }

            var childrenList = await childrenQuery
                .Select(c => new ChildViewModel
                {
                    ChildId = c.Id, // ✅ Pass the actual child ID
                    UserId = c.UserId,
                    ChildName = c.ChildName,
                    Email = c.User.Email,
                    DateOfBirth = c.DateOfBirth,
                    Gender = c.Gender.ToString(),
                    RegistrationDate = c.CreatedAt,
                    ReferralTypeName = c.User.ReferralType.Name,
                    School = c.School

                })
                .OrderByDescending(c => c.RegistrationDate)
                .ToListAsync();

            // Dropdown and filter state
            ViewBag.ReferralTypes = await _context.ReferralTypes
                .Select(r => new SelectListItem { Value = r.Id.ToString(), Text = r.Name })
                .ToListAsync();

            ViewBag.SelectedReferralType = referralTypeId?.ToString();
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
            ViewBag.ChildName = childName;
            ViewBag.SchoolName = schoolName;

            return View(childrenList.ToPagedList(pageNumber, pageSize));
        }


        //iewChildDetails

        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> ViewChildDetails(int childId)
        {
            var child = await _context.Children
                .Include(c => c.User)
                    .ThenInclude(u => u.ReferralType)
                .Include(c => c.User.PersonalDetails)
                .FirstOrDefaultAsync(c => c.Id == childId);

            if (child == null)
                return NotFound();

            var user = child.User;
            var personalDetails = user.PersonalDetails;

            var adminNotes = await _context.AdminNotes
                .Where(n => n.ChildId == child.Id)
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new AdminNoteViewModel
                {
                    Id = n.Id,
                    ChildId = n.ChildId,
                    NoteText = n.NoteText,
                    CreatedAt = n.CreatedAt,
                })
                .ToListAsync();

            ViewBag.ReferralTypes = await _context.ReferralTypes
                .Select(r => new SelectListItem
                {
                    Value = r.Id.ToString(),
                    Text = r.Name
                }).ToListAsync();

            var measurements = await _context.WeeklyMeasurements
                .Where(m => m.ChildId == child.Id)
                .Select(m => new WeeklyMeasurementViewModel
                {
                    Id = m.Id,
                    Age = m.Age,
                    Height = m.Height,
                    Weight = m.Weight,
                    CentileScore = m.CentileScore,
                    HealthRange = AdminDashboardController.GetHealthRange(m.CentileScore),
                    DateRecorded = m.DateRecorded
                }).ToListAsync();

            var healthScores = await _context.HealthScores
                .Where(h => h.ChildId == child.Id)
                .Select(h => new AdminHealthScoreViewModel
                {
                    Id = h.Id,
                    UserName = user.Email,
                    PhysicalActivityScore = h.PhysicalActivityScore,
                    BreakfastScore = h.BreakfastScore,
                    FruitVegScore = h.FruitVegScore,
                    SweetSnacksScore = h.SweetSnacksScore,
                    FattyFoodsScore = h.FattyFoodsScore,
                    TotalScore = h.TotalScore,
                    HealthClassification = h.HealthClassification,
                    DateRecorded = h.DateRecorded
                }).ToListAsync();

            var model = new AdminChildFullDetailsViewModel
            {
                UserId = user.Id,
                Email = user.Email,
                ReferralTypeId = user.ReferralTypeId ?? 0,
                ReferralType = user.ReferralType?.Name ?? "N/A",
                RegistrationDate = user.RegistrationDate,
                ChildId = child.Id,
                ChildName = child.ChildName ?? "N/A",
                DateOfBirth = child.DateOfBirth,
                Gender = child.Gender.ToString(),
                EngagementStatus = child.EngagementStatus,
                TotalPoints = child.TotalPoints,
                ParentGuardianName = personalDetails?.ParentGuardianName ?? "N/A",
                RelationshipToChild = personalDetails?.RelationshipToChild ?? "N/A",
                TeleNumber = personalDetails?.TeleNumber ?? "N/A",
                ParentEmail = personalDetails?.Email ?? "N/A",
                Postcode = personalDetails?.Postcode ?? "N/A",
                // ✅ CHILD school/class (from Child table)
                School = child.School ?? "N/A",
                Class = child.Class ?? "N/A",

                // ✅ PARENT school/class (from PersonalDetails table)
                ParentSchool = child.School ?? "N/A",
                ParentClass = child.Class ?? "N/A",


                Measurements = measurements,
                HealthScores = healthScores,
                AdminNotes = adminNotes,
                NewNoteText = string.Empty
            };

            var referralType = await _context.ReferralTypes.FindAsync(user.ReferralTypeId);
            model.RequiresSchoolSelection = referralType?.RequiresSchoolSelection ?? false;
            model.ReferralTypeCategory = referralType?.Category ?? ReferralCategory.Other;


            ViewBag.GenderList = Enum.GetValues(typeof(Gender))
                .Cast<Gender>()
                .Select(g => new SelectListItem
                {
                    Value = g.ToString(),
                    Text = g.ToString()
                })
                .ToList();

            ViewBag.RelationshipList = new List<SelectListItem>
            {
                new SelectListItem { Value = "Mother", Text = "Mother" },
                new SelectListItem { Value = "Father", Text = "Father" },
                new SelectListItem { Value = "Guardian", Text = "Guardian" },
                new SelectListItem { Value = "Other", Text = "Other" }
            };

            ViewBag.SchoolList = await _context.Schools
    .Where(s => s.IsActive) // ✅ Only active schools
    .OrderBy(s => s.Name)
    .Select(s => new SelectListItem
    {
        Value = s.Name,
        Text = s.Name
    })
    .ToListAsync();

            ViewBag.ClassList = await _context.Classes
                .Where(c => c.IsActive) // ✅ Only active classes
                .OrderBy(c => c.Name)
                .Select(c => new SelectListItem
                {
                    Value = c.Name,
                    Text = c.Name
                })
                .ToListAsync();


            var notifications = await _context.Notifications
                .Where(n => n.UserId == user.Id)
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new NotificationViewModel
                {
                    Id = n.Id,
                    UserName = user.Email,
                    Message = n.Message,
                    IsRead = n.IsRead ? "Read" : "Unread",
                    CreatedAt = n.CreatedAt
                })
                .ToListAsync();

            model.Notifications = notifications;

            var consentAnswers = await _context.ConsentAnswers
                .Where(a => a.UserId == user.Id)
                .Include(a => a.ConsentQuestion)
                .Select(a => new AdminConsentAnswerViewModel
                {
                    Question = a.ConsentQuestion.Text,
                    Answer = a.Answer,
                    SubmittedAt = a.SubmittedAt
                })
                .ToListAsync();

            model.ConsentAnswers = consentAnswers;

            return View("ViewChildDetails", model);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAdminNote(AdminChildFullDetailsViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.NewNoteText))
            {
                TempData["Error"] = "Note text cannot be empty.";
                return RedirectToAction("ViewChildDetails", new { userId = model.UserId });
            }

            var child = await _context.Children.FirstOrDefaultAsync(c => c.Id == model.ChildId);
            if (child == null)
            {
                TempData["Error"] = "Child not found.";
                return RedirectToAction("ViewChildDetails", new { userId = model.UserId });
            }

            var note = new AdminNote
            {
                ChildId = child.Id,
                NoteText = model.NewNoteText,
                CreatedAt = DateTime.UtcNow
            };

            _context.AdminNotes.Add(note);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Note added successfully.";
            return RedirectToAction("ViewChildDetails", "AdminDashboard", new { childId = model.ChildId });
        }




        [HttpPost]
        [Authorize(Roles = "Admin,Employee")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LogMeasurement(WeeklyMeasurements model)
        {
            ModelState.Remove("Child");
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Invalid data. Please fill all required fields.";
                return RedirectToAction("ViewChildDetails", "AdminDashboard", new { childId = model.ChildId });
            }

            var child = await _context.Children.FindAsync(model.ChildId);
            if (child == null)
            {
                TempData["Error"] = "Child not found.";
                return RedirectToAction("RegisteredChildren", "AdminDashboard");
            }

            // Optional: Set current time if DateRecorded is not passed
            if (model.DateRecorded == default)
            {
                model.DateRecorded = DateTime.UtcNow;
            }

            _context.WeeklyMeasurements.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Measurement logged successfully.";
            return RedirectToAction("ViewChildDetails", "AdminDashboard", new { childId = model.ChildId });
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMeasurement(int id)
        {
            var record = await _context.WeeklyMeasurements.FindAsync(id);
            if (record != null)
            {
                int childId = record.ChildId;

                _context.WeeklyMeasurements.Remove(record);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Measurement deleted successfully.";
                return RedirectToAction("ViewChildDetails", new { childId });
            }

            TempData["Error"] = "Measurement not found.";
            return RedirectToAction("RegisteredChildren"); // Fallback
        }




        [HttpPost]
        public async Task<IActionResult> UpdateUserInfo(string userId, int referralTypeId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound();

            user.ReferralTypeId = referralTypeId;
            await _context.SaveChangesAsync();

            var childId = await _context.Children
                .Where(c => c.UserId == userId)
                .Select(c => c.Id)
                .FirstOrDefaultAsync();

            if (childId == 0)
                return NotFound();

            return RedirectToAction("ViewChildDetails", new { childId });
        }




        [HttpPost]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> UpdateChildInfo(
         int childId,
         string childName,
         DateTime dateOfBirth,
         string gender,
         EngagementStatus engagementStatus,
         string? school,
         string? className // 👈 use className (avoid "class")
     )
        {
            var child = await _context.Children.FirstOrDefaultAsync(c => c.Id == childId);
            if (child == null) return NotFound();

            child.ChildName = childName;
            child.DateOfBirth = DateTimeUtils.EnsureUtc(dateOfBirth);
            child.EngagementStatus = engagementStatus;

            // ✅ update school/class in Child table
            child.School = string.IsNullOrWhiteSpace(school) ? child.School : school.Trim();
            child.Class = string.IsNullOrWhiteSpace(className) ? child.Class : className.Trim();

            if (Enum.TryParse<Gender>(gender, out var parsedGender))
                child.Gender = parsedGender;
            else
            {
                TempData["Error"] = "Invalid gender.";
                return RedirectToAction("ViewChildDetails", new { childId });
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Child updated successfully.";
            return RedirectToAction("ViewChildDetails", new { childId });
        }





        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateParentInfo(
      string UserId,
      int ChildId,
      string ParentGuardianName,
      string RelationshipToChild,
      string TeleNumber,
      string ParentEmail,
      string Postcode
  )
        {
            var pd = await _context.PersonalDetails
                .FirstOrDefaultAsync(p => p.UserId == UserId);

            if (pd == null)
            {
                pd = new PersonalDetails
                {
                    UserId = UserId
                };
                _context.PersonalDetails.Add(pd);
            }

            pd.ParentGuardianName = (ParentGuardianName ?? "").Trim();
            pd.RelationshipToChild = (RelationshipToChild ?? "").Trim();
            pd.TeleNumber = (TeleNumber ?? "").Trim();
            pd.Email = (ParentEmail ?? "").Trim();
            pd.Postcode = (Postcode ?? "").Trim();

            await _context.SaveChangesAsync();

            TempData["Success"] = "Parent/guardian details saved successfully.";
            return RedirectToAction("ViewChildDetails", new { childId = ChildId });
        }





        //Health score admin update or add
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddOrUpdateHealthScore(AdminHealthScoreViewModel model, int childId)
        {
            if (!ModelState.IsValid)
            {
                foreach (var modelState in ModelState)
                {
                    foreach (var error in modelState.Value.Errors)
                    {
                        Console.WriteLine($"Key: {modelState.Key}, Error: {error.ErrorMessage}");
                    }
                }

                TempData["Error"] = "Invalid input. Please review and try again.";
                return RedirectToAction("ViewChildDetails", new { childId });
            }

            if (model.PhysicalActivityScore < 0 || model.PhysicalActivityScore > 4 ||
                model.BreakfastScore < 0 || model.BreakfastScore > 4 ||
                model.FruitVegScore < 0 || model.FruitVegScore > 4 ||
                model.SweetSnacksScore < 0 || model.SweetSnacksScore > 4 ||
                model.FattyFoodsScore < 0 || model.FattyFoodsScore > 4)
            {
                TempData["Error"] = "All scores must be selected from the dropdown.";
                return RedirectToAction("ViewChildDetails", new { childId });
            }

            int[] scoreMap = { 1, 2, 3, 4, 5 };
            int total = scoreMap[model.PhysicalActivityScore] +
                        scoreMap[model.BreakfastScore] +
                        scoreMap[model.FruitVegScore] +
                        scoreMap[model.SweetSnacksScore] +
                        scoreMap[model.FattyFoodsScore];

            string classification = total >= 15 ? "Healthy" : "Unhealthy";

            var newScore = new HealthScore
            {
                ChildId = childId, // must be a valid ID
                                   // optionally also assign UserId if needed
                PhysicalActivityScore = model.PhysicalActivityScore,
                BreakfastScore = model.BreakfastScore,
                FruitVegScore = model.FruitVegScore,
                SweetSnacksScore = model.SweetSnacksScore,
                FattyFoodsScore = model.FattyFoodsScore,
                TotalScore = total,
                HealthClassification = classification,
                DateRecorded = model.DateRecorded == default ? DateTime.UtcNow : model.DateRecorded
            };


            _context.HealthScores.Add(newScore);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Health Score added successfully.";
            return RedirectToAction("ViewChildDetails", new { childId });
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteHealthScore(int id)
        {
            var score = await _context.HealthScores.FindAsync(id);
            if (score == null)
            {
                TempData["Error"] = "Health score not found.";
                return RedirectToAction("RegisteredChildren");
            }

            int childId = score.ChildId;

            _context.HealthScores.Remove(score);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Health score deleted successfully.";
            return RedirectToAction("ViewChildDetails", new { childId });
        }



        //Code for Add/Update/Delete School/Ref/Classes
        // ================== SCHOOLS ==================
        // GET: ManageSchools
        [Authorize(Roles = "Admin,Employee")]
        public IActionResult ManageSchools()
        {
           
            var schools = _context.Schools.OrderBy(s => s.Name).ToList();
            return View(schools);
        }

        // POST: Add School
        [HttpPost]
        [Authorize(Roles = "Admin,Employee")]
        public IActionResult AddSchool(Schools model)
        {
            if (ModelState.IsValid && !string.IsNullOrWhiteSpace(model.Name))
            {
                model.IsActive = true; // ✅ Set active by default
                _context.Schools.Add(model);
                _context.SaveChanges();
            }
            return RedirectToAction("ManageSchools");
        }


        // POST: Edit School
        [HttpPost]
        [Authorize(Roles = "Admin,Employee")]
        public IActionResult EditSchool(Schools model)
        {
            if (ModelState.IsValid)
            {
                var school = _context.Schools.Find(model.Id);
                if (school != null)
                {
                    school.Name = model.Name;
                    _context.SaveChanges();
                }
            }
            return RedirectToAction("ManageSchools");
        }

        // POST: Delete School
        [HttpPost]
        [Authorize(Roles = "Admin,Employee")]
        public IActionResult DeleteSchool(int id)
        {
            var school = _context.Schools.Find(id);
            if (school != null)
            {
                _context.Schools.Remove(school);
                _context.SaveChanges();
            }
            return RedirectToAction("ManageSchools");
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Employee")]
        public IActionResult ToggleSchoolStatus(int id)
        {
            var school = _context.Schools.Find(id);
            if (school != null)
            {
                school.IsActive = !school.IsActive;
                _context.SaveChanges();
            }
            return RedirectToAction("ManageSchools");
        }


        // GET: /AdminDashboard/ManageClasses
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> ManageClasses()
        {
            var classList = await _context.Classes.OrderBy(c => c.Name).ToListAsync();
            return View(classList);
        }

        // POST: /AdminDashboard/AddClass
        [HttpPost]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> AddClass(Classes model)
        {
           
            if (!string.IsNullOrWhiteSpace(model.Name))
            {
                _context.Classes.Add(model);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("ManageClasses");
        }

        // POST: /AdminDashboard/EditClass
        [HttpPost]
        public async Task<IActionResult> EditClass(Classes model)
        {
            var existing = await _context.Classes.FindAsync(model.Id);
            if (existing != null)
            {
                existing.Name = model.Name;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("ManageClasses");
        }

        // POST: /AdminDashboard/DeleteClass
        [HttpPost]
        public async Task<IActionResult> DeleteClass(int id)
        {
            var item = await _context.Classes.FindAsync(id);
            if (item != null)
            {
                _context.Classes.Remove(item);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("ManageClasses");
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Employee")] // ✅ Allow both Admin and Employee
        public async Task<IActionResult> ToggleClassStatus(int id)
        {
            var cls = await _context.Classes.FindAsync(id);
            if (cls != null)
            {
                cls.IsActive = !cls.IsActive;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("ManageClasses");
        }


        // GET: ManageReferralTypes
        [Authorize(Roles = "Admin,Employee")]
        public IActionResult ManageReferralTypes()
        {
            EnsureSelfReferralExistsAndActive();
            var referralTypes = _context.ReferralTypes.OrderBy(r => r.Name).ToList();

            ViewBag.Categories = Enum.GetValues(typeof(ReferralCategory))
                .Cast<ReferralCategory>()
                .Select(c => new SelectListItem
                {
                    Value = ((int)c).ToString(),
                    Text = c.ToString()
                })
                .ToList();

            return View(referralTypes);
        }

        // POST: Add Referral Type
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddReferralType(ReferralTypeViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (model.Category == ReferralCategory.SelfReferral &&
                    _context.ReferralTypes.Any(r => r.Category == ReferralCategory.SelfReferral))
                {
                    TempData["Error"] = "Self Referral already exists and is managed automatically.";
                    return RedirectToAction("ManageReferralTypes");
                }

                if (_context.ReferralTypes.Any(r => r.Name == model.Name && r.Category == model.Category))
                {
                    TempData["Error"] = "A referral type with this name and category already exists.";
                    return RedirectToAction("ManageReferralTypes");
                }

                var referral = new ReferralType
                {
                    Name = model.Name,
                    Category = model.Category,
                    RequiresSchoolSelection = model.RequiresSchoolSelection,
                    IsActive = model.IsActive,
                    UsageCount = 0
                };

                _context.ReferralTypes.Add(referral);
                _context.SaveChanges();

                TempData["Success"] = "Referral type added successfully.";
                return RedirectToAction("ManageReferralTypes");
            }

            TempData["Error"] = "Invalid referral type data.";
            return RedirectToAction("ManageReferralTypes");
        }



        // POST: Edit Referral Type
        [HttpPost]
        public IActionResult EditReferralType(ReferralType model)
        {
            var existing = _context.ReferralTypes.Find(model.Id);
            if (existing?.Category == ReferralCategory.SelfReferral || model.Category == ReferralCategory.SelfReferral)
            {
                TempData["Error"] = "Self Referral cannot be edited.";
                return RedirectToAction("ManageReferralTypes");
            }

            if (ModelState.IsValid)
            {
                if (existing != null)
                {
                    existing.Name = model.Name;
                    existing.Category = model.Category;
                    existing.RequiresSchoolSelection = model.RequiresSchoolSelection;
                    existing.IsActive = model.IsActive;

                    _context.SaveChanges();
                    TempData["Success"] = "Referral type updated.";
                }
            }

            return RedirectToAction("ManageReferralTypes");
        }

        [HttpPost]
        public IActionResult DeleteReferralType(int id)
        {
            var referral = _context.ReferralTypes.Find(id);
            if (referral?.Category == ReferralCategory.SelfReferral)
            {
                TempData["Error"] = "Self Referral cannot be deleted.";
                return RedirectToAction("ManageReferralTypes");
            }

            if (referral != null)
            {
                _context.ReferralTypes.Remove(referral);
                _context.SaveChanges();
                TempData["Success"] = "Referral type deleted.";
            }

            return RedirectToAction("ManageReferralTypes");
        }

        [HttpPost]
        public async Task<IActionResult> ToggleSchoolFlag(int id)
        {
            var r = await _context.ReferralTypes.FindAsync(id);
            if (r is { Category: ReferralCategory.SelfReferral })
            {
                r.IsActive = true;
                r.RequiresSchoolSelection = false;
                await _context.SaveChangesAsync();
                TempData["Info"] = "Self Referral is fixed as active and does not require school selection.";
            }
            else if (r != null)
            {
                r.RequiresSchoolSelection = !r.RequiresSchoolSelection;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(ManageReferralTypes));
        }

        [HttpPost]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var r = await _context.ReferralTypes.FindAsync(id);
            if (r is { Category: ReferralCategory.SelfReferral })
            {
                r.IsActive = true;
                r.RequiresSchoolSelection = false;
                await _context.SaveChangesAsync();
                TempData["Info"] = "Self Referral is always active.";
            }
            else if (r != null)
            {
                r.IsActive = !r.IsActive;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(ManageReferralTypes));
        }


        //Notificatiion
        [HttpPost]
        public async Task<IActionResult> SendNotificationFromChildView(string UserId, int ChildId, string NotificationMessage)
        {
            if (string.IsNullOrWhiteSpace(NotificationMessage))
            {
                TempData["Error"] = "Notification message cannot be empty.";
                return RedirectToAction("ViewChildDetails", new { childId = ChildId });
            }

            var user = await _context.Users.FindAsync(UserId);
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("RegisteredChildren");
            }

            // Save to DB
            var notification = new Notification
            {
                UserId = UserId,
                Message = NotificationMessage,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            var emailBody = $@"
<p>Hello,</p>
<p>You have received a new message from Time4Wellbeing:</p>
<blockquote style='border-left: 4px solid #ccc; margin: 10px 0; padding-left: 10px; color: #555;'>
    {NotificationMessage}
</blockquote>
<p>You can also view this message by logging into your account.</p>
<p style='font-size: 0.9em; color: #888; margin-top: 20px;'>
    This is a computer-generated email. Please do not reply directly to this address.
    For further assistance, email <a href='mailto:info@time4sportuk.com'>info@time4sportuk.com</a>
    or visit <a href='https://www.time4sportuk.com/t4w'>https://www.time4sportuk.com/t4w</a>.
</p>
<p>Best regards,<br/>Time4Wellbeing Team</p>";

            // Send email using configured SMTP settings.
            try
            {
                await _emailSender.SendEmailAsync(user.Email, "You've received a new notification", emailBody);
            }
            catch (Exception ex)
            {
                TempData["Warning"] = $"Notification saved, but email failed: {ex.Message}";
            }

            TempData["Success"] = "Notification sent and email delivered.";
            return RedirectToAction("ViewChildDetails", new { childId = ChildId });
        }





        //All in one page
        public async Task<IActionResult> ManageEntities()
        {
            var viewModel = new AdminEntityManagementViewModel
            {
                Schools = await _context.Schools.ToListAsync(),
                Classes = await _context.Classes.ToListAsync(),
                ReferralTypes = await _context.ReferralTypes.ToListAsync()
            };
            return View(viewModel);
        }

        //sEPERATE S/r/c
        public async Task<IActionResult> ChildCompletionReport(
     DateTime? startDate,
     DateTime? endDate,
     int? referralTypeId,
     bool? hasPersonalDetails,
     bool? hasHealthScores,
     bool? hasMeasurements,
     int? page)
        {
            int pageSize = 10;
            int pageNumber = page ?? 1;

            var childrenQuery = _context.Children
                .Include(c => c.User)
                    .ThenInclude(u => u.ReferralType)
                .Where(c => !c.IsDeleted)
                .OrderByDescending(c => c.CreatedAt)
                .AsQueryable();

            if (startDate.HasValue && endDate.HasValue)
            {
                childrenQuery = childrenQuery.Where(c =>
                    c.User.RegistrationDate >= startDate && c.User.RegistrationDate <= endDate);
            }

            if (referralTypeId.HasValue)
            {
                childrenQuery = childrenQuery.Where(c =>
                    c.User.ReferralTypeId == referralTypeId);
            }

            var children = await childrenQuery.ToListAsync();

            var childStatusList = new List<ChildCompletionStatusViewModel>();

            foreach (var child in children)
            {
                var user = child.User;

                var hasPD = await _context.PersonalDetails.AnyAsync(p => p.UserId == user.Id);
                var hasHS = await _context.HealthScores.AnyAsync(h => h.ChildId == child.Id);
                var hasM = await _context.WeeklyMeasurements.AnyAsync(m => m.ChildId == child.Id);

                // Apply optional filters
                if (hasPersonalDetails.HasValue && hasPD != hasPersonalDetails.Value) continue;
                if (hasHealthScores.HasValue && hasHS != hasHealthScores.Value) continue;
                if (hasMeasurements.HasValue && hasM != hasMeasurements.Value) continue;

                childStatusList.Add(new ChildCompletionStatusViewModel
                {
                    UserId = user.Id,
                    Email = user.Email,
                    ReferralType = user.ReferralType?.Name ?? "N/A",
                    RegistrationDate = child.CreatedAt,
                    ChildName = child.ChildName,
                    DateOfBirth = child.DateOfBirth,
                    HasPersonalDetails = hasPD,
                    HasHealthScores = hasHS,
                    HasMeasurements = hasM
                });
            }

            // ViewBag values for dropdowns
            ViewBag.ReferralTypes = await _context.ReferralTypes
                .Select(r => new SelectListItem { Value = r.Id.ToString(), Text = r.Name })
                .ToListAsync();

            ViewBag.SelectedReferralType = referralTypeId?.ToString();
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
            ViewBag.HasPersonalDetails = hasPersonalDetails;
            ViewBag.HasHealthScores = hasHealthScores;
            ViewBag.HasMeasurements = hasMeasurements;

            return View("ChildCompletionReport", childStatusList.ToPagedList(pageNumber, pageSize));
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> SendReminderEmail(string email, string childName, bool missingPersonal, bool missingHealth, bool missingMeasurement)
        {
            try
            {
                var missingParts = new List<string>();
                if (missingPersonal) missingParts.Add("Personal Details");
                if (missingHealth) missingParts.Add("Health Scores");
                if (missingMeasurement) missingParts.Add("Measurements");

                if (!missingParts.Any())
                {
                    TempData["Error"] = "Nothing missing for this child.";
                    return RedirectToAction("ChildCompletionReport");
                }

                var replacements = new Dictionary<string, string>
        {
            { "ChildName", childName },
            { "MissingSections", string.Join(", ", missingParts) }
        };

                var emailSenderConcrete = _emailSender as EmailSender;
                if (emailSenderConcrete == null)
                {
                    TempData["Error"] = "Email sender not configured properly.";
                    return RedirectToAction("ChildCompletionReport");
                }

                var htmlBody = emailSenderConcrete.LoadTemplate("ReminderTemplate.html", replacements);
                await _emailSender.SendEmailAsync(email, "Time4Wellbeing - Please Complete Your Child's Info", htmlBody);

                TempData["Success"] = $"Reminder sent to {email}.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error sending email: {ex.Message}";
            }

            return RedirectToAction("ChildCompletionReport");
        }





        // GET: Manage Consent Questions
        [Authorize(Roles = "Admin,Employee")]
        public IActionResult ManageConsentQuestions()
        {
          
            var questions = _context.ConsentQuestions.OrderBy(q => q.Id).ToList();
            return View(questions);
        }

        // POST: Add Consent Question
        [HttpPost]
        public IActionResult AddConsentQuestion(ConsentQuestion model)
        {
            if (!ModelState.IsValid)
            {
                // Log validation errors to console (or a log file in production)
                foreach (var entry in ModelState)
                {
                    foreach (var error in entry.Value.Errors)
                    {
                        Console.WriteLine($"❌ Field: {entry.Key}, Error: {error.ErrorMessage}");
                    }
                }

                TempData["Error"] = "Model state is invalid. Please check your inputs.";
                return RedirectToAction("ManageConsentQuestions");
            }

            if (string.IsNullOrWhiteSpace(model.Text))
            {
                TempData["Error"] = "Question text cannot be empty.";
                return RedirectToAction("ManageConsentQuestions");
            }

            _context.ConsentQuestions.Add(model);
            _context.SaveChanges();

            TempData["Success"] = "Consent question added successfully.";
            return RedirectToAction("ManageConsentQuestions");
        }


        // POST: Edit Consent Question
        [HttpPost]
        public IActionResult EditConsentQuestion(ConsentQuestion model)
        {
            var existing = _context.ConsentQuestions.Find(model.Id);
            if (existing != null && ModelState.IsValid)
            {
                existing.Text = model.Text;
                existing.IsRequired = model.IsRequired;
                _context.SaveChanges();
            }

            return RedirectToAction("ManageConsentQuestions");
        }

        // POST: Delete Consent Question
        [HttpPost]
        public IActionResult DeleteConsentQuestion(int id)
        {
            var question = _context.ConsentQuestions.Find(id);
            if (question != null)
            {
                _context.ConsentQuestions.Remove(question);
                _context.SaveChanges();
            }

            return RedirectToAction("ManageConsentQuestions");
        }

        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> AllConsentAnswers(int page = 1)
        {
          
            int pageSize = 10;

            var consentAnswers = await _context.ConsentAnswers
                .Include(a => a.User)
                    .ThenInclude(u => u.Children)
                .Include(a => a.ConsentQuestion)
                .Select(a => new AdminConsentAnswerViewModel
                {
                    UserEmail = a.User.Email,
                    ChildName = a.User.Children.FirstOrDefault().ChildName ?? "N/A",
                    Question = a.ConsentQuestion.Text,
                    Answer = a.Answer,
                    SubmittedAt = a.SubmittedAt
                })
                .OrderByDescending(a => a.SubmittedAt)
                .ToListAsync();

            var pagedList = consentAnswers.ToPagedList(page, pageSize);

            return View(pagedList);
        }


        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> ViewRegistrationReminders(int page = 1)
        {
            int pageSize = 10;

            var query = _context.RegistrationReminders
                .Include(r => r.User)
                .OrderByDescending(r => r.SentAt)
                .Select(r => new AdminRegistrationReminderViewModel
                {
                    UserEmail = r.User.Email,
                    SentAt = r.SentAt
                });

            // Fix: Convert the query to a List before applying pagination
            var remindersList = await query.ToListAsync();
            var pagedReminders = remindersList.ToPagedList(page, pageSize);

            return View(pagedReminders);
        } 

        // Controller Method with Filters + Pagination
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AllFeedback(string category, string userEmail, DateTime? startDate, DateTime? endDate, int? page)
        {
            var query = _context.Feedbacks.Include(f => f.User).AsQueryable();

            if (!string.IsNullOrEmpty(category))
                query = query.Where(f => f.Category == category);

            if (!string.IsNullOrEmpty(userEmail))
                query = query.Where(f => f.User.Email.Contains(userEmail));

            if (startDate.HasValue)
                query = query.Where(f => f.SubmittedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(f => f.SubmittedAt <= endDate.Value);

            int pageSize = 10;
            int pageNumber = page ?? 1;

            var pagedList = query
     .OrderByDescending(f => f.SubmittedAt)
     .ToPagedList(pageNumber, pageSize); // ✅ Use this


            ViewBag.Categories = await _context.Feedbacks
                .Select(f => f.Category)
                .Distinct()
                .ToListAsync();

            return View(pagedList);  // ✅ Must pass IPagedList
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> ManageAIGoals()
        {
            var users = await _userManager.Users.ToListAsync();

            var model = users.Select(u => new UserAIGoalToggleViewModel
            {
                UserId = u.Id,
                Email = u.Email,
                EnableAIGoals = u.EnableAIGoals
            }).ToList();

            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> UpdateAIGoalSetting(string userId, bool enable)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound();

            user.EnableAIGoals = enable;
            await _userManager.UpdateAsync(user);

            return RedirectToAction("ManageAIGoals");
        }

        // GET: /AdminDashboard/ChildDetails/5
        [HttpGet]
        public async Task<IActionResult> ChildDetails(int id)
        {
            var child = await _context.Children
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (child == null)
            {
                TempData["Error"] = "Child not found.";
                return RedirectToAction("RegisteredChildren"); // or wherever your admin list lives
            }

            // Tasks
            var tasks = await _context.ChildGameTasks
                .Include(t => t.GameTask)
                .Where(t => t.ChildId == child.Id)
                .OrderByDescending(t => t.AssignedDate)
                .ToListAsync();

            var activeTasks = tasks
                .Where(t => !t.IsCompleted && !t.IsExpired)
                .ToList();

            var completedOrExpired = tasks
                .Where(t => t.IsCompleted || t.IsExpired)
                .OrderByDescending(t => t.CompletedDate ?? t.ExpiryDate)

                .ToList();

            // Points
            var pointHistory = await _context.UserPointHistories
                .Include(p => p.Child)
                .Where(p => p.ChildId == child.Id && p.UserId == child.UserId)
                .OrderByDescending(p => p.CreatedUtc)
                .ToListAsync();

            // Parent rewards visible to this child (common or child-specific)
            var now = DateTime.UtcNow;
            var parentRewards = await _context.ParentRewards
                .Where(r =>
                    r.ParentUserId == child.UserId &&
                    r.IsActive &&
                    (!r.ValidFromUtc.HasValue || r.ValidFromUtc <= now) &&
                    (!r.ValidToUtc.HasValue || r.ValidToUtc >= now) &&
                    (r.IsCommon || r.ChildId == child.Id))
                .OrderBy(r => r.CoinCost)
                .ToListAsync();

            // Redemptions by this child
            var redemptions = await _context.ParentRewardRedemptions
                .Include(r => r.ParentReward)
                .Where(r => r.ChildId == child.Id)
                .OrderByDescending(r => r.RequestedUtc)
                .ToListAsync();

            var vm = new AdminChildOverviewViewModel
            {
                Child = child,
                ActiveTasks = activeTasks,
                CompletedOrExpiredTasks = completedOrExpired,
                PointHistory = pointHistory,
                ParentRewards = parentRewards,
                Redemptions = redemptions
            };

            ViewData["Title"] = $"Child Overview - {child.ChildName}";
            return View(vm); // Views/AdminDashboard/ChildDetails.cshtml
        }

        private void EnsureSelfReferralExistsAndActive()
        {
            var selfReferral = _context.ReferralTypes.FirstOrDefault(r => r.Category == ReferralCategory.SelfReferral);
            var changed = false;

            if (selfReferral == null)
            {
                _context.ReferralTypes.Add(new ReferralType
                {
                    Name = "Self-Referral",
                    Category = ReferralCategory.SelfReferral,
                    RequiresSchoolSelection = false,
                    IsActive = true,
                    UsageCount = 0
                });
                changed = true;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(selfReferral.Name))
                {
                    selfReferral.Name = "Self-Referral";
                    changed = true;
                }

                if (!selfReferral.IsActive)
                {
                    selfReferral.IsActive = true;
                    changed = true;
                }

                if (selfReferral.RequiresSchoolSelection)
                {
                    selfReferral.RequiresSchoolSelection = false;
                    changed = true;
                }
            }

            if (changed)
            {
                _context.SaveChanges();
            }
        }

    }
}
