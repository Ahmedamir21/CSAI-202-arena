using Microsoft.AspNetCore.Mvc;
using SwiftShip.Web.Services;
using SwiftShip.Web.ViewModels;

namespace SwiftShip.Web.Controllers
{
    /// <summary>Sign up, log in, log out, change password, edit the profile.</summary>
    public class AccountController : AppController
    {
        private readonly AccountService _accounts;

        public AccountController(AccountService accounts)
        {
            _accounts = accounts;
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            if (Me != null)
                return SendHome();
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var result = _accounts.Login(model.Username, model.Password);
            if (!result.Success)
            {
                model.Error = result.Error;
                return View(model);
            }

            var user = new CurrentUser
            {
                UserID = result.Value.UserID,
                Username = result.Value.Username,
                Role = result.Value.Role,
                DisplayName = result.Value.DisplayName ?? result.Value.Username,
                BranchID = result.Value.BranchID,
                BranchName = result.Value.BranchName,
                MustChangePassword = result.Value.MustChangePassword
            };
            user.Save(HttpContext.Session);

            if (!string.IsNullOrEmpty(model.ReturnUrl) && model.ReturnUrl.StartsWith("/"))
                return Redirect(model.ReturnUrl);

            return SendHome();
        }

        [HttpGet]
        public IActionResult SignUp()
        {
            return View(new SignUpViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SignUp(SignUpViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var result = _accounts.SignUp(model.Username, model.Password, model.ConfirmPassword,
                                          model.FullName, model.Email, model.Phone,
                                          model.CustomerType, model.CompanyName);
            if (!result.Success)
            {
                model.Error = result.Error;
                return View(model);
            }

            Succeed("Your account is ready. Please sign in.");
            return RedirectToAction("Login", "Account");
        }

        [RequireRole]
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View(new ChangePasswordViewModel
            {
                Username = Me.Username,
                FirstLogin = Me.MustChangePassword
            });
        }

        [RequireRole]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ChangePassword(ChangePasswordViewModel model)
        {
            model.Username = Me.Username;

            var result = _accounts.ChangePasswordFor(Me.Username, model.CurrentPassword,
                                                     model.NewPassword, model.ConfirmPassword);
            if (!result.Success)
            {
                model.Error = result.Error;
                return View(model);
            }

            // the flag is cleared in the session too, otherwise the filter sends him back here
            Me.MustChangePassword = false;
            Me.Save(HttpContext.Session);

            Succeed("Your password has been changed.");
            return SendHome();
        }

        [RequireRole]
        [HttpGet]
        public IActionResult Profile()
        {
            var profile = _accounts.Profile(Me.UserID);
            return View(new ProfileViewModel
            {
                CustomerID = Me.UserID,
                FullName = profile.FullName,
                Email = profile.Email,
                Phone = profile.Phone,
                CustomerType = profile.CustomerType,
                CompanyName = profile.CompanyName
            });
        }

        [RequireRole(Models.Roles.Customer)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Profile(ProfileViewModel model)
        {
            var result = _accounts.UpdateProfile(Me.UserID, model.FullName, model.Email, model.Phone);
            if (!result.Success)
            {
                model.Error = result.Error;
                return View(model);
            }

            Me.DisplayName = model.FullName;
            Me.Save(HttpContext.Session);

            Succeed("Your profile has been saved.");
            return RedirectToAction("Profile", "Account");
        }

        [RequireRole]
        public IActionResult Logout()
        {
            CurrentUser.Clear(HttpContext.Session);
            return RedirectToAction("Index", "Home");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

        /// <summary>Everybody lands on the home page of his own role.</summary>
        private IActionResult SendHome()
        {
            switch (Me.Role)
            {
                case Models.Roles.Admin:
                    return RedirectToAction("Index", "Admin");
                case Models.Roles.Courier:
                    return RedirectToAction("Today", "Courier");
                case Models.Roles.BranchEmployee:
                    return RedirectToAction("Index", "Branch");
                default:
                    return RedirectToAction("Index", "Customer");
            }
        }
    }
}
