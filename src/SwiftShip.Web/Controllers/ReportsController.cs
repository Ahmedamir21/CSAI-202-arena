using Microsoft.AspNetCore.Mvc;
using SwiftShip.Web.Models;
using SwiftShip.Web.Reporting;
using SwiftShip.Web.Services;
using SwiftShip.Web.ViewModels;

namespace SwiftShip.Web.Controllers
{
    /// <summary>
    /// The reports. Statistical reports, detailed reports about one part of the data, and
    /// the managerial overall view. Every one of them is a stored procedure, a table with
    /// filters, a chart and a CSV export.
    /// </summary>
    [RequireRole(Models.Roles.Admin)]
    public class ReportsController : AppController
    {
        private readonly ReportService _reports;
        private readonly LookupService _lookups;
        private readonly AdminService _admin;

        public ReportsController(ReportService reports, LookupService lookups, AdminService admin)
        {
            _reports = reports;
            _lookups = lookups;
            _admin = admin;
        }

        /// <summary>Managerial: the KPI cards and the company-wide charts.</summary>
        [HttpGet]
        public IActionResult Index(DateTime? from, DateTime? to)
        {
            return View(new ManagerialReportViewModel
            {
                Kpis = _reports.Kpis(),
                RevenueByBranch = _reports.RevenueByBranch(from, to),
                Leaderboard = _reports.CourierLeaderboard(),
                Complaints = _reports.ComplaintStats(),
                Routes = _reports.BusiestRoutes(8),
                OnTime = _reports.OnTimeDelivery(from, to),
                FromDate = from,
                ToDate = to
            });
        }

        /// <summary>The five statistical reports, chosen from a row of tabs.</summary>
        [HttpGet]
        public IActionResult Statistical(string report = "status", DateTime? from = null,
                                         DateTime? to = null, int topN = 10)
        {
            ReportData data;
            switch (report)
            {
                case "month":
                    data = _reports.ShipmentsPerMonth(from, to);
                    break;
                case "revenue":
                    data = _reports.RevenuePerMonth(from, to);
                    break;
                case "service":
                    data = _reports.ShipmentsByServiceLevel();
                    break;
                case "cities":
                    data = _reports.TopDestinationCities(topN);
                    break;
                default:
                    report = "status";
                    data = _reports.ShipmentsPerStatus();
                    break;
            }

            return View(new ReportPageViewModel
            {
                Section = "statistical",
                Report = report,
                Data = data,
                FromDate = from,
                ToDate = to,
                TopN = topN
            });
        }

        /// <summary>
        /// The detailed reports: one courier, one branch, one customer, or the delayed list.
        /// The dropdowns show names, never identifiers.
        /// </summary>
        [HttpGet]
        public IActionResult Detailed(string report = "delayed", int? courierId = null,
                                      int? branchId = null, int? customerId = null,
                                      DateTime? from = null, DateTime? to = null)
        {
            var model = new ReportPageViewModel
            {
                Section = "detailed",
                Report = report,
                CourierID = courierId ?? (_admin.Staff(null, Roles.Courier, null).FirstOrDefault()?.UserID),
                BranchID = branchId ?? Me.BranchID,
                CustomerID = customerId,
                FromDate = from,
                ToDate = to,
                Couriers = Select(_admin.Staff(null, Roles.Courier, null),
                                  s => s.UserID, s => s.FullName + " - " + s.BranchName, null),
                Branches = Select(_lookups.Branches(), b => b.BranchID,
                                  b => b.BranchName + " - " + b.CityName, null),
                Customers = Select(_admin.Customers(null), c => c.CustomerID,
                                   c => c.FullName, "-- choose a customer --")
            };

            switch (report)
            {
                case "courier":
                    model.Data = _reports.CourierPerformance(model.CourierID ?? 0);
                    break;
                case "branch":
                    model.Data = _reports.BranchActivity(model.BranchID ?? 0, from, to);
                    break;
                case "customer":
                    model.Data = _reports.CustomerHistory(model.CustomerID ?? 0);
                    break;
                default:
                    model.Report = "delayed";
                    model.Data = _reports.DelayedShipments();
                    break;
            }

            return View(model);
        }

        /// <summary>The CSV export of whichever report is on the screen.</summary>
        [HttpGet]
        public IActionResult Csv(string report, int? courierId, int? branchId, int? customerId,
                                 DateTime? from, DateTime? to, int topN = 10)
        {
            ReportData data;
            switch (report)
            {
                case "status": data = _reports.ShipmentsPerStatus(); break;
                case "month": data = _reports.ShipmentsPerMonth(from, to); break;
                case "revenue": data = _reports.RevenuePerMonth(from, to); break;
                case "service": data = _reports.ShipmentsByServiceLevel(); break;
                case "cities": data = _reports.TopDestinationCities(topN); break;
                case "routes": data = _reports.BusiestRoutes(topN); break;
                case "courier": data = _reports.CourierPerformance(courierId ?? 0); break;
                case "branch": data = _reports.BranchActivity(branchId ?? 0, from, to); break;
                case "customer": data = _reports.CustomerHistory(customerId ?? 0); break;
                case "leaderboard": data = _reports.CourierLeaderboard(); break;
                case "complaints": data = _reports.ComplaintStats(); break;
                case "ontime": data = _reports.OnTimeDelivery(from, to); break;
                case "revenue-by-branch": data = _reports.RevenueByBranch(from, to); break;
                default: data = _reports.DelayedShipments(); break;
            }

            var table = data.Tables.Count > 0 ? data.Tables[0] : null;
            return File(Csv.ToBytes(table), "text/csv", data.CsvFileName + ".csv");
        }
    }
}
