using Microsoft.AspNetCore.Mvc;
using SwiftShip.Web.Models;
using SwiftShip.Web.Services;
using SwiftShip.Web.ViewModels;

namespace SwiftShip.Web.Controllers
{
    /// <summary>Everything a customer can do.</summary>
    [RequireRole(Models.Roles.Customer)]
    public class CustomerController : AppController
    {
        private readonly CustomerService _customers;
        private readonly ShipmentService _shipments;
        private readonly LookupService _lookups;
        private readonly ReportService _reports;

        public CustomerController(CustomerService customers, ShipmentService shipments,
                                  LookupService lookups, ReportService reports)
        {
            _customers = customers;
            _shipments = shipments;
            _lookups = lookups;
            _reports = reports;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var all = _customers.MyShipments(Me.UserID, null, null, null, null, null);
            var model = new CustomerHomeViewModel
            {
                Recent = all.Take(5).ToList(),
                OnTheWay = all.Count(s => s.CurrentStatusID >= 2 && s.CurrentStatusID <= 5),
                Delivered = all.Count(s => s.CurrentStatusID == 6),
                Open = all.Count(s => s.CurrentStatusID == 1),
                Spent = all.Where(s => s.PaymentStatus == "Paid").Sum(s => s.Price)
            };
            return View(model);
        }

        // ---------------------------------------------------------------- quoting and booking

        [HttpGet]
        public IActionResult Quote()
        {
            return View(new QuoteViewModel
            {
                Cities = Select(_lookups.Cities(), c => c.CityID,
                                c => c.CityName + " (" + c.GovernorateName + ")", "-- choose a city --"),
                ServiceLevels = Select(_lookups.ServiceLevels(), s => s.Id, s => s.Name, null)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Quote(QuoteViewModel model)
        {
            model.Cities = Select(_lookups.Cities(), c => c.CityID,
                                  c => c.CityName + " (" + c.GovernorateName + ")", "-- choose a city --");
            model.ServiceLevels = Select(_lookups.ServiceLevels(), s => s.Id, s => s.Name, null);

            var result = _shipments.Quote(model.FromCityID, model.ToCityID, model.WeightKg,
                                          model.ServiceLevelID, model.DeclaredValue, model.IsInsured);
            if (result.Success)
                model.Result = result.Value;
            else
                model.Error = result.Error;

            return View(model);
        }

        [HttpGet]
        public IActionResult Book()
        {
            return View(BuildBookModel(new BookShipmentViewModel()));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Book(BookShipmentViewModel model)
        {
            var result = _customers.Book(Me.UserID, model.SenderAddressID, model.ReceiverName,
                                         model.ReceiverPhone, model.ReceiverAddressID,
                                         model.ReceiverStreet, model.ReceiverBuildingNo,
                                         model.ReceiverApartmentNo, model.ReceiverFloorNo,
                                         model.ReceiverLandmark, model.ReceiverCityID,
                                         model.PackageTypeID, model.ServiceLevelID, model.WeightKg,
                                         model.DeclaredValue, model.IsInsured, model.PaymentMethod);
            if (!result.Success)
            {
                model.Error = result.Error;
                return View(BuildBookModel(model));
            }

            Succeed("Shipment " + result.Value.TrackingNumber + " was booked. It costs "
                    + result.Value.Price.ToString("0.00") + " EGP.");
            return RedirectToAction("Details", "Customer", new { id = 0, code = result.Value.TrackingNumber });
        }

        // ---------------------------------------------------------------- my shipments

        [HttpGet]
        public IActionResult MyShipments(int? statusId, DateTime? from, DateTime? to,
                                         int? cityId, string search)
        {
            var model = new MyShipmentsViewModel
            {
                Shipments = _customers.MyShipments(Me.UserID, statusId, from, to, cityId, search),
                StatusID = statusId,
                FromDate = from,
                ToDate = to,
                CityID = cityId,
                Search = search,
                Statuses = Select(_lookups.Statuses(), s => s.Id, s => s.Name, "-- every status --"),
                Cities = Select(_lookups.Cities(), c => c.CityID, c => c.CityName, "-- every city --")
            };
            return View(model);
        }

        /// <summary>The details of one shipment: the timeline, the payment, the actions.</summary>
        [HttpGet]
        public IActionResult Details(int id, string code = null)
        {
            var timeline = new List<TimelineRow>();
            var payments = new List<PaymentRow>();
            var assignments = new List<AssignmentRow>();

            int shipmentId = id;
            if (shipmentId == 0 && !string.IsNullOrWhiteSpace(code))
            {
                var found = _shipments.Find(code.Trim().ToUpperInvariant(), null);
                if (found.Count == 0 || found[0].CustomerID != Me.UserID)
                {
                    Fail("This shipment does not belong to you.");
                    return RedirectToAction("MyShipments", "Customer");
                }
                shipmentId = found[0].ShipmentID;
            }

            // the customer id is passed so the database refuses to show somebody else's parcel
            var shipment = _customers.Details(shipmentId, Me.UserID, timeline, payments, assignments);
            if (shipment == null)
            {
                Fail("This shipment does not exist, or it is not yours.");
                return RedirectToAction("MyShipments", "Customer");
            }

            return View(new ShipmentDetailsViewModel
            {
                Shipment = shipment,
                Timeline = timeline,
                Payments = payments,
                Assignments = assignments
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Cancel(int shipmentId, string reason)
        {
            var result = _customers.Cancel(shipmentId, Me.UserID, reason);
            if (result.Success)
                Succeed("The shipment has been cancelled and the payment will be refunded.");
            else
                Fail(result.Error);
            return RedirectToAction("Details", "Customer", new { id = shipmentId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Pay(int shipmentId, string paymentMethod, string referenceNumber)
        {
            var result = _customers.Pay(shipmentId, Me.UserID, paymentMethod, referenceNumber);
            if (result.Success)
                Succeed("The payment has been recorded.");
            else
                Fail(result.Error);
            return RedirectToAction("Details", "Customer", new { id = shipmentId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Rate(int shipmentId, int stars, string comment)
        {
            var result = _customers.Rate(shipmentId, Me.UserID, stars, comment);
            if (result.Success)
                Succeed("Thank you, your rating has been saved.");
            else
                Fail(result.Error);
            return RedirectToAction("Details", "Customer", new { id = shipmentId });
        }

        /// <summary>The invoice, printed from the browser.</summary>
        [HttpGet]
        public IActionResult Invoice(int id)
        {
            var invoice = _customers.Invoice(id);
            if (invoice == null)
            {
                Fail("This shipment does not exist.");
                return RedirectToAction("MyShipments", "Customer");
            }
            return View(invoice);
        }

        /// <summary>CSV export of the customer's own shipments.</summary>
        [HttpGet]
        public IActionResult ExportCsv()
        {
            var data = _reports.MyShipmentsCsv(Me.UserID);
            return File(Csv.ToBytes(data.Tables.Count > 0 ? data.Tables[0] : null),
                        "text/csv", data.CsvFileName + ".csv");
        }

        // ---------------------------------------------------------------- address book

        [HttpGet]
        public IActionResult Addresses(int? editId)
        {
            var model = new AddressBookViewModel
            {
                Addresses = _customers.Addresses(Me.UserID),
                Cities = Select(_lookups.Cities(), c => c.CityID,
                                c => c.CityName + " (" + c.GovernorateName + ")", "-- choose a city --"),
                Labels = Select(_lookups.AddressLabels(), l => l.Id, l => l.Name, null)
            };

            if (editId.HasValue)
                model.Editing = model.Addresses.FirstOrDefault(a => a.AddressID == editId.Value)
                                ?? new AddressRow();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Addresses(AddressBookViewModel model)
        {
            int? editing = model.Editing.AddressID > 0 ? model.Editing.AddressID : (int?)null;

            var result = _customers.SaveAddress(Me.UserID, editing, model.Editing.Label,
                                                model.Editing.Street, model.Editing.BuildingNo,
                                                model.Editing.ApartmentNo, model.Editing.FloorNo,
                                                model.Editing.Landmark, model.Editing.CityID,
                                                model.Editing.Notes, model.Editing.IsDefault);
            if (!result.Success)
            {
                Fail(result.Error);
                return RedirectToAction("Addresses", "Customer");
            }

            Succeed(editing.HasValue ? "The address has been saved." : "The address has been added.");
            return RedirectToAction("Addresses", "Customer");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteAddress(int addressId)
        {
            var result = _customers.DeleteAddress(addressId, Me.UserID);
            if (result.Success)
                Succeed("The address has been deleted.");
            else
                Fail(result.Error);
            return RedirectToAction("Addresses", "Customer");
        }

        // ---------------------------------------------------------------- complaints

        [HttpGet]
        public IActionResult Complaints()
        {
            return View(new ComplaintsViewModel
            {
                Complaints = _customers.MyComplaints(Me.UserID),
                MyShipments = _customers.MyShipments(Me.UserID, null, null, null, null, null)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Complaints(ComplaintsViewModel model)
        {
            var result = _customers.Complain(model.ShipmentID, Me.UserID, model.Subject, model.Description);
            if (result.Success)
                Succeed("Your complaint has been sent to the branch.");
            else
                Fail(result.Error);
            return RedirectToAction("Complaints", "Customer");
        }

        // ---------------------------------------------------------------- helpers

        private BookShipmentViewModel BuildBookModel(BookShipmentViewModel model)
        {
            model.MyAddresses = _customers.Addresses(Me.UserID);
            model.Cities = Select(_lookups.Cities(), c => c.CityID,
                                  c => c.CityName + " (" + c.GovernorateName + ")", "-- choose a city --");
            model.PackageTypes = Select(_lookups.PackageTypes(), p => p.Id, p => p.ToString(), null);
            model.ServiceLevels = Select(_lookups.ServiceLevels(), s => s.Id, s => s.ToString(), null);
            model.PaymentMethods = Select(_lookups.PaymentMethods(), p => p.Id, p => p.Name, null);

            // a live price as soon as the customer has chosen enough to calculate one
            if (model.SenderAddressID > 0 && model.ReceiverCityID > 0 && model.WeightKg > 0)
            {
                var sender = model.MyAddresses.FirstOrDefault(a => a.AddressID == model.SenderAddressID);
                if (sender != null)
                {
                    var quote = _shipments.Quote(sender.CityID, model.ReceiverCityID, model.WeightKg,
                                                 model.ServiceLevelID, model.DeclaredValue, model.IsInsured);
                    if (quote.Success)
                        model.QuotedPrice = quote.Value.Price;
                }
            }
            return model;
        }
    }
}
