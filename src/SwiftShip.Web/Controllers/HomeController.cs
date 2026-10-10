using Microsoft.AspNetCore.Mvc;
using SwiftShip.Web.Services;

namespace SwiftShip.Web.Controllers
{
    public class HomeController : AppController
    {
        private readonly LookupService _lookups;

        public HomeController(LookupService lookups)
        {
            _lookups = lookups;
        }

        /// <summary>The landing page. Anybody can open it, nobody has to log in.</summary>
        public IActionResult Index()
        {
            // the city marquee and the "where do we deliver" line are real data
            ViewBag.Cities = _lookups.Cities().Select(c => c.CityName).Distinct().Take(12).ToList();
            return View();
        }

        /// <summary>Shown when something goes wrong that we did not expect.</summary>
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            ViewBag.Technical = HttpContext.TraceIdentifier;
            return View();
        }
    }
}
