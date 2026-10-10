using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SwiftShip.Web.Models;
using SwiftShip.Web.Services;

namespace SwiftShip.Web.Filters
{
    /// <summary>
    /// The gatekeeper. Put [RequireRole(Roles.Admin)] on a controller or on one action and
    /// nobody who is not logged in with that role can reach it, even by typing the URL.
    /// </summary>
    public class RequireRoleAttribute : ActionFilterAttribute
    {
        private readonly string[] _roles;

        public RequireRoleAttribute(params string[] roles)
        {
            _roles = roles;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var user = CurrentUser.From(context.HttpContext.Session);

            if (user == null)
            {
                string back = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;
                context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl = back });
                return;
            }

            if (_roles != null && _roles.Length > 0 && !_roles.Contains(user.Role))
            {
                context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
                return;
            }

            // The first admin (and every account an admin just created) has to change his
            // password before he can do anything else.
            if (user.MustChangePassword && !IsPasswordChange(context))
            {
                context.Result = new RedirectToActionResult("ChangePassword", "Account", null);
                return;
            }

            base.OnActionExecuting(context);
        }

        private static bool IsPasswordChange(ActionExecutingContext context)
        {
            string controller = context.RouteData.Values["controller"] as string;
            string action = context.RouteData.Values["action"] as string;
            return controller == "Account" &&
                   (action == "ChangePassword" || action == "Logout");
        }
    }
}
