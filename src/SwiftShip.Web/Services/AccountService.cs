using SwiftShip.Web.Data;
using SwiftShip.Web.Models;

namespace SwiftShip.Web.Services
{
    /// <summary>Sign up, log in, change password, edit the profile.</summary>
    public class AccountService
    {
        private readonly AccountRepository _accounts = new AccountRepository();

        public Result<UserAccount> Login(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return Result<UserAccount>.Fail("Please enter your username and your password.");

            UserAccount account = _accounts.GetByUsername(username.Trim());

            // The same message is shown whether the user does not exist or the password is
            // wrong, so the form cannot be used to find out which usernames are taken.
            if (account == null || !PasswordHasher.Verify(password, account.PasswordSalt, account.PasswordHash))
                return Result<UserAccount>.Fail("The username or the password is not correct.");

            if (!account.IsActive)
                return Result<UserAccount>.Fail("This account is not active. Please contact the administrator.");

            _accounts.UpdateLastLogin(account.UserID);
            return Result<UserAccount>.Ok(account);
        }

        public Result<int> SignUp(string username, string password, string confirmPassword,
                                  string fullName, string email, string phone,
                                  string customerType, string companyName)
        {
            // server-side validation; the client side only makes it faster to see the mistake
            if (string.IsNullOrWhiteSpace(username) || username.Trim().Length < 4)
                return Result<int>.Fail("The username must be at least 4 characters long.");
            if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
                return Result<int>.Fail("The password must be at least 8 characters long.");
            if (password != confirmPassword)
                return Result<int>.Fail("The two passwords are not the same.");
            if (string.IsNullOrWhiteSpace(fullName))
                return Result<int>.Fail("Please write your full name.");
            if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
                return Result<int>.Fail("Please write a valid email address.");
            if (string.IsNullOrWhiteSpace(phone))
                return Result<int>.Fail("Please write a phone number, the courier will need it.");
            if (customerType == "Business" && string.IsNullOrWhiteSpace(companyName))
                return Result<int>.Fail("A business account must give the company name.");
            if (customerType == "Individual")
                companyName = null;
            if (_accounts.UsernameExists(username.Trim()))
                return Result<int>.Fail("This username is already taken, please choose another one.");
            if (_accounts.EmailExists(email.Trim()))
                return Result<int>.Fail("There is already an account with this email address.");

            byte[] salt = PasswordHasher.CreateSalt();
            byte[] hash = PasswordHasher.Hash(password, salt);

            int id = _accounts.CreateCustomer(username.Trim(), hash, salt, fullName.Trim(),
                                              email.Trim(), phone.Trim(), customerType, companyName);
            if (id == 0)
                return Result<int>.Fail("The account could not be created. Please try again.");

            return Result<int>.Ok(id);
        }

        /// <summary>
        /// The current password is checked first, then a new salt is made and the new hash is
        /// stored. The old hash is never reused, so changing the password also rotates the salt.
        /// </summary>
        public Result ChangePasswordFor(string username, string currentPassword,
                                        string newPassword, string confirm)
        {
            var check = Login(username, currentPassword);
            if (!check.Success)
                return Result.Fail("The current password is not correct.");

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
                return Result.Fail("The new password must be at least 8 characters long.");
            if (newPassword != confirm)
                return Result.Fail("The two new passwords are not the same.");

            byte[] salt = PasswordHasher.CreateSalt();
            byte[] hash = PasswordHasher.Hash(newPassword, salt);
            _accounts.ChangePassword(check.Value.UserID, hash, salt);
            return Result.Ok();
        }

        public Result UpdateProfile(int customerId, string fullName, string email, string phone)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return Result.Fail("Please write your full name.");
            if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
                return Result.Fail("Please write a valid email address.");
            if (string.IsNullOrWhiteSpace(phone))
                return Result.Fail("Please write a phone number.");

            _accounts.UpdateCustomerProfile(customerId, fullName.Trim(), email.Trim(), phone.Trim());
            return Result.Ok();
        }

        public CustomerProfile Profile(int customerId)
        {
            return _accounts.GetProfile(customerId);
        }
    }
}
