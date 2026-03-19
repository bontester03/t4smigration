using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebApit4s.DAL;
using WebApit4s.Models;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using WebApit4s.Identity; // for ApplicationUser


namespace WebApit4s.Controllers
{
    public class PersonalDetailsController : Controller
    {
        private readonly TimeContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public PersonalDetailsController(TimeContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }


        // GET: PersonalDetails
        public async Task<IActionResult> Index()
        {
            var timeContext = _context.PersonalDetails.Include(p => p.User);
            return View(await timeContext.ToListAsync());
        }

        // GET: PersonalDetails/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var personalDetails = await _context.PersonalDetails
                .Include(p => p.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (personalDetails == null)
            {
                return NotFound();
            }

            return View(personalDetails);
        }

        // GET: PersonalDetails/Create
        [HttpGet]
        public async Task<IActionResult> Create(string? userId = null)
        {
            var effectiveUserId = userId ?? _userManager.GetUserId(User);
            var user = await _userManager.FindByIdAsync(effectiveUserId);
            if (user == null) return RedirectToAction("Login", "Account");

            await PopulateViewBagsAsync(effectiveUserId);
            return View(new PersonalDetails { UserId = effectiveUserId });
        }






        // POST: PersonalDetails/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PersonalDetails personalDetails)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Attach UserId to PersonalDetails
            personalDetails.UserId = user.Id;

            // Get ReferralType to handle self-referral logic
            var referralType = await _context.ReferralTypes
                .FirstOrDefaultAsync(r => r.Id == user.ReferralTypeId);

            // Prevent ModelState error on UserId
            ModelState.Remove("UserId");

            if (!ModelState.IsValid)
            {
                await PopulateViewBagsAsync(user.Id);
                return View(personalDetails);
            }

            // ✅ Save and redirect to next step (Child creation)
            _context.Add(personalDetails);
            await _context.SaveChangesAsync();

            return RedirectToAction("Create", "Child");
        }


        private async Task PopulateViewBagsAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            var referralType = await _context.ReferralTypes.FirstOrDefaultAsync(r => r.Id == user.ReferralTypeId);

            ViewBag.RequiresSchoolSelection = referralType?.RequiresSchoolSelection ?? false;

            ViewBag.SchoolOptions = new SelectList(
                await _context.Schools.Where(s => s.IsActive).OrderBy(s => s.Name).ToListAsync(),
                "Name", "Name");

            ViewBag.ClassOptions = new SelectList(
                await _context.Classes.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync(),
                "Name", "Name");

            ViewBag.RelationshipOptions = new SelectList(new List<string> { "Father", "Mother", "Guardian", "Others" });
        }



        // GET: PersonalDetails/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var personalDetails = await _context.PersonalDetails.FindAsync(id);
            if (personalDetails == null)
            {
                return NotFound();
            }
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id", personalDetails.UserId);
            return View(personalDetails);
        }

        // POST: PersonalDetails/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,ParentGuardianName,RelationshipToChild,TeleNumber,Email,Postcode,UserId")] PersonalDetails personalDetails)
        {
            if (id != personalDetails.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(personalDetails);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PersonalDetailsExists(personalDetails.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id", personalDetails.UserId);
            return View(personalDetails);
        }

        // GET: PersonalDetails/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var personalDetails = await _context.PersonalDetails
                .Include(p => p.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (personalDetails == null)
            {
                return NotFound();
            }

            return View(personalDetails);
        }

        // POST: PersonalDetails/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var personalDetails = await _context.PersonalDetails.FindAsync(id);
            if (personalDetails != null)
            {
                _context.PersonalDetails.Remove(personalDetails);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PersonalDetailsExists(int id)
        {
            return _context.PersonalDetails.Any(e => e.Id == id);
        }
    }
}
