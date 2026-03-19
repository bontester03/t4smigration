using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebApit4s.DAL;
using WebApit4s.Models;
using WebApit4s.Utilities;
using WebApit4s.ViewModels;

namespace WebApit4s.Controllers
{
    public class AdminSchoolController : Controller
    {
        private readonly TimeContext _context;

        public AdminSchoolController(TimeContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var schools = await _context.Schools
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .ToListAsync();

            ViewBag.Schools = new SelectList(schools, "Name", "Name");
            return View();
        }

        public IActionResult Testing()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ViewSchoolRecords(string schoolName)
        {
            if (string.IsNullOrWhiteSpace(schoolName))
            {
                TempData["Error"] = "Please select a school.";
                return RedirectToAction("Index");
            }

            var viewModel = await GetSchoolRecordsData(schoolName);
            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> GetRecordsBySchool(string schoolName)
        {
            if (string.IsNullOrWhiteSpace(schoolName))
            {
                return BadRequest("School name is required.");
            }

            var viewModel = await GetSchoolRecordsData(schoolName);
            return Json(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> ChildEdit(int childId)
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
                    HealthRange = GetHealthRange(m.CentileScore),
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
                School = child.School ?? "N/A",
                Class = child.Class ?? "N/A",
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
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .Select(s => new SelectListItem
                {
                    Value = s.Name,
                    Text = s.Name
                })
                .ToListAsync();

            ViewBag.ClassList = await _context.Classes
                .Where(c => c.IsActive)
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

            // Pass the school name to the view for the back button
            ViewBag.SchoolName = child.School;

            return View("ChildEdit", model);
        }

        // POST: Update User Info
        [HttpPost]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> UpdateUserInfo(string userId, int referralTypeId, int childId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound();

            user.ReferralTypeId = referralTypeId;
            await _context.SaveChangesAsync();

            TempData["Success"] = "User info updated successfully.";
            return RedirectToAction("ChildEdit", new { childId });
        }

        // POST: Add Admin Note
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> AddAdminNote(AdminChildFullDetailsViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.NewNoteText))
            {
                TempData["Error"] = "Note text cannot be empty.";
                return RedirectToAction("ChildEdit", new { childId = model.ChildId });
            }

            var child = await _context.Children.FirstOrDefaultAsync(c => c.Id == model.ChildId);
            if (child == null)
            {
                TempData["Error"] = "Child not found.";
                return RedirectToAction("Index");
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
            return RedirectToAction("ChildEdit", new { childId = model.ChildId });
        }

        // POST: Update Child Info
        [HttpPost]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> UpdateChildInfo(
            int childId,
            string childName,
            DateTime dateOfBirth,
            string gender,
            EngagementStatus engagementStatus,
            string? school,
            string? className)
        {
            var child = await _context.Children.FirstOrDefaultAsync(c => c.Id == childId);
            if (child == null) return NotFound();

            child.ChildName = childName;
            child.DateOfBirth = DateTimeUtils.EnsureUtc(dateOfBirth);
            child.EngagementStatus = engagementStatus;
            child.School = string.IsNullOrWhiteSpace(school) ? child.School : school.Trim();
            child.Class = string.IsNullOrWhiteSpace(className) ? child.Class : className.Trim();

            if (Enum.TryParse<Gender>(gender, out var parsedGender))
                child.Gender = parsedGender;
            else
            {
                TempData["Error"] = "Invalid gender.";
                return RedirectToAction("ChildEdit", new { childId });
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Child updated successfully.";
            return RedirectToAction("ChildEdit", new { childId });
        }

        // POST: Update Parent Info
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> UpdateParentInfo(
            string UserId,
            int ChildId,
            string ParentGuardianName,
            string RelationshipToChild,
            string TeleNumber,
            string ParentEmail,
            string Postcode)
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
            return RedirectToAction("ChildEdit", new { childId = ChildId });
        }

        // POST: Log Measurement
        [HttpPost]
        [Authorize(Roles = "Admin,Employee")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LogMeasurement(WeeklyMeasurements model)
        {
            ModelState.Remove("Child");
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Invalid data. Please fill all required fields.";
                return RedirectToAction("ChildEdit", new { childId = model.ChildId });
            }

            var child = await _context.Children.FindAsync(model.ChildId);
            if (child == null)
            {
                TempData["Error"] = "Child not found.";
                return RedirectToAction("Index");
            }

            if (model.DateRecorded == default)
            {
                model.DateRecorded = DateTime.UtcNow;
            }

            _context.WeeklyMeasurements.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Measurement logged successfully.";
            return RedirectToAction("ChildEdit", new { childId = model.ChildId });
        }

        // POST: Delete Measurement
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> DeleteMeasurement(int id, int childId)
        {
            var record = await _context.WeeklyMeasurements.FindAsync(id);
            if (record != null)
            {
                _context.WeeklyMeasurements.Remove(record);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Measurement deleted successfully.";
            }
            else
            {
                TempData["Error"] = "Measurement not found.";
            }

            return RedirectToAction("ChildEdit", new { childId });
        }

        // POST: Add or Update Health Score
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> AddOrUpdateHealthScore(AdminHealthScoreViewModel model, int childId)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Invalid input. Please review and try again.";
                return RedirectToAction("ChildEdit", new { childId });
            }

            if (model.PhysicalActivityScore < 0 || model.PhysicalActivityScore > 4 ||
                model.BreakfastScore < 0 || model.BreakfastScore > 4 ||
                model.FruitVegScore < 0 || model.FruitVegScore > 4 ||
                model.SweetSnacksScore < 0 || model.SweetSnacksScore > 4 ||
                model.FattyFoodsScore < 0 || model.FattyFoodsScore > 4)
            {
                TempData["Error"] = "All scores must be selected from the dropdown.";
                return RedirectToAction("ChildEdit", new { childId });
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
                ChildId = childId,
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
            return RedirectToAction("ChildEdit", new { childId });
        }

        // POST: Delete Health Score
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> DeleteHealthScore(int id, int childId)
        {
            var score = await _context.HealthScores.FindAsync(id);
            if (score != null)
            {
                _context.HealthScores.Remove(score);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Health score deleted successfully.";
            }
            else
            {
                TempData["Error"] = "Health score not found.";
            }

            return RedirectToAction("ChildEdit", new { childId });
        }

        // POST: Send Notification
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> SendNotification(string UserId, int ChildId, string NotificationMessage)
        {
            if (string.IsNullOrWhiteSpace(NotificationMessage))
            {
                TempData["Error"] = "Notification message cannot be empty.";
                return RedirectToAction("ChildEdit", new { childId = ChildId });
            }

            var user = await _context.Users.FindAsync(UserId);
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("Index");
            }

            var notification = new Notification
            {
                UserId = UserId,
                Message = NotificationMessage,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Notification sent successfully.";
            return RedirectToAction("ChildEdit", new { childId = ChildId });
        }

        private static string GetHealthRange(int centileScore)
        {
            if (centileScore < 2)
                return "Underweight";
            else if (centileScore >= 2 && centileScore <= 90)
                return "Healthy Weight";
            else
                return "Overweight";
        }

        private async Task<SchoolRecordsViewModel> GetSchoolRecordsData(string schoolName)
        {
            var childrenBySchool = await _context.Children
                .Include(c => c.User)
                .Include(c => c.HealthScores)
                .Include(c => c.WeeklyMeasurements)
                .Include(c => c.AdminNotes)
                .Where(c => c.School == schoolName && !c.IsDeleted)
                .ToListAsync();

            var userIdsBySchool = childrenBySchool
                .Select(c => c.UserId)
                .Distinct()
                .ToList();

            var personalDetailsBySchool = await _context.PersonalDetails
                .Include(p => p.User)
                .Where(p => userIdsBySchool.Contains(p.UserId))
                .ToListAsync();

            var childIds = childrenBySchool.Select(c => c.Id).ToList();

            var healthScores = await _context.HealthScores
                .Include(h => h.Child)
                .Where(h => childIds.Contains(h.ChildId) && !h.IsDeleted)
                .OrderByDescending(h => h.DateRecorded)
                .ToListAsync();

            var measurements = await _context.WeeklyMeasurements
                .Include(m => m.Child)
                .Where(m => childIds.Contains(m.ChildId))
                .OrderByDescending(m => m.DateRecorded)
                .ToListAsync();

            return new SchoolRecordsViewModel
            {
                SchoolName = schoolName,
                Children = childrenBySchool,
                PersonalDetails = personalDetailsBySchool,
                HealthScores = healthScores,
                Measurements = measurements,
                TotalChildrenRecords = childrenBySchool.Count,
                TotalPersonalDetailsRecords = personalDetailsBySchool.Count,
                TotalHealthScores = healthScores.Count,
                TotalMeasurements = measurements.Count
            };
        }

        // GET: Export school data to Word document
        [HttpGet]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> ExportToWord(string schoolName, string className = "")
        {
            if (string.IsNullOrWhiteSpace(schoolName))
            {
                TempData["Error"] = "School name is required.";
                return RedirectToAction("Index");
            }

            // Get children data filtered by school and optionally by class
            var query = _context.Children
                .Include(c => c.WeeklyMeasurements)
                .Where(c => c.School == schoolName && !c.IsDeleted);

            if (!string.IsNullOrWhiteSpace(className))
            {
                query = query.Where(c => c.Class == className);
            }

            var children = await query
                .OrderBy(c => c.Class)
                .ThenBy(c => c.ChildName)
                .ToListAsync();

            if (!children.Any())
            {
                TempData["Error"] = "No records found for the selected criteria.";
                return RedirectToAction("ViewSchoolRecords", new { schoolName });
            }

            // Create Word document in memory
            using var memoryStream = new MemoryStream();
            using (var wordDocument = WordprocessingDocument.Create(memoryStream, WordprocessingDocumentType.Document, true))
            {
                // Add main document part
                var mainPart = wordDocument.AddMainDocumentPart();
                mainPart.Document = new Document();
                var body = mainPart.Document.AppendChild(new Body());

                // Add title
                var titlePara = body.AppendChild(new Paragraph());
                var titleRun = titlePara.AppendChild(new Run());
                titleRun.AppendChild(new RunProperties(
                    new Bold(),
                    new FontSize() { Val = "32" }
                ));
                titleRun.AppendChild(new Text(schoolName));

                // Add subtitle with class filter if applicable
                var subtitlePara = body.AppendChild(new Paragraph());
                var subtitleRun = subtitlePara.AppendChild(new Run());
                subtitleRun.AppendChild(new RunProperties(
                    new FontSize() { Val = "24" }
                ));
                var subtitleText = string.IsNullOrWhiteSpace(className)
                    ? "All Classes - Student Health Records"
                    : $"Class: {className} - Student Health Records";
                subtitleRun.AppendChild(new Text(subtitleText));

                // Add date
                var datePara = body.AppendChild(new Paragraph());
                var dateRun = datePara.AppendChild(new Run());
                dateRun.AppendChild(new Text($"Generated: {DateTime.Now:dd MMM yyyy HH:mm}"));

                // Add spacing
                body.AppendChild(new Paragraph());

                // Create table
                var table = new Table();

                // Table properties
                var tableProperties = new TableProperties(
                    new TableBorders(
                        new TopBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 12 },
                        new BottomBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 12 },
                        new LeftBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 12 },
                        new RightBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 12 },
                        new InsideHorizontalBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 12 },
                        new InsideVerticalBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 12 }
                    )
                );
                table.AppendChild(tableProperties);

                // Table header
                var headerRow = new TableRow();
                AddTableCell(headerRow, "S.No", true);
                AddTableCell(headerRow, "Child Name", true);
                AddTableCell(headerRow, "Class", true);
                AddTableCell(headerRow, "DOB", true);
                AddTableCell(headerRow, "Weight (kg)", true);
                AddTableCell(headerRow, "Height (cm)", true);
                table.AppendChild(headerRow);

                // Table data rows
                int serialNumber = 1;
                foreach (var child in children)
                {
                    var latestMeasurement = child.WeeklyMeasurements
                        .OrderByDescending(m => m.DateRecorded)
                        .FirstOrDefault();

                    var dataRow = new TableRow();
                    AddTableCell(dataRow, serialNumber.ToString());
                    AddTableCell(dataRow, child.ChildName ?? "N/A");
                    AddTableCell(dataRow, child.Class ?? "N/A");
                    AddTableCell(dataRow, child.DateOfBirth.ToString("dd/MM/yyyy"));
                    AddTableCell(dataRow, latestMeasurement != null ? latestMeasurement.Weight.ToString("F1") : "-");
                    AddTableCell(dataRow, latestMeasurement != null ? latestMeasurement.Height.ToString("F1") : "-");
                    table.AppendChild(dataRow);

                    serialNumber++;
                }

                body.AppendChild(table);

                // Add footer note
                body.AppendChild(new Paragraph());
                var footerPara = body.AppendChild(new Paragraph());
                var footerRun = footerPara.AppendChild(new Run());
                footerRun.AppendChild(new RunProperties(new Italic()));
                footerRun.AppendChild(new Text($"Total Students: {children.Count}"));

                mainPart.Document.Save();
            }

            // Return file
            memoryStream.Position = 0;
            var fileName = string.IsNullOrWhiteSpace(className)
                ? $"{schoolName.Replace(" ", "_")}_AllClasses_{DateTime.Now:yyyyMMdd}.docx"
                : $"{schoolName.Replace(" ", "_")}_{className.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd}.docx";

            return File(memoryStream.ToArray(),
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                fileName);
        }

        // Helper method to add table cells
        private void AddTableCell(TableRow row, string text, bool isHeader = false)
        {
            var cell = new TableCell();
            var paragraph = new Paragraph();
            var run = new Run();

            if (isHeader)
            {
                run.AppendChild(new RunProperties(new Bold()));
            }

            run.AppendChild(new Text(text));
            paragraph.AppendChild(run);
            cell.AppendChild(paragraph);
            row.AppendChild(cell);
        }
    }
}
