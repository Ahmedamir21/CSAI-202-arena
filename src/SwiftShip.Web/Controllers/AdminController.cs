using Microsoft.AspNetCore.Mvc;
using SwiftShip.Web.Models;
using SwiftShip.Web.Services;
using SwiftShip.Web.ViewModels;

namespace SwiftShip.Web.Controllers
{
    /// <summary>Company management. Everything here needs the Admin role.</summary>
    [RequireRole(Models.Roles.Admin)]
    public class AdminController : AppController
    {
        private readonly AdminService _admin;
        private readonly LookupService _lookups;
        private readonly ShipmentService _shipments;
        private readonly ReportService _reports;

        public AdminController(AdminService admin, LookupService lookups,
                               ShipmentService shipments, ReportService reports)
        {
            _admin = admin;
            _lookups = lookups;
            _shipments = shipments;
            _reports = reports;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View(new AdminHomeViewModel
            {
                Kpis = _reports.Kpis(),
                Delayed = _reports.DelayedShipments()
            });
        }

        // ---------------------------------------------------------------- branches and cities

        [HttpGet]
        public IActionResult Branches(int? editId)
        {
            var model = new BranchAdminViewModel
            {
                Branches = _admin.Branches(),
                Cities = Select(_lookups.Cities(), c => c.CityID,
                                c => c.CityName + " (" + c.GovernorateName + ")", "-- choose a city --"),
                Managers = Select(_admin.Staff(null, Roles.BranchEmployee, null),
                                  s => s.UserID, s => s.FullName, "-- nobody yet --")
            };
            if (editId.HasValue)
                model.Editing = model.Branches.FirstOrDefault(b => b.BranchID == editId.Value)
                                ?? new BranchItem();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Branches(BranchAdminViewModel model)
        {
            int? id = model.Editing.BranchID > 0 ? model.Editing.BranchID : (int?)null;
            var result = _admin.SaveBranch(id, model.Editing.BranchName, model.Street,
                                           model.BuildingNo, model.CityID, model.Phone,
                                           model.ManagerEmployeeID, model.Editing.IsActive);
            if (result.Success)
                Succeed("The branch has been saved.");
            else
                Fail(result.Error);
            return RedirectToAction("Branches", "Admin");
        }

        [HttpGet]
        public IActionResult Cities(int? editId)
        {
            var model = new CitiesAdminViewModel
            {
                Cities = _lookups.Cities(),
                Zones = Select(_lookups.Zones(), z => z.Id, z => z.Name, "-- choose a zone --")
            };
            if (editId.HasValue)
            {
                model.Editing = model.Cities.FirstOrDefault(c => c.CityID == editId.Value) ?? new CityItem();
                model.ZoneID = model.Editing.ZoneID;
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Cities(CitiesAdminViewModel model)
        {
            int? id = model.Editing.CityID > 0 ? model.Editing.CityID : (int?)null;
            var result = _admin.SaveCity(id, model.Editing.CityName, model.Editing.GovernorateName, model.ZoneID);
            if (result.Success)
                Succeed("The city has been saved.");
            else
                Fail(result.Error);
            return RedirectToAction("Cities", "Admin");
        }

        // ---------------------------------------------------------------- pricing

        [HttpGet]
        public IActionResult Pricing(bool onlyActive = true, int? editId = null)
        {
            var model = new PricingViewModel
            {
                Rules = _admin.PricingRules(onlyActive),
                History = _admin.PricingHistory(null, null),
                OnlyActive = onlyActive,
                Zones = Select(_lookups.Zones(), z => z.Id, z => z.Name, "-- choose a zone --")
            };
            if (editId.HasValue)
                model.Editing = model.Rules.FirstOrDefault(r => r.RuleID == editId.Value)
                                ?? new PricingRuleRow();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Pricing(PricingViewModel model)
        {
            int? id = model.Editing.RuleID > 0 ? model.Editing.RuleID : (int?)null;
            var result = _admin.SavePricingRule(id, model.Editing.FromZoneID, model.Editing.ToZoneID,
                                                model.Editing.BasePrice, model.Editing.PricePerKg,
                                                model.Editing.InsuranceRate, model.EffectiveFrom);
            if (result.Success)
                Succeed("The pricing rule has been saved. The old rule stays in the history.");
            else
                Fail(result.Error);
            return RedirectToAction("Pricing", "Admin");
        }

        // ---------------------------------------------------------------- staff

        [HttpGet]
        public IActionResult Staff(string search, string role, int? branchId)
        {
            return View(new StaffListViewModel
            {
                Staff = _admin.Staff(search, role, branchId),
                Search = search,
                Role = role,
                BranchID = branchId,
                Roles = SelectStrings(new[] { Roles.Admin, Roles.BranchEmployee, Roles.Courier },
                                      "-- everybody --"),
                Branches = Select(_lookups.Branches(), b => b.BranchID, b => b.BranchName,
                                  "-- every branch --")
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SetUserActive(int userId, bool isActive)
        {
            _admin.SetUserActive(userId, isActive);
            Succeed(isActive ? "The account has been activated." : "The account has been deactivated.");
            return RedirectToAction("Staff", "Admin");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult MoveEmployee(int userId, int branchId)
        {
            var result = _admin.MoveEmployee(userId, branchId);
            if (result.Success)
                Succeed("The employee has been moved.");
            else
                Fail(result.Error);
            return RedirectToAction("Staff", "Admin");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AssignVehicle(int courierId, string plateNumber, string vehicleType, decimal? capacityKg)
        {
            var result = _admin.AssignVehicle(courierId, plateNumber, vehicleType, capacityKg);
            if (result.Success)
                Succeed("The vehicle has been assigned.");
            else
                Fail(result.Error);
            return RedirectToAction("Staff", "Admin");
        }

        [HttpGet]
        public IActionResult CreateStaff()
        {
            return View(BuildStaffModel(new CreateStaffViewModel()));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateStaff(CreateStaffViewModel model)
        {
            var result = _admin.CreateStaff(model.Username, model.Password, model.Role, model.FullName,
                                            model.Phone, model.Email, model.HireDate, model.Salary,
                                            model.BranchID, model.AccessLevel, model.LicenseNumber,
                                            model.PlateNumber, model.VehicleType, model.CapacityKg,
                                            model.Position);
            if (!result.Success)
            {
                model.Error = result.Error;
                return View(BuildStaffModel(model));
            }

            Succeed(model.FullName + " can now sign in. The password has to be changed at the first login.");
            return RedirectToAction("Staff", "Admin");
        }

        // ---------------------------------------------------------------- lookups

        [HttpGet]
        public IActionResult Lookups()
        {
            return View(new LookupsAdminViewModel
            {
                PackageTypes = _lookups.PackageTypes(),
                ServiceLevels = _lookups.ServiceLevels()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SavePackageType(int? packageTypeId, string typeName, decimal maxWeightKg, string sizeNote)
        {
            var result = _admin.SavePackageType(packageTypeId, typeName, maxWeightKg, sizeNote);
            if (result.Success)
                Succeed("The package type has been saved.");
            else
                Fail(result.Error);
            return RedirectToAction("Lookups", "Admin");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SaveServiceLevel(int? serviceLevelId, string levelName,
                                              decimal priceMultiplier, int maxDays, string description)
        {
            var result = _admin.SaveServiceLevel(serviceLevelId, levelName, priceMultiplier, maxDays, description);
            if (result.Success)
                Succeed("The service level has been saved.");
            else
                Fail(result.Error);
            return RedirectToAction("Lookups", "Admin");
        }

        // ---------------------------------------------------------------- complaints

        [HttpGet]
        public IActionResult Complaints(string status)
        {
            return View(new AdminComplaintsViewModel
            {
                Complaints = _admin.Complaints(status),
                Status = status,
                Statuses = SelectStrings(new[] { "Open", "InProgress", "Resolved" },
                                         "-- every complaint --")
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Complaints(AdminComplaintsViewModel model)
        {
            var result = _admin.CloseComplaint(model.ComplaintID, Me.UserID, model.Resolution);
            if (result.Success)
                Succeed("The complaint has been closed.");
            else
                Fail(result.Error);
            return RedirectToAction("Complaints", "Admin");
        }

        // ---------------------------------------------------------------- customers

        [HttpGet]
        public IActionResult Customers(string search, int? selected)
        {
            var model = new AdminCustomersViewModel
            {
                Customers = _admin.Customers(search),
                Search = search
            };
            if (selected.HasValue)
                model.Selected = _admin.CustomerHistory(selected.Value);
            return View(model);
        }

        // ---------------------------------------------------------------- correcting a status

        [HttpGet]
        public IActionResult CorrectStatus(string search)
        {
            var model = new CorrectStatusViewModel
            {
                Search = search,
                Found = string.IsNullOrWhiteSpace(search)
                    ? new List<ShipmentCard>()
                    : _shipments.Find(null, search),
                Statuses = Select(_lookups.Statuses(), s => s.Id, s => s.Name, "-- choose a status --")
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CorrectStatus(CorrectStatusViewModel model)
        {
            var result = _admin.CorrectStatus(model.ShipmentID, model.NewStatusID, Me.UserID, model.Reason);
            if (result.Success)
                Succeed("The status has been corrected and the reason was written in the history.");
            else
                Fail(result.Error);
            return RedirectToAction("CorrectStatus", "Admin", new { search = model.Search });
        }

        // ---------------------------------------------------------------- helpers

        private CreateStaffViewModel BuildStaffModel(CreateStaffViewModel model)
        {
            model.Roles = SelectStrings(new[] { Roles.Courier, Roles.BranchEmployee, Roles.Admin }, null);
            model.Branches = Select(_lookups.Branches(), b => b.BranchID,
                                    b => b.BranchName + " - " + b.CityName, "-- choose a branch --");
            model.VehicleTypes = SelectStrings(new[] { "Bike", "Van", "Truck" }, "-- no vehicle yet --");
            model.Positions = SelectStrings(new[] { "Clerk", "Supervisor", "Manager" }, null);
            return model;
        }
    }
}
