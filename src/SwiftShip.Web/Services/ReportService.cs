using SwiftShip.Web.Data;
using SwiftShip.Web.Reporting;

namespace SwiftShip.Web.Services
{
    /// <summary>The reports. Each method is one stored procedure and one page section.</summary>
    public class ReportService
    {
        private readonly ReportRepository _reports = new ReportRepository();

        // statistical
        public ReportData ShipmentsPerStatus() => _reports.ShipmentsPerStatus();
        public ReportData ShipmentsPerMonth(DateTime? from, DateTime? to) => _reports.ShipmentsPerMonth(from, to);
        public ReportData RevenuePerMonth(DateTime? from, DateTime? to) => _reports.RevenuePerMonth(from, to);
        public ReportData ShipmentsByServiceLevel() => _reports.ShipmentsByServiceLevel();
        public ReportData TopDestinationCities(int topN) => _reports.TopDestinationCities(topN);
        public ReportData BusiestRoutes(int topN) => _reports.BusiestRoutes(topN);

        // detailed
        public ReportData DelayedShipments() => _reports.DelayedShipments();
        public ReportData CourierPerformance(int courierId) => _reports.CourierPerformance(courierId);
        public ReportData BranchActivity(int branchId, DateTime? from, DateTime? to) => _reports.BranchActivity(branchId, from, to);
        public ReportData CustomerHistory(int customerId) => _reports.CustomerHistory(customerId);

        // managerial
        public ManagerialKpis Kpis() => _reports.Kpis();
        public ReportData RevenueByBranch(DateTime? from, DateTime? to) => _reports.RevenueByBranch(from, to);
        public ReportData CourierLeaderboard() => _reports.CourierLeaderboard();
        public ReportData ComplaintStats() => _reports.ComplaintStats();
        public ReportData OnTimeDelivery(DateTime? from, DateTime? to) => _reports.OnTimeDelivery(from, to);

        public ReportData MyShipmentsCsv(int customerId) => _reports.MyShipmentsCsv(customerId);
    }
}
