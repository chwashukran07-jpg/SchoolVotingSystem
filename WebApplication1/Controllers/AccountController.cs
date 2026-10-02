using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using VotingSystem.Models;

namespace VotingSystem.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private ApplicationUserManager UserManager =>
            HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();

        private ApplicationSignInManager SignInManager =>
            HttpContext.GetOwinContext().Get<ApplicationSignInManager>();

        private IAuthenticationManager AuthenticationManager =>
            HttpContext.GetOwinContext().Authentication;

        private ApplicationDbContext Db =>
            HttpContext.GetOwinContext().Get<ApplicationDbContext>();

        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            // redirect to admin login if the returnUrl is for admin area
            if (returnUrl != null && returnUrl.StartsWith("/Admin", StringComparison.OrdinalIgnoreCase))
                return RedirectToAction("AdminLogin", new { returnUrl });

            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginViewModel model, string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            if (!ModelState.IsValid) return View(model);

            model.UserName = Student.Normalize(model.UserName);
            if (!await SignInWithRoleAsync(model, "Voter"))
            {
                ModelState.AddModelError("", "Invalid Student ID or password.");
                return View(model);
            }

            if (Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
            return RedirectToAction("Index", "Vote");
        }

        // election admin login (username and password)
        [AllowAnonymous]
        public ActionResult AdminLogin(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
        public async Task<ActionResult> AdminLogin(LoginViewModel model, string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            if (!ModelState.IsValid) return View(model);

            model.UserName = model.UserName.Trim();
            if (!await SignInWithRoleAsync(model, "Admin"))
            {
                ModelState.AddModelError("", "Invalid admin username or password.");
                return View(model);
            }

            if (Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
            return RedirectToAction("Index", "Admin");
        }

        // each form to allow logins from its roles
        private async Task<bool> SignInWithRoleAsync(LoginViewModel model, string role)
        {
            var user = await UserManager.FindByNameAsync(model.UserName);
            if (user == null || !await UserManager.IsInRoleAsync(user.Id, role)) return false;

            var result = await SignInManager.PasswordSignInAsync(model.UserName, model.Password, model.RememberMe, shouldLockout: false);
            return result == SignInStatus.Success;
        }

        // a student to create a new account (Student id+password) only if the tudent id is on the voter list
        [AllowAnonymous]
        public ActionResult Register()
        {
            return View();
        }

        [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var number = Student.Normalize(model.StudentNumber);
            var student = Db.Students.FirstOrDefault(s => s.StudentNumber == number);
            if (student == null)
            {
                ModelState.AddModelError("StudentNumber", "This Student ID is not on the voter list. Please contact the election admin.");
                return View(model);
            }

            // making sure the student number is not already registered
            if (await UserManager.FindByNameAsync(number) != null)
            {
                ModelState.AddModelError("StudentNumber", "An account already exists for this Student ID. Please log in instead.");
                return View(model);
            }

            var user = new ApplicationUser { UserName = number };
            var result = await UserManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error);
                return View(model);
            }

            await UserManager.AddToRoleAsync(user.Id, "Voter");
            await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);

            TempData["Message"] = "Welcome, " + student.FullName + "! Your voter account is ready.";
            return RedirectToAction("Index", "Vote");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult LogOff()
        {
            AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
            return RedirectToAction("Index", "Home");
        }
    }
}
