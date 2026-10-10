using Microsoft.AspNetCore.Mvc.Rendering;
using SwiftShip.Web.Models;
using SwiftShip.Web.Reporting;

namespace SwiftShip.Web.ViewModels
{
    /// <summary>
    /// All the view models of the project. A view model carries exactly what one screen
    /// needs: the data, the lists behind the dropdowns, and the message to show if something
    /// went wrong.
    /// </summary>

    public class LoginViewModel
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string ReturnUrl { get; set; }
        public string Error { get; set; }
    }

    public class SignUpViewModel
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string CustomerType { get; set; } = "Individual";
        public string CompanyName { get; set; }
        public string Error { get; set; }
    }

    public class ChangePasswordViewModel
    {
        public string Username { get; set; }
        public string CurrentPassword { get; set; }
        public string NewPassword { get; set; }
        public string ConfirmPassword { get; set; }
        public bool FirstLogin { get; set; }
        public string Error { get; set; }
        public string Message { get; set; }
    }

    public class ProfileViewModel
    {
        public int CustomerID { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string CustomerType { get; set; }
        public string CompanyName { get; set; }
        public string Error { get; set; }
        public string Message { get; set; }
    }

    /// <summary>Used by every screen that has a message to show at the top.</summary>
    public class Toast
    {
        public string Kind { get; set; }        // success, error, warning, info
        public string Text { get; set; }
    }

    public class QuoteViewModel
    {
        public int FromCityID { get; set; }
        public int ToCityID { get; set; }
        public decimal WeightKg { get; set; } = 1;
        public int ServiceLevelID { get; set; } = 1;
        public decimal DeclaredValue { get; set; }
        public bool IsInsured { get; set; }

        public ShipmentRepository.QuoteResult Result { get; set; }
        public string Error { get; set; }

        public List<SelectListItem> Cities { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> ServiceLevels { get; set; } = new List<SelectListItem>();
    }

    public class BookShipmentViewModel
    {
        public int SenderAddressID { get; set; }
        public int? ReceiverAddressID { get; set; }
        public string ReceiverName { get; set; }
        public string ReceiverPhone { get; set; }
        public string ReceiverStreet { get; set; }
        public string ReceiverBuildingNo { get; set; }
        public string ReceiverApartmentNo { get; set; }
        public string ReceiverFloorNo { get; set; }
        public string ReceiverLandmark { get; set; }
        public int ReceiverCityID { get; set; }
        public int PackageTypeID { get; set; } = 1;
        public int ServiceLevelID { get; set; } = 1;
        public decimal WeightKg { get; set; } = 1;
        public decimal DeclaredValue { get; set; }
        public bool IsInsured { get; set; }
        public string PaymentMethod { get; set; } = "CashOnDelivery";

        public List<AddressRow> MyAddresses { get; set; } = new List<AddressRow>();
        public List<SelectListItem> Cities { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> PackageTypes { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> ServiceLevels { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> PaymentMethods { get; set; } = new List<SelectListItem>();

        public string TrackingNumber { get; set; }
        public decimal QuotedPrice { get; set; }
        public string Error { get; set; }
    }

    public class MyShipmentsViewModel
    {
        public List<ShipmentCard> Shipments { get; set; } = new List<ShipmentCard>();
        public int? StatusID { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? CityID { get; set; }
        public string Search { get; set; }
        public List<SelectListItem> Statuses { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Cities { get; set; } = new List<SelectListItem>();
    }

    public class ShipmentDetailsViewModel
    {
        public ShipmentCard Shipment { get; set; }
        public List<TimelineRow> Timeline { get; set; } = new List<TimelineRow>();
        public List<PaymentRow> Payments { get; set; } = new List<PaymentRow>();
        public List<AssignmentRow> Assignments { get; set; } = new List<AssignmentRow>();

        public int? Stars { get; set; }
        public string Comment { get; set; }
        public string ComplaintSubject { get; set; }
        public string ComplaintDescription { get; set; }

        public bool CanCancel { get { return Shipment != null && Shipment.CurrentStatusID == 1; } }
        public bool CanRate { get { return Shipment != null && Shipment.CurrentStatusID == 6 && Shipment.RatingStars == null; } }
        public bool CanPay
        {
            get
            {
                return Shipment != null && Shipment.PaymentStatus == "Pending"
                       && Shipment.CurrentStatusID != 9;
            }
        }

        public string Error { get; set; }
        public string Message { get; set; }
    }

    public class AddressBookViewModel
    {
        public List<AddressRow> Addresses { get; set; } = new List<AddressRow>();
        public AddressRow Editing { get; set; } = new AddressRow();
        public List<SelectListItem> Cities { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Labels { get; set; } = new List<SelectListItem>();
        public string Error { get; set; }
        public string Message { get; set; }
    }

    public class ComplaintsViewModel
    {
        public List<ComplaintRow> Complaints { get; set; } = new List<ComplaintRow>();
        public List<ShipmentCard> MyShipments { get; set; } = new List<ShipmentCard>();
        public int ShipmentID { get; set; }
        public string Subject { get; set; }
        public string Description { get; set; }
        public string Error { get; set; }
        public string Message { get; set; }
    }

    public class TrackViewModel
    {
        public string TrackingNumber { get; set; }
        public ShipmentCard Shipment { get; set; }
        public List<TimelineRow> Timeline { get; set; } = new List<TimelineRow>();
        public string Error { get; set; }
    }

    public class CustomerHomeViewModel
    {
        public List<ShipmentCard> Recent { get; set; } = new List<ShipmentCard>();
        public int OnTheWay { get; set; }
        public int Delivered { get; set; }
        public int Open { get; set; }
        public decimal Spent { get; set; }
    }

    public class CourierTodayViewModel
    {
        public DateTime Date { get; set; }
        public List<AssignmentRow> Tasks { get; set; } = new List<AssignmentRow>();
        public string Error { get; set; }
        public string Message { get; set; }
    }

    public class CourierHistoryViewModel
    {
        public List<ShipmentCard> Shipments { get; set; } = new List<ShipmentCard>();
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? StatusID { get; set; }
        public List<SelectListItem> Statuses { get; set; } = new List<SelectListItem>();
    }

    public class CourierPerformanceViewModel
    {
        public CourierPerformanceRow Summary { get; set; }
        public List<CourierRepository.DailyPoint> PerDay { get; set; } = new List<CourierRepository.DailyPoint>();
    }

    public class BranchDashboardViewModel
    {
        public BranchRepository.Dashboard Dashboard { get; set; }
        public string TrackingNumber { get; set; }
        public string Error { get; set; }
        public string Message { get; set; }
    }

    public class AssignCourierViewModel
    {
        public List<ShipmentCard> Shipments { get; set; } = new List<ShipmentCard>();
        public List<CourierItem> Couriers { get; set; } = new List<CourierItem>();
        public int ShipmentID { get; set; }
        public int CourierID { get; set; }
        public string AssignmentType { get; set; } = "Delivery";
        public string Search { get; set; }
        public string Error { get; set; }
        public string Message { get; set; }
    }

    public class WalkInViewModel
    {
        public int CustomerID { get; set; }
        public int SenderAddressID { get; set; }
        public string ReceiverName { get; set; }
        public string ReceiverPhone { get; set; }
        public string ReceiverStreet { get; set; }
        public string ReceiverBuildingNo { get; set; }
        public string ReceiverApartmentNo { get; set; }
        public string ReceiverFloorNo { get; set; }
        public string ReceiverLandmark { get; set; }
        public int ReceiverCityID { get; set; }
        public int PackageTypeID { get; set; } = 1;
        public int ServiceLevelID { get; set; } = 1;
        public decimal WeightKg { get; set; } = 1;
        public decimal DeclaredValue { get; set; }
        public bool IsInsured { get; set; }
        public string PaymentMethod { get; set; } = "CashOnDelivery";
        public string CustomerSearch { get; set; }

        public List<CustomerAdminRow> Customers { get; set; } = new List<CustomerAdminRow>();
        public List<AddressRow> Addresses { get; set; } = new List<AddressRow>();
        public List<SelectListItem> Cities { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> PackageTypes { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> ServiceLevels { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> PaymentMethods { get; set; } = new List<SelectListItem>();
        public string Error { get; set; }
        public string Message { get; set; }
    }

    public class TransfersViewModel
    {
        public List<TransferRow> Incoming { get; set; } = new List<TransferRow>();
        public List<ShipmentCard> Shipments { get; set; } = new List<ShipmentCard>();
        public int ShipmentID { get; set; }
        public int ToBranchID { get; set; }
        public List<SelectListItem> Branches { get; set; } = new List<SelectListItem>();
        public string Error { get; set; }
        public string Message { get; set; }
    }

    public class BranchComplaintsViewModel
    {
        public List<ComplaintRow> Complaints { get; set; } = new List<ComplaintRow>();
        public int ComplaintID { get; set; }
        public string Resolution { get; set; }
        public bool CloseIt { get; set; }
        public string Error { get; set; }
        public string Message { get; set; }
    }

    public class BranchFailedViewModel
    {
        public List<ShipmentCard> Shipments { get; set; } = new List<ShipmentCard>();
        public int ShipmentID { get; set; }
        public string Notes { get; set; }
        public string Action { get; set; }        // Reschedule or Return
        public string Error { get; set; }
        public string Message { get; set; }
    }

    public class StaffListViewModel
    {
        public List<StaffRow> Staff { get; set; } = new List<StaffRow>();
        public string Search { get; set; }
        public string Role { get; set; }
        public int? BranchID { get; set; }
        public List<SelectListItem> Roles { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Branches { get; set; } = new List<SelectListItem>();
    }

    public class CreateStaffViewModel
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string Role { get; set; } = "Courier";
        public string FullName { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public DateTime HireDate { get; set; } = DateTime.Today;
        public decimal Salary { get; set; }
        public int BranchID { get; set; }
        public string AccessLevel { get; set; }
        public string LicenseNumber { get; set; }
        public string PlateNumber { get; set; }
        public string VehicleType { get; set; }
        public decimal? CapacityKg { get; set; }
        public string Position { get; set; } = "Clerk";

        public List<SelectListItem> Roles { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Branches { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> VehicleTypes { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Positions { get; set; } = new List<SelectListItem>();
        public string Error { get; set; }
        public string Message { get; set; }
    }

    public class BranchAdminViewModel
    {
        public List<BranchItem> Branches { get; set; } = new List<BranchItem>();
        public BranchItem Editing { get; set; } = new BranchItem();
        public int CityID { get; set; }
        public string Street { get; set; }
        public string BuildingNo { get; set; }
        public string Phone { get; set; }
        public int? ManagerEmployeeID { get; set; }
        public List<SelectListItem> Cities { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Managers { get; set; } = new List<SelectListItem>();
        public string Error { get; set; }
        public string Message { get; set; }
    }

    public class CitiesAdminViewModel
    {
        public List<CityItem> Cities { get; set; } = new List<CityItem>();
        public CityItem Editing { get; set; } = new CityItem();
        public int ZoneID { get; set; }
        public List<SelectListItem> Zones { get; set; } = new List<SelectListItem>();
        public string Error { get; set; }
        public string Message { get; set; }
    }

    public class PricingViewModel
    {
        public List<PricingRuleRow> Rules { get; set; } = new List<PricingRuleRow>();
        public List<PricingRuleRow> History { get; set; } = new List<PricingRuleRow>();
        public PricingRuleRow Editing { get; set; } = new PricingRuleRow();
        public bool OnlyActive { get; set; } = true;
        public DateTime? EffectiveFrom { get; set; }
        public List<SelectListItem> Zones { get; set; } = new List<SelectListItem>();
        public string Error { get; set; }
        public string Message { get; set; }
    }

    public class LookupsAdminViewModel
    {
        public List<LookupItem> PackageTypes { get; set; } = new List<LookupItem>();
        public List<LookupItem> ServiceLevels { get; set; } = new List<LookupItem>();
        public string Error { get; set; }
        public string Message { get; set; }
    }

    public class AdminComplaintsViewModel
    {
        public List<ComplaintRow> Complaints { get; set; } = new List<ComplaintRow>();
        public string Status { get; set; }
        public int ComplaintID { get; set; }
        public string Resolution { get; set; }
        public List<SelectListItem> Statuses { get; set; } = new List<SelectListItem>();
        public string Error { get; set; }
        public string Message { get; set; }
    }

    public class AdminCustomersViewModel
    {
        public List<CustomerAdminRow> Customers { get; set; } = new List<CustomerAdminRow>();
        public string Search { get; set; }
        public CustomerHistory Selected { get; set; }
        public string Message { get; set; }
    }

    public class CorrectStatusViewModel
    {
        public string Search { get; set; }
        public List<ShipmentCard> Found { get; set; } = new List<ShipmentCard>();
        public int ShipmentID { get; set; }
        public int NewStatusID { get; set; }
        public string Reason { get; set; }
        public List<SelectListItem> Statuses { get; set; } = new List<SelectListItem>();
        public string Error { get; set; }
        public string Message { get; set; }
    }

    public class AdminHomeViewModel
    {
        public ManagerialKpis Kpis { get; set; }
        public ReportData Delayed { get; set; }
    }

    /// <summary>One report page: the chart, the tables and the filters.</summary>
    public class ReportPageViewModel
    {
        public string Section { get; set; }          // statistical, detailed, managerial
        public string Report { get; set; }           // which report to draw
        public ReportData Data { get; set; }

        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? CourierID { get; set; }
        public int? BranchID { get; set; }
        public int? CustomerID { get; set; }
        public int TopN { get; set; } = 10;

        public List<SelectListItem> Couriers { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Branches { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Customers { get; set; } = new List<SelectListItem>();
    }

    public class ManagerialReportViewModel
    {
        public ManagerialKpis Kpis { get; set; }
        public ReportData RevenueByBranch { get; set; }
        public ReportData Leaderboard { get; set; }
        public ReportData Complaints { get; set; }
        public ReportData Routes { get; set; }
        public ReportData OnTime { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
