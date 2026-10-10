using SwiftShip.Web.Data;
using SwiftShip.Web.Models;

namespace SwiftShip.Web.Services
{
    /// <summary>Branches, cities, pricing, staff, complaints and customers.</summary>
    public class AdminService
    {
        private readonly AdminRepository _admin = new AdminRepository();

        public List<BranchItem> Branches() => _admin.Branches();

        public Result SaveBranch(int? branchId, string name, string street, string buildingNo,
                                 int cityId, string phone, int? managerId, bool isActive)
        {
            if (string.IsNullOrWhiteSpace(name))
                return Result.Fail("Please write the name of the branch.");
            if (string.IsNullOrWhiteSpace(street))
                return Result.Fail("Please write the street of the branch.");
            if (cityId <= 0)
                return Result.Fail("Please choose the city of the branch.");
            try
            {
                _admin.SaveBranch(branchId, name.Trim(), street.Trim(), buildingNo, cityId,
                                  phone, managerId, isActive);
                return Result.Ok();
            }
            catch (Exception ex)
            {
                return Result.Fail(CustomerService.Friendly(ex));
            }
        }

        public Result SaveCity(int? cityId, string cityName, string governorate, int zoneId)
        {
            if (string.IsNullOrWhiteSpace(cityName))
                return Result.Fail("Please write the name of the city.");
            if (string.IsNullOrWhiteSpace(governorate))
                return Result.Fail("Please write the governorate.");
            if (zoneId <= 0)
                return Result.Fail("Please choose the pricing zone.");
            _admin.SaveCity(cityId, cityName.Trim(), governorate.Trim(), zoneId);
            return Result.Ok();
        }

        public Result SavePackageType(int? id, string name, decimal maxWeight, string sizeNote)
        {
            if (string.IsNullOrWhiteSpace(name))
                return Result.Fail("Please write the name of the package type.");
            if (maxWeight <= 0)
                return Result.Fail("The maximum weight must be more than zero.");
            _admin.SavePackageType(id, name.Trim(), maxWeight, sizeNote);
            return Result.Ok();
        }

        public Result SaveServiceLevel(int? id, string name, decimal multiplier, int maxDays, string description)
        {
            if (string.IsNullOrWhiteSpace(name))
                return Result.Fail("Please write the name of the service level.");
            if (multiplier <= 0)
                return Result.Fail("The price multiplier must be more than zero.");
            if (maxDays <= 0)
                return Result.Fail("The promised number of days must be more than zero.");
            _admin.SaveServiceLevel(id, name.Trim(), multiplier, maxDays, description);
            return Result.Ok();
        }

        public List<StaffRow> Staff(string search, string role, int? branchId)
            => _admin.Staff(search, role, branchId);

        /// <summary>Creates a courier, a branch employee or another administrator.</summary>
        public Result<int> CreateStaff(string username, string password, string role, string fullName,
                                       string phone, string email, DateTime hireDate, decimal salary,
                                       int branchId, string accessLevel, string licenseNumber,
                                       string plateNumber, string vehicleType, decimal? capacityKg,
                                       string position)
        {
            if (string.IsNullOrWhiteSpace(username) || username.Trim().Length < 4)
                return Result<int>.Fail("The username must be at least 4 characters long.");
            if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
                return Result<int>.Fail("The password must be at least 8 characters long.");
            if (role != Roles.Courier && role != Roles.BranchEmployee && role != Roles.Admin)
                return Result<int>.Fail("Please choose what kind of account this is.");
            if (string.IsNullOrWhiteSpace(fullName))
                return Result<int>.Fail("Please write the full name.");
            if (string.IsNullOrWhiteSpace(phone))
                return Result<int>.Fail("Please write a phone number.");
            if (branchId <= 0)
                return Result<int>.Fail("Please choose the branch this person belongs to.");
            if (role == Roles.Courier && string.IsNullOrWhiteSpace(licenseNumber))
                return Result<int>.Fail("A courier needs a licence number.");

            var accounts = new AccountRepository();
            if (accounts.UsernameExists(username.Trim()))
                return Result<int>.Fail("This username is already taken.");

            byte[] salt = PasswordHasher.CreateSalt();
            byte[] hash = PasswordHasher.Hash(password, salt);

            try
            {
                int id = accounts.CreateStaff(username.Trim(), hash, salt, role, fullName.Trim(),
                                              phone.Trim(), email, hireDate, salary, branchId,
                                              role == Roles.Admin ? accessLevel : null,
                                              licenseNumber, plateNumber, vehicleType, capacityKg,
                                              position);
                return Result<int>.Ok(id);
            }
            catch (Exception ex)
            {
                return Result<int>.Fail(CustomerService.Friendly(ex));
            }
        }

        /// <summary>Deactivating an account keeps its history; it only stops it signing in.</summary>
        public Result SetUserActive(int userId, bool isActive)
        {
            _admin.SetUserActive(userId, isActive);
            return Result.Ok();
        }

        public Result MoveEmployee(int employeeId, int branchId)
        {
            if (branchId <= 0)
                return Result.Fail("Please choose the branch.");
            _admin.MoveEmployee(employeeId, branchId);
            return Result.Ok();
        }

        public Result AssignVehicle(int courierId, string plate, string vehicleType, decimal? capacity)
        {
            if (string.IsNullOrWhiteSpace(plate))
                return Result.Fail("Please write the plate number.");
            if (string.IsNullOrWhiteSpace(vehicleType))
                return Result.Fail("Please choose the type of vehicle.");
            _admin.AssignVehicle(courierId, plate.Trim(), vehicleType, capacity);
            return Result.Ok();
        }

        public List<PricingRuleRow> PricingRules(bool onlyActive) => _admin.PricingRules(onlyActive);

        public Result SavePricingRule(int? ruleId, int fromZoneId, int toZoneId, decimal basePrice,
                                      decimal pricePerKg, decimal insuranceRate, DateTime? effectiveFrom)
        {
            if (fromZoneId <= 0 || toZoneId <= 0)
                return Result.Fail("Please choose the zone we collect from and the zone we deliver to.");
            if (basePrice < 0 || pricePerKg < 0)
                return Result.Fail("The prices cannot be negative.");
            if (insuranceRate < 0 || insuranceRate > 1)
                return Result.Fail("The insurance rate is a fraction, for example 0.01 for one percent.");
            try
            {
                _admin.UpsertPricingRule(ruleId, fromZoneId, toZoneId, basePrice, pricePerKg,
                                         insuranceRate, effectiveFrom);
                return Result.Ok();
            }
            catch (Exception ex)
            {
                return Result.Fail(CustomerService.Friendly(ex));
            }
        }

        public List<PricingRuleRow> PricingHistory(int? fromZoneId, int? toZoneId)
            => _admin.PricingHistory(fromZoneId, toZoneId);

        public List<ComplaintRow> Complaints(string status) => _admin.Complaints(status);

        public Result CloseComplaint(int complaintId, int employeeId, string resolution)
        {
            if (string.IsNullOrWhiteSpace(resolution) || resolution.Trim().Length < 10)
                return Result.Fail("Please write the resolution before closing the complaint.");
            try
            {
                _admin.CloseComplaint(complaintId, employeeId, resolution.Trim());
                return Result.Ok();
            }
            catch (Exception ex)
            {
                return Result.Fail(CustomerService.Friendly(ex));
            }
        }

        /// <summary>Corrects a shipment status by hand. The reason is mandatory and is logged.</summary>
        public Result CorrectStatus(int shipmentId, int newStatusId, int employeeId, string reason)
        {
            if (newStatusId <= 0)
                return Result.Fail("Please choose the new status.");
            if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 5)
                return Result.Fail("A reason is required when you correct a status by hand.");
            try
            {
                _admin.CorrectStatus(shipmentId, newStatusId, employeeId, reason.Trim());
                return Result.Ok();
            }
            catch (Exception ex)
            {
                return Result.Fail(CustomerService.Friendly(ex));
            }
        }

        public List<CustomerAdminRow> Customers(string search) => _admin.Customers(search);

        public CustomerHistory CustomerHistory(int customerId) => _admin.CustomerHistory(customerId);
    }
}
