using SwiftShip.Web.Models;

namespace SwiftShip.Web.Services
{
    /// <summary>The names we use in the session, so they are written in one place.</summary>
    public static class SessionKeys
    {
        public const string UserID = "UserID";
        public const string Username = "Username";
        public const string Role = "Role";
        public const string DisplayName = "DisplayName";
        public const string BranchID = "BranchID";
        public const string BranchName = "BranchName";
        public const string MustChangePassword = "MustChangePassword";
    }

    /// <summary>Who is logged in. Built from the session, checked by the role filter.</summary>
    public class CurrentUser
    {
        public int UserID { get; set; }
        public string Username { get; set; }
        public string Role { get; set; }
        public string DisplayName { get; set; }
        public int? BranchID { get; set; }
        public string BranchName { get; set; }
        public bool MustChangePassword { get; set; }

        public bool IsAdmin { get { return Role == Roles.Admin; } }
        public bool IsCourier { get { return Role == Roles.Courier; } }
        public bool IsBranchEmployee { get { return Role == Roles.BranchEmployee; } }
        public bool IsCustomer { get { return Role == Roles.Customer; } }

        public static CurrentUser From(ISession session)
        {
            int? id = session.GetInt32(SessionKeys.UserID);
            if (!id.HasValue)
                return null;

            return new CurrentUser
            {
                UserID = id.Value,
                Username = session.GetString(SessionKeys.Username),
                Role = session.GetString(SessionKeys.Role),
                DisplayName = session.GetString(SessionKeys.DisplayName),
                BranchID = session.GetInt32(SessionKeys.BranchID),
                BranchName = session.GetString(SessionKeys.BranchName),
                MustChangePassword = session.GetString(SessionKeys.MustChangePassword) == "1"
            };
        }

        public void Save(ISession session)
        {
            session.SetInt32(SessionKeys.UserID, UserID);
            session.SetString(SessionKeys.Username, Username ?? "");
            session.SetString(SessionKeys.Role, Role ?? "");
            session.SetString(SessionKeys.DisplayName, DisplayName ?? Username ?? "");
            if (BranchID.HasValue)
                session.SetInt32(SessionKeys.BranchID, BranchID.Value);
            else
                session.Remove(SessionKeys.BranchID);
            session.SetString(SessionKeys.BranchName, BranchName ?? "");
            session.SetString(SessionKeys.MustChangePassword, MustChangePassword ? "1" : "0");
        }

        public static void Clear(ISession session)
        {
            session.Clear();
        }
    }
}
