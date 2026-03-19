using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using WebApit4s.Identity;
using WebApit4s.Models;
using WebApit4s.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebApit4s.DAL;
using Microsoft.AspNetCore.Identity.UI.Services;
using WebApit4s.Services;

namespace WebApit4s.Controllers
{
    [Authorize(Roles = "Admin")]
    public class KpiReportController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly TimeContext _context;
        private readonly IEmailSender _emailSender;
        private readonly KpiExportService _kpiExportService;



        public KpiReportController(UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager, TimeContext context, IEmailSender emailSender,
            KpiExportService kpiExportService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _emailSender = emailSender;
            _kpiExportService = kpiExportService;
        }

        // GET: /KpiReport/Index?year=2023
        public async Task<IActionResult> Index(int? year)
        {
            var selectedYear = year ?? DateTime.UtcNow.Year;
            var reports = await _context.kpiReports
                .Where(k => k.ReportingYear == selectedYear)
                .OrderBy(k => k.ContractRef)
                .ToListAsync();

            // Create view model with all sections
            var viewModel = new KpiReportViewModel
            {
                Year = selectedYear,
                KeyPerformanceIndicators = reports,
                ServiceLevel = new ServiceLevelData
                {
                    PreviousYear = reports.FirstOrDefault(r => r.ContractRef == "FWM 5")?.Q1 ?? 0
                },
                Checklist = new List<ChecklistItem>
                {
                    new ChecklistItem { Description = "Quarterly monitoring reports submitted" },
                    new ChecklistItem { Description = "Annual service review completed" },
                    new ChecklistItem { Description = "Safeguarding protocols followed" }
                }
            };

            ViewBag.SelectedYear = selectedYear;
            return View(viewModel);
        }

        public async Task<IActionResult> GenerateKpis(int year)
        {

            var kpis = new List<KpiReport>();

            // FWM 1 - Total Referrals
            int totalReferrals = await _context.Users.CountAsync(u => u.ReferralTypeId != null); // Adjust based on model
            kpis.Add(new KpiReport
            {
                ContractRef = "FWM 1",
                Measure = "Number of referrals received by the Service",
                Target = "N/A",
                InformationRequired = "The total number of referrals received by the Service",
                Q1 = totalReferrals, // Replace with real quarter filtering logic
                ReportingYear = year
            });

            // FWM 2 - NCMP Referrals
            int ncmpReferrals = await _context.Users
                .CountAsync(u => u.ReferralType != null && u.ReferralType.Category == ReferralCategory.NCMP);

            kpis.Add(new KpiReport
            {
                ContractRef = "FWM 2",
                Measure = "Number of referrals to the Service as a result of NCMP (where known)",
                Target = "N/A",
                InformationRequired = "The total number of referrals to the Service as a result of NCMP (where known)",
                Q1 = ncmpReferrals,
                ReportingYear = year
            });

            // FWM 3 - % families contacted within 2 days
            int contacted = await _context.RegistrationReminders
                .Where(r => r.SentAt.Year == year).CountAsync();
            int shouldHaveContacted = contacted; // Adjust this with actual logic if needed
            kpis.Add(new KpiReport
            {
                ContractRef = "FWM 3",
                Measure = "Percentage of Families contacted within two (2) Working Days of referral",
                Target = "100%",
                InformationRequired = "Families contacted within two (2) Working Days vs. Total",
                Q1 = contacted,
                PreviousYear = shouldHaveContacted,
                ReportingYear = year
            });

            // FWM 4 - % BMI Maintained or Reduced
            var exited = await _context.WeeklyMeasurements
                .Where(m => m.DateRecorded.Year == year)
                .GroupBy(m => m.ChildId)
                .ToListAsync();

            int maintained = exited.Count(g => g.OrderBy(m => m.DateRecorded).First().Weight >= g.OrderBy(m => m.DateRecorded).Last().Weight);
            int exitedTotal = exited.Count;

            kpis.Add(new KpiReport
            {
                ContractRef = "FWM 4",
                Measure = "Percentage of Service Users who maintain or reduce their baseline BMI (upon exiting the Service)",
                Target = "80%",
                InformationRequired = "Number who maintain/reduce BMI vs. those who exit",
                Q1 = maintained,
                PreviousYear = exitedTotal,
                ReportingYear = year
            });

            // FWM5: Number of Service Users engaged in the Service
          
            var fwm5EngagedUsers = await _context.Children
                .Where(c => c.EngagementStatus == EngagementStatus.Engaged && !c.IsDeleted)
                .CountAsync();

            kpis.Add(new KpiReport
            {
                ContractRef = "FWM 5",
                Measure = "Number of Service Users engaged in the Service",
                Target = "TBC",
                InformationRequired = "Include all eligible referrals and those who exited (not withdrawn/ineligible)",
                PreviousYear = null,
                Q1 = fwm5EngagedUsers, // ✅ You can assign this
                Q2 = null,
                Q3 = null,
                Q4 = null,
                ReportingYear = year
                // Do NOT assign YTD
            });


            // FWM6: Number of Service Users engaged in the Service
            int withdrawnCount = await _context.Children
    .Where(c => c.EngagementStatus == EngagementStatus.Withdrawn && !c.IsDeleted)
    .CountAsync();

            kpis.Add(new KpiReport
            {
                ContractRef = "FWM 6",
                Measure = "Number of Service Users who withdrew participation from the Service",
                Target = "N/A",
                InformationRequired = "The number of Service Users who withdrew participation from the Service",
                Q1 = withdrawnCount,
                ReportingYear = year
            });

            // FWM7: Number of Service Users engaged in the Service
            int ineligibleCount = await _context.Children
    .Where(c => c.EngagementStatus == EngagementStatus.Ineligible && !c.IsDeleted)
    .CountAsync();

            kpis.Add(new KpiReport
            {
                ContractRef = "FWM 7",
                Measure = "Number of ineligible referrals",
                Target = "N/A",
                InformationRequired = "The number of ineligible referrals",
                Q1 = ineligibleCount,
                ReportingYear = year
            });

            // FWM8: Number of Service Users engaged in the Service
            int totalReferralsFWM8 = await _context.Users
    .Where(u => u.ReferralTypeId != null)
    .CountAsync();

            int uncontactable = await _context.Children
                .Where(c => c.EngagementStatus == EngagementStatus.Uncontactable)
                .CountAsync();

            kpis.Add(new KpiReport
            {
                ContractRef = "FWM 8",
                Measure = "Percentage of families uncontactable after three attempts",
                Target = "N/A",
                InformationRequired = "Number of families uncontactable vs. total referrals",
                Q1 = uncontactable,
                PreviousYear = totalReferralsFWM8,
                ReportingYear = year
            });

            
            // FWM 9 - Percentage of Service Users who report achievement of their goals around healthy dietary behaviours
         

            // FWM10: Number of Service Users engaged in the Service

            // Save to DB (optional: clear existing first for the year)
            var existing = _context.kpiReports.Where(k => k.ReportingYear == year);
            _context.kpiReports.RemoveRange(existing);
            _context.kpiReports.AddRange(kpis);
            await _context.SaveChangesAsync();

            TempData["Success"] = "KPI data generated for the year " + year;
            return RedirectToAction("Index", new { year });
        }


        // GET: /KpiReport/Export?year=2024
        public async Task<IActionResult> Export(int year)
        {
            var reports = await _context.kpiReports
                .Where(r => r.ReportingYear == year)
                .ToListAsync();

            if (!reports.Any())
            {
                TempData["Error"] = $"No reports found for year {year}.";
                return RedirectToAction("Index", new { year });
            }

            // Generate Excel file
            var excelBytes = _kpiExportService.GenerateKpiExcelReport(reports, year);
            var fileName = $"KPI_Report_{year - 1}_{year}.xlsx";

            return File(excelBytes, 
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
                fileName);
        }
    }
}
