using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SwiftShip.Web.Controllers
{
    /// <summary>
    /// Everything the controllers share: who is logged in, the messages that appear at the
    /// top of the page, and building the dropdown lists.
    /// </summary>
    public abstract class AppController : Controller
    {
        private CurrentUser _me;

        /// <summary>The account that is signed in.</summary>
        protected CurrentUser Me
        {
            get
            {
                if (_me == null)
                    _me = SwiftShip.Web.Services.CurrentUser.From(HttpContext.Session);
                return _me;
            }
        }

        public override void OnActionExecuting(Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext context)
        {
            base.OnActionExecuting(context);
            ViewBag.Me = Me;
            ViewBag.Error = TempData["Error"];
            ViewBag.Message = TempData["Message"];
        }

        /// <summary>Shows a red bar at the top of the next page.</summary>
        protected void Fail(string message)
        {
            TempData["Error"] = message;
        }

        /// <summary>Shows a green bar at the top of the next page.</summary>
        protected void Succeed(string message)
        {
            TempData["Message"] = message;
        }

        /// <summary>Builds a dropdown: the text is what the user reads, the value is the id.</summary>
        protected static List<SelectListItem> Select<T>(IEnumerable<T> items, Func<T, int> value,
                                                        Func<T, string> text, string prompt = null)
        {
            var list = items.Select(i => new SelectListItem
            {
                Value = value(i).ToString(),
                Text = text(i)
            }).ToList();

            if (prompt != null)
                list.Insert(0, new SelectListItem { Value = "", Text = prompt });

            return list;
        }

        /// <summary>A dropdown of plain words, for example the three vehicle types.</summary>
        protected static List<SelectListItem> SelectStrings(IEnumerable<string> items, string prompt = null)
        {
            var list = items.Select(s => new SelectListItem { Value = s, Text = s }).ToList();
            if (prompt != null)
                list.Insert(0, new SelectListItem { Value = "", Text = prompt });
            return list;
        }
    }
}
