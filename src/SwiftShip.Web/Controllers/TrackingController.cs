using Microsoft.AspNetCore.Mvc;
using SwiftShip.Web.Models;
using SwiftShip.Web.Services;
using SwiftShip.Web.ViewModels;

namespace SwiftShip.Web.Controllers
{
    /// <summary>The public tracking page. Nobody has to log in to use it.</summary>
    public class TrackingController : AppController
    {
        private readonly CustomerService _customers;

        public TrackingController(CustomerService customers)
        {
            _customers = customers;
        }

        [HttpGet]
        public IActionResult Index(string code = null)
        {
            var model = new TrackViewModel { TrackingNumber = code };

            if (!string.IsNullOrWhiteSpace(code))
            {
                var timeline = new List<TimelineRow>();
                var shipment = _customers.Track(code, timeline);
                if (shipment == null)
                    model.Error = "No shipment has the tracking number " + code.ToUpperInvariant() + ".";
                else
                {
                    model.Shipment = shipment;
                    model.Timeline = timeline;
                }
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(TrackViewModel model)
        {
            return RedirectToAction("Index", "Tracking", new { code = model.TrackingNumber });
        }
    }
}
