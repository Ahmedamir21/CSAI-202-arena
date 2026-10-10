namespace SwiftShip.Web.Models
{
    /// <summary>The four roles a user account can have.</summary>
    public static class Roles
    {
        public const string Customer = "Customer";
        public const string Courier = "Courier";
        public const string BranchEmployee = "BranchEmployee";
        public const string Admin = "Admin";
    }

    /// <summary>One row of the UserAccount table, plus the name we show on screen.</summary>
    public class UserAccount
    {
        public int UserID { get; set; }
        public string Username { get; set; }
        public byte[] PasswordHash { get; set; }
        public byte[] PasswordSalt { get; set; }
        public string Role { get; set; }
        public bool IsActive { get; set; }
        public bool MustChangePassword { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public string DisplayName { get; set; }
        public int? BranchID { get; set; }
        public string BranchName { get; set; }
    }

    public class CustomerProfile
    {
        public int CustomerID { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string CustomerType { get; set; }
        public string CompanyName { get; set; }
        public string Phone { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class PhoneRow
    {
        public string PhoneNumber { get; set; }
        public string PhoneType { get; set; }
    }

    public class AddressRow
    {
        public int AddressID { get; set; }
        public int? CustomerID { get; set; }
        public string Label { get; set; }
        public string Street { get; set; }
        public string BuildingNo { get; set; }
        public string ApartmentNo { get; set; }
        public string FloorNo { get; set; }
        public string Landmark { get; set; }
        public int CityID { get; set; }
        public string CityName { get; set; }
        public string GovernorateName { get; set; }
        public string Notes { get; set; }
        public bool IsDefault { get; set; }
        public string FullAddress { get; set; }
    }

    /// <summary>
    /// One row of vw_ShipmentCard. Everything a list or a details screen shows is already
    /// joined here, with names instead of identifiers.
    /// </summary>
    public class ShipmentCard
    {
        public int ShipmentID { get; set; }
        public string TrackingNumber { get; set; }
        public int CustomerID { get; set; }
        public string CustomerName { get; set; }
        public string ReceiverName { get; set; }
        public string ReceiverPhone { get; set; }
        public int OriginCityID { get; set; }
        public string OriginCity { get; set; }
        public int DestinationCityID { get; set; }
        public string DestinationCity { get; set; }
        public string PackageTypeName { get; set; }
        public string ServiceLevelName { get; set; }
        public decimal WeightKg { get; set; }
        public decimal DeclaredValue { get; set; }
        public bool IsInsured { get; set; }
        public decimal Price { get; set; }
        public string PaymentMethod { get; set; }
        public string PaymentStatus { get; set; }
        public string CurrentStatus { get; set; }
        public int CurrentStatusID { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpectedDeliveryDate { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public string CourierName { get; set; }
        public int? RatingStars { get; set; }
        public string RatingComment { get; set; }
        public string CancelReason { get; set; }
    }

    public class TimelineRow
    {
        public int SeqNo { get; set; }
        public string StatusName { get; set; }
        public DateTime ChangedAt { get; set; }
        public string Notes { get; set; }
        public string ChangedByEmployeeName { get; set; }
        public string BranchName { get; set; }
        public string BranchCity { get; set; }
    }

    public class PaymentRow
    {
        public int PaymentID { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; }
        public string PaymentStatus { get; set; }
        public DateTime? PaidAt { get; set; }
        public string ReferenceNumber { get; set; }
        public string CollectedByCourierName { get; set; }
    }

    public class AssignmentRow
    {
        public int AssignmentID { get; set; }
        public int ShipmentID { get; set; }
        public string TrackingNumber { get; set; }
        public string AssignmentType { get; set; }
        public DateTime AssignedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string Notes { get; set; }
        public string CourierName { get; set; }
        public int CourierID { get; set; }
        public string ContactName { get; set; }
        public string ContactPhone { get; set; }
        public string CityName { get; set; }
        public string StreetAddress { get; set; }
        public string Landmark { get; set; }
        public string CurrentStatus { get; set; }
        public int CurrentStatusID { get; set; }
        public DateTime ExpectedDeliveryDate { get; set; }
        public decimal Price { get; set; }
        public string PackageTypeName { get; set; }
        public decimal WeightKg { get; set; }
        public string PaymentMethod { get; set; }
    }

    public class ComplaintRow
    {
        public int ComplaintID { get; set; }
        public int ShipmentID { get; set; }
        public string TrackingNumber { get; set; }
        public string Subject { get; set; }
        public string Description { get; set; }
        public string ComplaintStatus { get; set; }
        public DateTime OpenedAt { get; set; }
        public string Resolution { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public string CustomerName { get; set; }
        public string HandledByName { get; set; }
        public string ShipmentStatus { get; set; }
    }

    /// <summary>Everything a dropdown needs: the value and the name the user reads.</summary>
    public class LookupItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Extra { get; set; }

        public override string ToString()
        {
            return string.IsNullOrEmpty(Extra) ? Name : Name + " (" + Extra + ")";
        }
    }

    public class CityItem
    {
        public int CityID { get; set; }
        public int ZoneID { get; set; }
        public string CityName { get; set; }
        public string GovernorateName { get; set; }
        public string ZoneName { get; set; }
    }

    public class BranchItem
    {
        public int BranchID { get; set; }
        public string BranchName { get; set; }
        public string CityName { get; set; }
        public bool IsActive { get; set; }
        public string ManagerName { get; set; }
        public int StaffCount { get; set; }
    }

    public class CourierItem
    {
        public int CourierID { get; set; }
        public string CourierName { get; set; }
        public string CourierStatus { get; set; }
        public string VehicleType { get; set; }
        public int OpenJobs { get; set; }
        public int DoneToday { get; set; }
    }

    public class PricingRuleRow
    {
        public int RuleID { get; set; }
        public int FromZoneID { get; set; }
        public int ToZoneID { get; set; }
        public string FromZone { get; set; }
        public string ToZone { get; set; }
        public decimal BasePrice { get; set; }
        public decimal PricePerKg { get; set; }
        public decimal InsuranceRate { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public bool IsActive { get; set; }
        public int ShipmentsPriced { get; set; }
        public decimal RevenuePriced { get; set; }
    }

    public class StaffRow
    {
        public int UserID { get; set; }
        public string Username { get; set; }
        public string Role { get; set; }
        public bool IsActive { get; set; }
        public string FullName { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public DateTime HireDate { get; set; }
        public decimal Salary { get; set; }
        public string AccessLevel { get; set; }
        public int BranchID { get; set; }
        public string BranchName { get; set; }
        public string LicenseNumber { get; set; }
        public string CourierStatus { get; set; }
        public string PlateNumber { get; set; }
        public string VehicleType { get; set; }
        public string Position { get; set; }
        public int OpenJobs { get; set; }
    }

    public class CustomerAdminRow
    {
        public int CustomerID { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string CustomerType { get; set; }
        public string CompanyName { get; set; }
        public string Username { get; set; }
        public bool IsActive { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public int ShipmentsCount { get; set; }
        public decimal TotalSpent { get; set; }
    }

    public class CourierPerformanceRow
    {
        public int CourierID { get; set; }
        public string CourierName { get; set; }
        public string BranchName { get; set; }
        public int DeliveriesDone { get; set; }
        public int PickupsDone { get; set; }
        public int FailedAttempts { get; set; }
        public int DeliveriesTaken { get; set; }
        public decimal AverageRating { get; set; }
        public int RatingsCount { get; set; }
        public decimal CashCollected { get; set; }
        public decimal SuccessRatePercent { get; set; }
        public decimal AverageDeliveryHours { get; set; }
    }

    public class TransferRow
    {
        public int TransferID { get; set; }
        public int ShipmentID { get; set; }
        public string TrackingNumber { get; set; }
        public string FromBranch { get; set; }
        public string ToBranch { get; set; }
        public DateTime SentAt { get; set; }
        public DateTime? ReceivedAt { get; set; }
        public string CurrentStatus { get; set; }
        public string DestinationCity { get; set; }
        public string ReceiverName { get; set; }
    }

    public class InvoiceRow
    {
        public string TrackingNumber { get; set; }
        public DateTime CreatedAt { get; set; }
        public decimal Price { get; set; }
        public decimal WeightKg { get; set; }
        public decimal DeclaredValue { get; set; }
        public bool IsInsured { get; set; }
        public string PaymentMethod { get; set; }
        public string PaymentStatus { get; set; }
        public DateTime? PaidAt { get; set; }
        public string ReferenceNumber { get; set; }
        public string CustomerName { get; set; }
        public string Email { get; set; }
        public string CompanyName { get; set; }
        public string PackageTypeName { get; set; }
        public string ServiceLevelName { get; set; }
        public string OriginCity { get; set; }
        public string DestinationCity { get; set; }
        public string OriginAddress { get; set; }
        public string DestinationAddress { get; set; }
        public string ReceiverName { get; set; }
        public string ReceiverPhone { get; set; }
    }

    public class WaybillRow
    {
        public string TrackingNumber { get; set; }
        public DateTime CreatedAt { get; set; }
        public decimal WeightKg { get; set; }
        public decimal Price { get; set; }
        public string PaymentMethod { get; set; }
        public DateTime ExpectedDeliveryDate { get; set; }
        public string ProofCode { get; set; }
        public string PackageTypeName { get; set; }
        public string ServiceLevelName { get; set; }
        public string SenderName { get; set; }
        public string CompanyName { get; set; }
        public string SenderPhone { get; set; }
        public string SenderAddress { get; set; }
        public string ReceiverName { get; set; }
        public string ReceiverPhone { get; set; }
        public string ReceiverAddress { get; set; }
        public string DestinationCity { get; set; }
        public string DestinationGovernorate { get; set; }
    }
}

namespace SwiftShip.Web.Reporting
{
    /// <summary>One result set of a report, with its columns and rows as text.</summary>
    public class ReportTable
    {
        public string Title { get; set; }
        public List<string> Columns { get; set; } = new List<string>();
        public List<List<string>> Rows { get; set; } = new List<List<string>>();
    }

    /// <summary>
    /// A report page: one or more tables plus, when it makes sense, the numbers for a chart.
    /// The pages render whatever the stored procedure returned, so adding a report is adding
    /// a stored procedure and one method in ReportRepository.
    /// </summary>
    public class ReportData
    {
        public string Title { get; set; }
        public string Description { get; set; }

        /// <summary>Every result set the procedure answered with. The first one is the main table.</summary>
        public List<ReportTable> Tables { get; set; } = new List<ReportTable>();
        public ReportTable Main
        {
            get { return Tables.Count > 0 ? Tables[0] : new ReportTable(); }
        }

        // the chart part
        public string ChartType { get; set; }              // bar, line, pie, doughnut
        public List<string> Labels { get; set; } = new List<string>();
        public List<ReportSeries> Series { get; set; } = new List<ReportSeries>();

        public string CsvFileName { get; set; } = "report";
    }

    public class ReportSeries
    {
        public string Name { get; set; }
        public List<decimal> Values { get; set; } = new List<decimal>();
    }

    /// <summary>The KPI cards on the managerial report.</summary>
    public class ManagerialKpis
    {
        public int TotalShipments { get; set; }
        public int DeliveredShipments { get; set; }
        public decimal DeliveredPercent { get; set; }
        public decimal Revenue { get; set; }
        public decimal AverageDeliveryHours { get; set; }
        public int OpenComplaints { get; set; }
        public int ActiveCouriers { get; set; }
        public int Customers { get; set; }
        public int DelayedShipments { get; set; }
        public decimal AverageRating { get; set; }
        public decimal OnTimePercent { get; set; }
    }
}
