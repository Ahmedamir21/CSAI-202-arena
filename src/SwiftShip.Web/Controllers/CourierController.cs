using Microsoft.AspNetCore.Mvc;
using SwiftShip.Web.Models;
using SwiftShip.Web.Services;
using SwiftShip.Web.ViewModels;

namespace SwiftShip.Web.Controllers
{
    /// <summary>The courier's day.</summary>
    [RequireRole(Models.Roles.Courier)]
    public class CourierController : AppController
    {
        private readonly CourierService _couriers;
        private readonly LookupService _lookups;

        public CourierController(CourierService couriers, LookupService lookups)
        {
            _couriers = couriers;
            _lookups = lookups;
        }

        /// <summary>Today's pickups and deliveries, with names and addresses, not ids.</summary>
        [HttpGet]
        public IActionResult Today(DateTime? date)
        {
            return View(new CourierTodayViewModel
            {
                Date = date ?? DateTime.Today,
                Tasks = _couriers.Tasks(Me.UserID, date)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Pickup(int assignmentId, int shipmentId, string notes)
        {
            var result = _couriers.PickUp(shipmentId, assignmentId, Me.UserID, notes);
            if (result.Success)
                Succeed("The pickup has been recorded.");
            else
                Fail(result.Error);
            return RedirectToAction("Today", "Courier");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CompleteTask(int assignmentId, string notes)
        {
            var result = _couriers.CompletePickup(assignmentId, Me.UserID, notes);
            if (result.Success)
                Succeed("The task has been closed.");
            else
                Fail(result.Error);
            return RedirectToAction("Today", "Courier");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult StartDelivery(int shipmentId)
        {
            var result = _couriers.StartDelivery(shipmentId, Me.UserID);
            if (result.Success)
                Succeed("The shipment is out for delivery.");
            else
                Fail(result.Error);
            return RedirectToAction("Today", "Courier");
        }

        /// <summary>Proof of delivery: the name of the receiver and the one-time code.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Deliver(int shipmentId, string receiverName, string proofCode, string notes)
        {
            var result = _couriers.Deliver(shipmentId, Me.UserID, receiverName, proofCode, notes);
            if (result.Success)
                Succeed("Delivered. The cash on delivery was recorded too.");
            else
                Fail(result.Error);
            return RedirectToAction("Today", "Courier");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult FailDelivery(int shipmentId, string reason)
        {
            var result = _couriers.FailDelivery(shipmentId, Me.UserID, reason);
            if (result.Success)
                Succeed("The failed attempt has been recorded, the branch will see it.");
            else
                Fail(result.Error);
            return RedirectToAction("Today", "Courier");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CollectCash(int shipmentId)
        {
            var result = _couriers.CollectCash(shipmentId, Me.UserID);
            if (result.Success)
                Succeed("The cash has been recorded.");
            else
                Fail(result.Error);
            return RedirectToAction("Today", "Courier");
        }

        [HttpGet]
        public IActionResult History(DateTime? from, DateTime? to, int? statusId)
        {
            return View(new CourierHistoryViewModel
            {
                Shipments = _couriers.History(Me.UserID, from, to, statusId),
                FromDate = from,
                ToDate = to,
                StatusID = statusId,
                Statuses = Select(_lookups.Statuses(), s => s.Id, s => s.Name, "-- every status --")
            });
        }

        /// <summary>His own performance report.</summary>
        [HttpGet]
        public IActionResult Performance()
        {
            var perDay = new List<CourierRepository.DailyPoint>();
            var summary = _couriers.Performance(Me.UserID, perDay);
            return View(new CourierPerformanceViewModel { Summary = summary, PerDay = perDay });
        }
    }
}
