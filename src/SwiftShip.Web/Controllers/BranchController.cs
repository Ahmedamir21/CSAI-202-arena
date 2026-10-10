using Microsoft.AspNetCore.Mvc;
using SwiftShip.Web.Models;
using SwiftShip.Web.Services;
using SwiftShip.Web.ViewModels;

namespace SwiftShip.Web.Controllers
{
    /// <summary>The work of a branch. Every action is limited to the branch of the employee.</summary>
    [RequireRole(Models.Roles.BranchEmployee)]
    public class BranchController : AppController
    {
        private readonly BranchService _branch;
        private readonly LookupService _lookups;
        private readonly AdminService _admin;
        private readonly CustomerService _customers;

        public BranchController(BranchService branch, LookupService lookups,
                                AdminService admin, CustomerService customers)
        {
            _branch = branch;
            _lookups = lookups;
            _admin = admin;
            _customers = customers;
        }

        /// <summary>The branch dashboard: the counters, the recent parcels, the couriers on duty.</summary>
        [HttpGet]
        public IActionResult Index()
        {
            return View(new BranchDashboardViewModel
            {
                Dashboard = _branch.Dashboard(Me.BranchID ?? 0)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Receive(string trackingNumber, string notes)
        {
            var result = _branch.Receive(trackingNumber, Me.BranchID ?? 0, Me.UserID, notes);
            if (result.Success)
                Succeed(trackingNumber.ToUpperInvariant() + " has been received at the branch.");
            else
                Fail(result.Error);
            return RedirectToAction("Index", "Branch");
        }

        /// <summary>Assigning a courier, chosen by name with his workload next to it.</summary>
        [HttpGet]
        public IActionResult Assign(string search)
        {
            var model = new AssignCourierViewModel
            {
                Shipments = _branch.Shipments(Me.BranchID ?? 0)
                                   .Where(s => s.CurrentStatusID != 6 && s.CurrentStatusID != 8 && s.CurrentStatusID != 9)
                                   .ToList(),
                Couriers = _lookups.BranchCouriers(Me.BranchID ?? 0),
                Search = search
            };

            if (!string.IsNullOrWhiteSpace(search))
            {
                string term = search.Trim().ToLowerInvariant();
                model.Shipments = model.Shipments
                    .Where(s => s.TrackingNumber.ToLowerInvariant().Contains(term)
                             || s.ReceiverName.ToLowerInvariant().Contains(term)
                             || s.DestinationCity.ToLowerInvariant().Contains(term))
                    .ToList();
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Assign(AssignCourierViewModel model)
        {
            var result = _branch.Assign(model.ShipmentID, model.CourierID, model.AssignmentType, Me.UserID);
            if (result.Success)
                Succeed("The courier has been assigned.");
            else
                Fail(result.Error);
            return RedirectToAction("Assign", "Branch", new { search = model.Search });
        }

        /// <summary>An order for somebody who walked into the branch.</summary>
        [HttpGet]
        public IActionResult WalkIn(string customerSearch, int? customerId)
        {
            var model = new WalkInViewModel
            {
                CustomerSearch = customerSearch,
                Customers = string.IsNullOrWhiteSpace(customerSearch)
                    ? new List<CustomerAdminRow>()
                    : _admin.Customers(customerSearch),
                Cities = Select(_lookups.Cities(), c => c.CityID,
                                c => c.CityName + " (" + c.GovernorateName + ")", "-- choose a city --"),
                PackageTypes = Select(_lookups.PackageTypes(), p => p.Id, p => p.ToString(), null),
                ServiceLevels = Select(_lookups.ServiceLevels(), s => s.Id, s => s.ToString(), null),
                PaymentMethods = Select(_lookups.PaymentMethods(), p => p.Id, p => p.Name, null)
            };

            if (customerId.HasValue)
            {
                model.CustomerID = customerId.Value;
                // the addresses of that customer, found through the report that lists them
                model.Addresses = _customers.Addresses(customerId.Value);
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult WalkIn(WalkInViewModel model)
        {
            var result = _branch.WalkIn(Me.UserID, Me.BranchID ?? 0, model.CustomerID,
                                        model.SenderAddressID, model.ReceiverName, model.ReceiverPhone,
                                        model.ReceiverStreet, model.ReceiverBuildingNo,
                                        model.ReceiverApartmentNo, model.ReceiverFloorNo,
                                        model.ReceiverLandmark, model.ReceiverCityID,
                                        model.PackageTypeID, model.ServiceLevelID, model.WeightKg,
                                        model.DeclaredValue, model.IsInsured, model.PaymentMethod);
            if (result.Success)
                Succeed("The order has been created and received at the branch.");
            else
                Fail(result.Error);

            return RedirectToAction("WalkIn", "Branch");
        }

        /// <summary>Sending a parcel to another branch and receiving the incoming ones.</summary>
        [HttpGet]
        public IActionResult Transfers()
        {
            return View(new TransfersViewModel
            {
                Incoming = _branch.Incoming(Me.BranchID ?? 0),
                Shipments = _branch.Shipments(Me.BranchID ?? 0)
                                   .Where(s => s.CurrentStatusID == 3)
                                   .ToList(),
                Branches = Select(_lookups.Branches().Where(b => b.BranchID != Me.BranchID),
                                  b => b.BranchID, b => b.BranchName + " - " + b.CityName,
                                  "-- choose a branch --")
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Transfer(TransfersViewModel model)
        {
            var result = _branch.Transfer(model.ShipmentID, Me.BranchID ?? 0, model.ToBranchID, Me.UserID);
            if (result.Success)
                Succeed("The shipment has been dispatched.");
            else
                Fail(result.Error);
            return RedirectToAction("Transfers", "Branch");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ReceiveTransfer(int transferId)
        {
            var result = _branch.ReceiveTransfer(transferId, Me.UserID, Me.BranchID ?? 0);
            if (result.Success)
                Succeed("The transfer has been received.");
            else
                Fail(result.Error);
            return RedirectToAction("Transfers", "Branch");
        }

        /// <summary>Failed deliveries: book another attempt or send the parcel back.</summary>
        [HttpGet]
        public IActionResult Failed()
        {
            return View(new BranchFailedViewModel
            {
                Shipments = _branch.FailedDeliveries(Me.BranchID ?? 0)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Failed(BranchFailedViewModel model)
        {
            Result result = model.Action == "Return"
                ? _branch.ReturnToSender(model.ShipmentID, Me.UserID, model.Notes)
                : _branch.Reschedule(model.ShipmentID, Me.UserID, model.Notes);

            if (result.Success)
                Succeed(model.Action == "Return"
                    ? "The shipment is going back to the sender."
                    : "A new delivery attempt has been scheduled.");
            else
                Fail(result.Error);

            return RedirectToAction("Failed", "Branch");
        }

        /// <summary>The complaints about the parcels this branch handled.</summary>
        [HttpGet]
        public IActionResult Complaints()
        {
            return View(new BranchComplaintsViewModel
            {
                Complaints = _branch.Complaints(Me.BranchID ?? 0)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Complaints(BranchComplaintsViewModel model)
        {
            var result = _branch.AnswerComplaint(model.ComplaintID, Me.UserID, model.Resolution, model.CloseIt);
            if (result.Success)
                Succeed(model.CloseIt ? "The complaint has been closed." : "Your answer has been saved.");
            else
                Fail(result.Error);
            return RedirectToAction("Complaints", "Branch");
        }

        /// <summary>The label that gets printed and stuck on the parcel.</summary>
        [HttpGet]
        public IActionResult Waybill(int id)
        {
            var waybill = _branch.Waybill(id);
            if (waybill == null)
            {
                Fail("This shipment does not exist.");
                return RedirectToAction("Index", "Branch");
            }
            return View(waybill);
        }
    }
}
