using SwiftShip.Web.Models;

namespace SwiftShip.Web.Data
{
    /// <summary>Reading and writing UserAccount, Customer and Employee rows.</summary>
    public class AccountRepository
    {
        public UserAccount GetByUsername(string username)
        {
            return Db.QuerySingle("dbo.usp_GetLoginByUsername",
                p => p.Add("@Username", username), Mappers.User);
        }

        public int CreateCustomer(string username, byte[] hash, byte[] salt, string fullName,
                                  string email, string phone, string customerType, string companyName)
        {
            object id = Db.Scalar("dbo.usp_SignUpCustomer", p =>
            {
                p.Add("@Username", username);
                p.Add("@PasswordHash", hash);
                p.Add("@PasswordSalt", salt);
                p.Add("@FullName", fullName);
                p.Add("@Email", email);
                p.Add("@Phone", phone);
                p.Add("@CustomerType", customerType);
                p.Add("@CompanyName", companyName);
            });
            return id == null ? 0 : Convert.ToInt32(id);
        }

        public int CreateStaff(string username, byte[] hash, byte[] salt, string role,
                               string fullName, string phone, string email, DateTime hireDate,
                               decimal salary, int branchId, string accessLevel,
                               string licenseNumber, string plateNumber, string vehicleType,
                               decimal? capacityKg, string position)
        {
            object id = Db.Scalar("dbo.usp_AdminCreateStaff", p =>
            {
                p.Add("@Username", username);
                p.Add("@PasswordHash", hash);
                p.Add("@PasswordSalt", salt);
                p.Add("@Role", role);
                p.Add("@FullName", fullName);
                p.Add("@Phone", phone);
                p.Add("@Email", email);
                p.Add("@HireDate", hireDate);
                p.Add("@Salary", salary);
                p.Add("@BranchID", branchId);
                p.Add("@AccessLevel", accessLevel);
                p.Add("@LicenseNumber", licenseNumber);
                p.Add("@PlateNumber", plateNumber);
                p.Add("@VehicleType", vehicleType);
                p.AddMoney("@CapacityKg", capacityKg);
                p.Add("@Position", position);
            });
            return id == null ? 0 : Convert.ToInt32(id);
        }

        public void ChangePassword(int userId, byte[] hash, byte[] salt)
        {
            Db.Execute("dbo.usp_ChangePassword", p =>
            {
                p.Add("@UserID", userId);
                p.Add("@PasswordHash", hash);
                p.Add("@PasswordSalt", salt);
            });
        }

        public void UpdateLastLogin(int userId)
        {
            Db.Execute("dbo.usp_UpdateLastLogin", p => p.Add("@UserID", userId));
        }

        public void SetUserActive(int userId, bool isActive)
        {
            Db.Execute("dbo.usp_SetUserActive", p =>
            {
                p.Add("@UserID", userId);
                p.Add("@IsActive", isActive);
            });
        }

        public bool UsernameExists(string username)
        {
            return Db.Scalar("dbo.usp_UsernameExists", p => p.Add("@Username", username))
                     is bool taken && taken;
        }

        public bool EmailExists(string email)
        {
            return Db.Scalar("dbo.usp_EmailExists", p => p.Add("@Email", email))
                     is bool taken && taken;
        }

        public void UpdateCustomerProfile(int customerId, string fullName, string email, string phone)
        {
            Db.Execute("dbo.usp_UpdateCustomerProfile", p =>
            {
                p.Add("@CustomerID", customerId);
                p.Add("@FullName", fullName);
                p.Add("@Email", email);
                p.Add("@Phone", phone);
            });
        }

        public CustomerProfile GetProfile(int customerId)
        {
            var row = new CustomerProfile { CustomerID = customerId };
            Db.ReadMultiple("dbo.usp_CustomerHistory", p => p.Add("@CustomerID", customerId),
                r =>
                {
                    row.FullName = r.Str("FullName");
                    row.Email = r.Str("Email");
                    row.CustomerType = r.Str("CustomerType");
                    row.CompanyName = r.Str("CompanyName");
                    row.CreatedAt = r.Date("CreatedAt") ?? DateTime.MinValue;
                },
                r => { },                                     // the shipments, not needed here
                r => { },                                     // the monthly totals
                r => { if (row.Phone == null) row.Phone = r.Str("PhoneNumber"); });
            return row;
        }
    }
}
