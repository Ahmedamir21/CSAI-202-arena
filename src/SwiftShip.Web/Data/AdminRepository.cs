using SwiftShip.Web.Models;

namespace SwiftShip.Web.Data
{
    /// <summary>The administration screens: branches, cities, pricing and staff.</summary>
    public class AdminRepository
    {
        public List<BranchItem> Branches()
        {
            return Db.Query("dbo.usp_AdminBranches", null, Mappers.Branch);
        }

        public void SaveBranch(int? branchId, string name, string street, string buildingNo,
                               int cityId, string phone, int? managerId, bool isActive)
        {
            Db.Execute("dbo.usp_SaveBranch", p =>
            {
                p.AddInt("@BranchID", branchId);
                p.Add("@BranchName", name);
                p.Add("@Street", street);
                p.Add("@BuildingNo", buildingNo);
                p.Add("@CityID", cityId);
                p.Add("@Phone", phone);
                p.AddInt("@ManagerEmployeeID", managerId);
                p.Add("@IsActive", isActive);
            });
        }

        public void SaveCity(int? cityId, string cityName, string governorate, int zoneId)
        {
            Db.Execute("dbo.usp_SaveCity", p =>
            {
                p.AddInt("@CityID", cityId);
                p.Add("@CityName", cityName);
                p.Add("@GovernorateName", governorate);
                p.Add("@ZoneID", zoneId);
            });
        }

        public void SavePackageType(int? id, string name, decimal maxWeight, string sizeNote)
        {
            Db.Execute("dbo.usp_SavePackageType", p =>
            {
                p.AddInt("@PackageTypeID", id);
                p.Add("@TypeName", name);
                p.Add("@MaxWeightKg", maxWeight);
                p.Add("@SizeNote", sizeNote);
            });
        }

        public void SaveServiceLevel(int? id, string name, decimal multiplier, int maxDays, string description)
        {
            Db.Execute("dbo.usp_SaveServiceLevel", p =>
            {
                p.AddInt("@ServiceLevelID", id);
                p.Add("@LevelName", name);
                p.Add("@PriceMultiplier", multiplier);
                p.Add("@MaxDays", maxDays);
                p.Add("@Description", description);
            });
        }

        public List<StaffRow> Staff(string search, string role, int? branchId)
        {
            return Db.Query("dbo.usp_AdminStaffList", p =>
            {
                p.Add("@Search", search);
                p.Add("@Role", role);
                p.AddInt("@BranchID", branchId);
            }, Mappers.Staff);
        }

        public void MoveEmployee(int employeeId, int branchId)
        {
            Db.Execute("dbo.usp_MoveEmployeeToBranch", p =>
            {
                p.Add("@EmployeeID", employeeId);
                p.Add("@BranchID", branchId);
            });
        }

        /// <summary>Deactivating somebody keeps his history; it only stops him signing in.</summary>
        public void SetUserActive(int userId, bool isActive)
        {
            Db.Execute("dbo.usp_SetUserActive", p =>
            {
                p.Add("@UserID", userId);
                p.Add("@IsActive", isActive);
            });
        }

        /// <summary>Closing a complaint: the resolution, who closed it and when.</summary>
        public void CloseComplaint(int complaintId, int employeeId, string resolution)
        {
            Db.Execute("dbo.usp_AnswerComplaint", p =>
            {
                p.Add("@ComplaintID", complaintId);
                p.Add("@EmployeeID", employeeId);
                p.Add("@Resolution", resolution);
                p.Add("@CloseIt", true);
            });
        }

        /// <summary>Answering without closing leaves the complaint InProgress.</summary>
        public void AnswerComplaint(int complaintId, int employeeId, string resolution)
        {
            Db.Execute("dbo.usp_AnswerComplaint", p =>
            {
                p.Add("@ComplaintID", complaintId);
                p.Add("@EmployeeID", employeeId);
                p.Add("@Resolution", resolution);
                p.Add("@CloseIt", false);
            });
        }

        public void AssignVehicle(int courierId, string plate, string vehicleType, decimal? capacity)
        {
            Db.Execute("dbo.usp_AssignVehicle", p =>
            {
                p.Add("@CourierID", courierId);
                p.Add("@PlateNumber", plate);
                p.Add("@VehicleType", vehicleType);
                p.AddMoney("@CapacityKg", capacity);
            });
        }

        public List<PricingRuleRow> PricingRules(bool onlyActive)
        {
            return Db.Query("dbo.usp_PricingRules",
                p => p.Add("@OnlyActive", onlyActive), Mappers.PricingRule);
        }

        public void UpsertPricingRule(int? ruleId, int fromZoneId, int toZoneId, decimal basePrice,
                                      decimal pricePerKg, decimal insuranceRate, DateTime? effectiveFrom)
        {
            Db.Execute("dbo.usp_UpsertPricingRule", p =>
            {
                p.AddInt("@RuleID", ruleId);
                p.Add("@FromZoneID", fromZoneId);
                p.Add("@ToZoneID", toZoneId);
                p.Add("@BasePrice", basePrice);
                p.Add("@PricePerKg", pricePerKg);
                p.Add("@InsuranceRate", insuranceRate);
                p.Add("@EffectiveFrom", effectiveFrom);
            });
        }

        /// <summary>The price history: every generation of every rule, with what it earned.</summary>
        public List<PricingRuleRow> PricingHistory(int? fromZoneId, int? toZoneId)
        {
            return Db.Query("dbo.usp_PricingHistory", p =>
            {
                p.AddInt("@FromZoneID", fromZoneId);
                p.AddInt("@ToZoneID", toZoneId);
            }, Mappers.PricingRule);
        }

        public List<ComplaintRow> Complaints(string status)
        {
            return Db.Query("dbo.usp_AdminComplaints",
                p => p.Add("@Status", status), Mappers.Complaint);
        }

        public void CorrectStatus(int shipmentId, int newStatusId, int employeeId, string reason)
        {
            Db.Execute("dbo.usp_AdminOverrideStatus", p =>
            {
                p.Add("@ShipmentID", shipmentId);
                p.Add("@NewStatusID", newStatusId);
                p.Add("@EmployeeID", employeeId);
                p.Add("@Reason", reason);
            });
        }

        public List<CustomerAdminRow> Customers(string search)
        {
            return Db.Query("dbo.usp_AdminCustomers",
                p => p.Add("@Search", search), Mappers.CustomerAdmin);
        }

        /// <summary>One customer: who he is, his shipments, his spending per month, his phones.</summary>
        public CustomerHistory CustomerHistory(int customerId)
        {
            var result = new CustomerHistory();
            Db.ReadMultiple("dbo.usp_CustomerHistory", p => p.Add("@CustomerID", customerId),
                r => result.Customer = Mappers.CustomerAdmin(r),
                r => result.Shipments.Add(Mappers.ShipmentCard(r)),
                r => result.Months.Add(new MonthlyPoint
                {
                    Label = r.Str("MonthKey"),
                    Shipments = r.Int("Shipments"),
                    Billed = r.Dec("Billed"),
                    Rating = r.Dec("AverageRating")
                }),
                r => result.Phones.Add(new PhoneRow
                {
                    PhoneNumber = r.Str("PhoneNumber"),
                    PhoneType = r.Str("PhoneType")
                }));
            return result;
        }
    }

    public class CustomerHistory
    {
        public CustomerAdminRow Customer { get; set; }
        public List<ShipmentCard> Shipments { get; set; } = new List<ShipmentCard>();
        public List<MonthlyPoint> Months { get; set; } = new List<MonthlyPoint>();
        public List<PhoneRow> Phones { get; set; } = new List<PhoneRow>();
    }

    public class MonthlyPoint
    {
        public string Label { get; set; }
        public int Shipments { get; set; }
        public decimal Billed { get; set; }
        public decimal Rating { get; set; }
    }
}
