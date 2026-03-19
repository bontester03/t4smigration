using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using WebApit4s.Identity;
using WebApit4s.Models;
using WebApit4s.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using WebApit4s.DAL;
using Microsoft.AspNetCore.Identity.UI.Services;
using WebApit4s.Utilities;


namespace WebApit4s.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly TimeContext _context;
        private readonly IEmailSender _emailSender;



        public AccountController(UserManager<ApplicationUser> userManager, 
            SignInManager<ApplicationUser> signInManager, TimeContext context,IEmailSender emailSender )
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _emailSender = emailSender;
        }

       

       

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);
            try
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
            
                if (user == null)
            {
                ModelState.AddModelError(string.Empty, "User not found.");
                return View(model);
            }

            if (!await _userManager.CheckPasswordAsync(user, model.Password))
            {
                ModelState.AddModelError(string.Empty, "Incorrect password.");
                return View(model);
            }

            if (!user.EmailConfirmed)
            {
                ModelState.AddModelError(string.Empty, "Email is not confirmed.");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(user, model.Password, false, false);

            if (result.Succeeded)
            {
                if (await _userManager.IsInRoleAsync(user, "Admin"))
                {
                    return RedirectToAction("Index", "AdminDashboard");
                }
                else if (await _userManager.IsInRoleAsync(user, "Employee"))
                {
                    return RedirectToAction("Index", "AdminDashboard");
                }

                return RedirectToAction("LandingPage", "Dashboard");
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty, "Account is locked out.");
            }
            else if (result.IsNotAllowed)
            {
                ModelState.AddModelError(string.Empty, "Login not allowed. Contact administrator.");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Login failed for unknown reason.");
            }

            return View(model);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("more than one element"))
            {
                // Instead of crash -> show message
                ModelState.AddModelError(string.Empty, "Multiple accounts found with this email. Please reset your password.");
                return View(model);
            }
        }



        [HttpGet]
        public IActionResult Register()
        {
            return Redirect("/register/step-1");
        }



        [HttpPost]
        [ActionName("Register")]
        [ValidateAntiForgeryToken]
        public IActionResult RegisterPost()
        {
            return Redirect("/register/step-1");
        }




        [Authorize(Roles = "Admin,Employee")]
        [HttpGet]
        public IActionResult Create()
        {
            var model = new CreateUserViewModel();

            if (User.IsInRole("Admin"))
            {
                model.AvailableUserTypes = new List<UserType> { UserType.Parent, UserType.Employee, UserType.Guest };
            }
            else if (User.IsInRole("Employee"))
            {
                model.AvailableUserTypes = new List<UserType> { UserType.Parent, UserType.Guest };
            }

            return View(model);
        }


        [Authorize(Roles = "Admin,Employee")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            var allowedTypes = User.IsInRole("Admin")
                ? new List<UserType> { UserType.Parent, UserType.Employee, UserType.Guest }
                : new List<UserType> { UserType.Parent, UserType.Guest };

            if (!allowedTypes.Contains(model.SelectedUserType))
            {
                ModelState.AddModelError("", "You are not authorised to create this type of user.");
                model.AvailableUserTypes = allowedTypes;
                return View(model);
            }

            if (!ModelState.IsValid)
            {
                model.AvailableUserTypes = allowedTypes;
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.FullName,
                Email = model.Email,
                EmailConfirmed = true,
                UserType = model.SelectedUserType
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, model.SelectedUserType.ToString());
                return RedirectToAction("UserList");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            model.AvailableUserTypes = allowedTypes;
            return View(model);
        }



        [Authorize(Roles = "Admin,Employee")]

        public IActionResult UserList()
        {
            // Just returns the view — actual user data comes via AJAX
            return View();
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Employee")]

        public async Task<IActionResult> GetUsers(int page = 1, int pageSize = 20, string role = "")
        {
            var query = _userManager.Users.AsQueryable();

            if (!string.IsNullOrEmpty(role))
            {
                query = query.Where(u => u.UserType.ToString() == role);
            }

            var total = await query.CountAsync();

            var users = await query
                .OrderBy(u => u.RegistrationDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new
                {
                    u.Id,
                    u.Email,
                    UserType =  u.UserType.ToString(),
                    u.RegistrationDate,
                    IsApproved = u.IsApprovedByAdmin,
                    u.LockoutEnd,
                    IsLockedOut = u.LockoutEnd != null && u.LockoutEnd > DateTimeOffset.UtcNow
                })
                .ToListAsync();

            return Json(new
            {
                currentPage = page,
                totalPages = (int)Math.Ceiling(total / (double)pageSize),
                users
            });
        }







        [Authorize(Roles = "Admin,Employee")]
        [HttpPost]
        public async Task<IActionResult> ToggleUserLock(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.Now)
            {
                user.LockoutEnd = DateTimeOffset.Now;
            }
            else
            {
                user.LockoutEnd = DateTimeOffset.MaxValue;
            }

            await _userManager.UpdateAsync(user);
            return Ok();
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> DeleteUser(string id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null) return NotFound(new { error = "User not found" });

                // Try delete
                var result = await _userManager.DeleteAsync(user);

                if (!result.Succeeded)
                {
                    return BadRequest(new
                    {
                        error = "Identity delete failed",
                        details = result.Errors.Select(e => new { e.Code, e.Description })
                    });
                }

                return Ok(new { message = "Deleted" });
            }
            catch (DbUpdateException ex)
            {
                // Most common cause: FK constraint from your own tables (Children, PersonalDetails, etc.)
                return StatusCode(500, new
                {
                    error = "DbUpdateException (likely FK constraint)",
                    message = ex.InnerException?.Message ?? ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Exception", message = ex.Message });
            }
        }


        [Authorize(Roles = "Admin,Parent,Employee")]
        [HttpGet]
        public IActionResult AddChild(string parentId)
        {
            if (string.IsNullOrEmpty(parentId))
                return BadRequest("Parent ID is required.");

            var model = new CreateChildViewModel
            {
                ParentId = parentId
            };

            return View(model);
        }


        [Authorize(Roles = "Admin,Employee")]

        [HttpPost]
        public async Task<IActionResult> AddChild(CreateChildViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var child = new Child
            {
                UserId = model.ParentId,
                ChildName = model.ChildName,
                Gender = model.Gender,
                DateOfBirth = DateTimeUtils.EnsureUtc(model.DateOfBirth),
                LastLogin = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                ChildGuid = Guid.NewGuid(),
                IsDeleted = false,
                Level = 1,
                TotalPoints = 0,
                EngagementStatus = EngagementStatus.Engaged
            };

            _context.Children.Add(child);
            await _context.SaveChangesAsync();

            return RedirectToAction("UserList");
        }




        [Authorize(Roles = "Admin,Employee")]

        [HttpPost]
        public async Task<IActionResult> Approve(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                user.IsApprovedByAdmin = true;
                await _userManager.UpdateAsync(user);
            }
            return RedirectToAction("UserList");
        }


        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }


        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
            {
                return RedirectToAction("ForgotPasswordConfirmation");
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var callbackUrl = Url.Action("ResetPassword", "Account", new { token, email = model.Email }, Request.Scheme);

            var replacements = new Dictionary<string, string>
                     {
                            { "resetLink", callbackUrl }
                     };


            string htmlMessage;

            if (_emailSender is EmailSender concreteSender)
            {
                htmlMessage = concreteSender.LoadTemplate("PasswordResetEmail.html", replacements);
            }
            else
            {
                htmlMessage = $"Reset your password by <a href='{callbackUrl}'>clicking here</a>.";
            }

            await _emailSender.SendEmailAsync(model.Email, "Reset Your Password", htmlMessage);

            return RedirectToAction("ForgotPasswordConfirmation");
        }

        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }


        [HttpGet]
        public IActionResult ResetPassword(string token, string email)
        {
            if (token == null || email == null)
                return BadRequest("A token and email must be supplied for password reset.");

            return View(new ResetPasswordViewModel { Token = token, Email = email });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return RedirectToAction("ResetPasswordConfirmation");
            }

            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.NewPassword);
            if (result.Succeeded)
            {
                return RedirectToAction("ResetPasswordConfirmation");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }


    }
}
