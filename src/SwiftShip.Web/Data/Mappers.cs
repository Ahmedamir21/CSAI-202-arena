using Microsoft.Data.SqlClient;
using SwiftShip.Web.Models;

namespace SwiftShip.Web.Data
{
    /// <summary>
    /// Turns a row of a SqlDataReader into one of the model classes.
    /// Kept in one file so that a column added to a view is changed in one place only.
    /// </summary>
    public static class Mappers
    {
        public static UserAccount User(SqlDataReader r)
        {
            return new UserAccount
            {
                UserID = r.Int("UserID"),
                Username = r.Str("Username"),
                PasswordHash = r.Bytes("PasswordHash"),
                PasswordSalt = r.Bytes("PasswordSalt"),
                Role = r.Str("Role"),
                IsActive = r.Bool("IsActive"),
                MustChangePassword = r.Bool("MustChangePassword"),
                LastLoginAt = r.Date("LastLoginAt"),
                DisplayName = r.Str("DisplayName"),
                BranchID = r.IntOrNull("BranchID"),
                BranchName = r.Str("BranchName")
            };
        }

        public static ShipmentCard ShipmentCard(SqlDataReader r)
        {
            return new ShipmentCard
            {
                ShipmentID = r.Int("ShipmentID"),
                TrackingNumber = r.Str("TrackingNumber"),
                CustomerID = r.Int("CustomerID"),
                CustomerName = r.Str("CustomerName"),
                ReceiverName = r.Str("ReceiverName"),
                ReceiverPhone = r.Str("ReceiverPhone"),
                OriginCityID = r.Int("OriginCityID"),
                OriginCity = r.Str("OriginCity"),
                DestinationCityID = r.Int("DestinationCityID"),
                DestinationCity = r.Str("DestinationCity"),
                PackageTypeName = r.Str("PackageTypeName"),
                ServiceLevelName = r.Str("ServiceLevelName"),
                WeightKg = r.Dec("WeightKg"),
                DeclaredValue = r.Dec("DeclaredValue"),
                IsInsured = r.Bool("IsInsured"),
                Price = r.Dec("Price"),
                PaymentMethod = r.Str("PaymentMethod"),
                PaymentStatus = r.Str("PaymentStatus"),
                CurrentStatus = r.Str("CurrentStatus"),
                CurrentStatusID = r.Int("CurrentStatusID"),
                CreatedAt = r.Date("CreatedAt") ?? DateTime.MinValue,
                ExpectedDeliveryDate = r.Date("ExpectedDeliveryDate") ?? DateTime.MinValue,
                DeliveredAt = r.Date("DeliveredAt"),
                CourierName = r.Str("CourierName"),
                RatingStars = (int?)r.IntOrNull("RatingStars"),
                RatingComment = r.Str("RatingComment"),
                CancelReason = r.Str("CancelReason")
            };
        }

        public static TimelineRow Timeline(SqlDataReader r)
        {
            return new TimelineRow
            {
                SeqNo = r.Int("SeqNo"),
                StatusName = r.Str("StatusName"),
                ChangedAt = r.Date("ChangedAt") ?? DateTime.MinValue,
                Notes = r.Str("Notes"),
                ChangedByEmployeeName = r.Str("ChangedByEmployeeName"),
                BranchName = r.Str("BranchName"),
                BranchCity = r.Str("BranchCity")
            };
        }

        public static PaymentRow Payment(SqlDataReader r)
        {
            return new PaymentRow
            {
                PaymentID = r.Int("PaymentID"),
                Amount = r.Dec("Amount"),
                PaymentMethod = r.Str("PaymentMethod"),
                PaymentStatus = r.Str("PaymentStatus"),
                PaidAt = r.Date("PaidAt"),
                ReferenceNumber = r.Str("ReferenceNumber"),
                CollectedByCourierName = r.Str("CollectedByCourierName")
            };
        }

        public static AssignmentRow Assignment(SqlDataReader r)
        {
            return new AssignmentRow
            {
                AssignmentID = r.Int("AssignmentID"),
                ShipmentID = r.Int("ShipmentID"),
                TrackingNumber = r.Str("TrackingNumber"),
                AssignmentType = r.Str("AssignmentType"),
                AssignedAt = r.Date("AssignedAt") ?? DateTime.MinValue,
                CompletedAt = r.Date("CompletedAt"),
                Notes = r.Str("Notes"),
                CourierName = r.Str("CourierName"),
                CourierID = r.Int("CourierID"),
                ContactName = r.Str("ContactName"),
                ContactPhone = r.Str("ContactPhone"),
                CityName = r.Str("CityName"),
                StreetAddress = r.Str("StreetAddress"),
                Landmark = r.Str("Landmark"),
                CurrentStatus = r.Str("CurrentStatus"),
                CurrentStatusID = r.Int("CurrentStatusID"),
                ExpectedDeliveryDate = r.Date("ExpectedDeliveryDate") ?? DateTime.MinValue,
                Price = r.Dec("Price"),
                PackageTypeName = r.Str("PackageTypeName"),
                WeightKg = r.Dec("WeightKg"),
                PaymentMethod = r.Str("PaymentMethod")
            };
        }

        public static ComplaintRow Complaint(SqlDataReader r)
        {
            return new ComplaintRow
            {
                ComplaintID = r.Int("ComplaintID"),
                ShipmentID = r.Int("ShipmentID"),
                TrackingNumber = r.Str("TrackingNumber"),
                Subject = r.Str("Subject"),
                Description = r.Str("Description"),
                ComplaintStatus = r.Str("ComplaintStatus"),
                OpenedAt = r.Date("OpenedAt") ?? DateTime.MinValue,
                Resolution = r.Str("Resolution"),
                ResolvedAt = r.Date("ResolvedAt"),
                CustomerName = r.Str("CustomerName"),
                HandledByName = r.Str("HandledByName"),
                ShipmentStatus = r.Str("ShipmentStatus")
            };
        }

        public static AddressRow Address(SqlDataReader r)
        {
            return new AddressRow
            {
                AddressID = r.Int("AddressID"),
                CustomerID = r.IntOrNull("CustomerID"),
                Label = r.Str("Label"),
                Street = r.Str("Street"),
                BuildingNo = r.Str("BuildingNo"),
                ApartmentNo = r.Str("ApartmentNo"),
                FloorNo = r.Str("FloorNo"),
                Landmark = r.Str("Landmark"),
                CityID = r.Int("CityID"),
                CityName = r.Str("CityName"),
                GovernorateName = r.Str("GovernorateName"),
                Notes = r.Str("Notes"),
                IsDefault = r.Bool("IsDefault"),
                FullAddress = r.Str("FullAddress")
            };
        }

        public static CityItem City(SqlDataReader r)
        {
            return new CityItem
            {
                CityID = r.Int("CityID"),
                ZoneID = r.Int("ZoneID"),
                CityName = r.Str("CityName"),
                GovernorateName = r.Str("GovernorateName"),
                ZoneName = r.Str("ZoneName")
            };
        }

        public static BranchItem Branch(SqlDataReader r)
        {
            return new BranchItem
            {
                BranchID = r.Int("BranchID"),
                BranchName = r.Str("BranchName"),
                CityName = r.Str("CityName"),
                IsActive = r.Bool("IsActive"),
                ManagerName = r.Str("ManagerName"),
                StaffCount = r.Int("StaffCount")
            };
        }

        public static CourierItem Courier(SqlDataReader r)
        {
            return new CourierItem
            {
                CourierID = r.Int("CourierID"),
                CourierName = r.Str("CourierName"),
                CourierStatus = r.Str("CourierStatus"),
                VehicleType = r.Str("VehicleType"),
                OpenJobs = r.Int("OpenJobs"),
                DoneToday = r.Int("DoneToday")
            };
        }

        public static PricingRuleRow PricingRule(SqlDataReader r)
        {
            return new PricingRuleRow
            {
                RuleID = r.Int("RuleID"),
                FromZoneID = r.Int("FromZoneID"),
                ToZoneID = r.Int("ToZoneID"),
                FromZone = r.Str("FromZone"),
                ToZone = r.Str("ToZone"),
                BasePrice = r.Dec("BasePrice"),
                PricePerKg = r.Dec("PricePerKg"),
                InsuranceRate = r.Dec("InsuranceRate"),
                EffectiveFrom = r.Date("EffectiveFrom") ?? DateTime.MinValue,
                EffectiveTo = r.Date("EffectiveTo"),
                IsActive = r.Bool("IsActive"),
                ShipmentsPriced = r.Int("ShipmentsPriced"),
                RevenuePriced = r.Dec("RevenuePriced")
            };
        }

        public static StaffRow Staff(SqlDataReader r)
        {
            return new StaffRow
            {
                UserID = r.Int("UserID"),
                Username = r.Str("Username"),
                Role = r.Str("Role"),
                IsActive = r.Bool("IsActive"),
                FullName = r.Str("FullName"),
                Phone = r.Str("Phone"),
                Email = r.Str("Email"),
                HireDate = r.Date("HireDate") ?? DateTime.MinValue,
                Salary = r.Dec("Salary"),
                AccessLevel = r.Str("AccessLevel"),
                BranchID = r.Int("BranchID"),
                BranchName = r.Str("BranchName"),
                LicenseNumber = r.Str("LicenseNumber"),
                CourierStatus = r.Str("CourierStatus"),
                PlateNumber = r.Str("PlateNumber"),
                VehicleType = r.Str("VehicleType"),
                Position = r.Str("Position"),
                OpenJobs = r.Int("OpenJobs")
            };
        }

        public static CustomerAdminRow CustomerAdmin(SqlDataReader r)
        {
            return new CustomerAdminRow
            {
                CustomerID = r.Int("CustomerID"),
                FullName = r.Str("FullName"),
                Email = r.Str("Email"),
                CustomerType = r.Str("CustomerType"),
                CompanyName = r.Str("CompanyName"),
                Username = r.Str("Username"),
                IsActive = r.Bool("IsActive"),
                LastLoginAt = r.Date("LastLoginAt"),
                ShipmentsCount = r.Int("ShipmentsCount"),
                TotalSpent = r.Dec("TotalSpent")
            };
        }

        public static CourierPerformanceRow CourierPerformance(SqlDataReader r)
        {
            return new CourierPerformanceRow
            {
                CourierID = r.Int("CourierID"),
                CourierName = r.Str("FullName"),
                BranchName = r.Str("BranchName"),
                DeliveriesDone = r.Int("DeliveriesDone"),
                PickupsDone = r.Int("PickupsDone"),
                FailedAttempts = r.Int("FailedAttempts"),
                DeliveriesTaken = r.Int("DeliveriesTaken"),
                AverageRating = r.Dec("AverageRating"),
                RatingsCount = r.Int("RatingsCount"),
                CashCollected = r.Dec("CashCollected"),
                SuccessRatePercent = r.Dec("SuccessRatePercent"),
                AverageDeliveryHours = r.Dec("AverageDeliveryHours")
            };
        }

        public static TransferRow Transfer(SqlDataReader r)
        {
            return new TransferRow
            {
                TransferID = r.Int("TransferID"),
                ShipmentID = r.Int("ShipmentID"),
                TrackingNumber = r.Str("TrackingNumber"),
                FromBranch = r.Str("FromBranch"),
                ToBranch = r.Str("ToBranch"),
                SentAt = r.Date("SentAt") ?? DateTime.MinValue,
                ReceivedAt = r.Date("ReceivedAt"),
                CurrentStatus = r.Str("CurrentStatus"),
                DestinationCity = r.Str("DestinationCity"),
                ReceiverName = r.Str("ReceiverName")
            };
        }

        public static InvoiceRow Invoice(SqlDataReader r)
        {
            return new InvoiceRow
            {
                TrackingNumber = r.Str("TrackingNumber"),
                CreatedAt = r.Date("CreatedAt") ?? DateTime.MinValue,
                Price = r.Dec("Price"),
                WeightKg = r.Dec("WeightKg"),
                DeclaredValue = r.Dec("DeclaredValue"),
                IsInsured = r.Bool("IsInsured"),
                PaymentMethod = r.Str("PaymentMethod"),
                PaymentStatus = r.Str("PaymentStatus"),
                PaidAt = r.Date("PaidAt"),
                ReferenceNumber = r.Str("ReferenceNumber"),
                CustomerName = r.Str("CustomerName"),
                Email = r.Str("Email"),
                CompanyName = r.Str("CompanyName"),
                PackageTypeName = r.Str("PackageTypeName"),
                ServiceLevelName = r.Str("ServiceLevelName"),
                OriginCity = r.Str("OriginCity"),
                DestinationCity = r.Str("DestinationCity"),
                OriginAddress = r.Str("OriginAddress"),
                DestinationAddress = r.Str("DestinationAddress"),
                ReceiverName = r.Str("ReceiverName"),
                ReceiverPhone = r.Str("ReceiverPhone")
            };
        }

        public static WaybillRow Waybill(SqlDataReader r)
        {
            return new WaybillRow
            {
                TrackingNumber = r.Str("TrackingNumber"),
                CreatedAt = r.Date("CreatedAt") ?? DateTime.MinValue,
                WeightKg = r.Dec("WeightKg"),
                Price = r.Dec("Price"),
                PaymentMethod = r.Str("PaymentMethod"),
                ExpectedDeliveryDate = r.Date("ExpectedDeliveryDate") ?? DateTime.MinValue,
                ProofCode = r.Str("ProofCode"),
                PackageTypeName = r.Str("PackageTypeName"),
                ServiceLevelName = r.Str("ServiceLevelName"),
                SenderName = r.Str("SenderName"),
                CompanyName = r.Str("CompanyName"),
                SenderPhone = r.Str("SenderPhone"),
                SenderAddress = r.Str("SenderAddress"),
                ReceiverName = r.Str("ReceiverName"),
                ReceiverPhone = r.Str("ReceiverPhone"),
                ReceiverAddress = r.Str("ReceiverAddress"),
                DestinationCity = r.Str("DestinationCity"),
                DestinationGovernorate = r.Str("DestinationGovernorate")
            };
        }
    }
}
