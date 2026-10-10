using System.Security.Cryptography;

namespace SwiftShip.Web.Services
{
    /// <summary>
    /// Passwords are never stored. We keep a random salt and the PBKDF2 hash of the
    /// password made with that salt, which is what the requirements file asks for when it
    /// says "encrypted password is recommended".
    /// The same parameters (SHA-256, 100 000 iterations, 32-byte salt, 64-byte key) are used
    /// by tools/build_seed_data.py, so the seeded accounts work with this code.
    /// </summary>
    public static class PasswordHasher
    {
        public const int SaltSize = 32;
        public const int KeySize = 64;
        public const int Iterations = 100000;

        public static byte[] CreateSalt()
        {
            var salt = new byte[SaltSize];
            RandomNumberGenerator.Fill(salt);
            return salt;
        }

        public static byte[] Hash(string password, byte[] salt)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256))
            {
                return pbkdf2.GetBytes(KeySize);
            }
        }

        public static bool Verify(string password, byte[] salt, byte[] expectedHash)
        {
            if (salt == null || expectedHash == null)
                return false;
            return FixedTimeEquals(Hash(password, salt), expectedHash);
        }

        // Comparing all the bytes even after a difference makes the comparison take the same
        // time whether the password is right or wrong, so it cannot be timed.
        private static bool FixedTimeEquals(byte[] a, byte[] b)
        {
            if (a.Length != b.Length)
                return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++)
                diff |= a[i] ^ b[i];
            return diff == 0;
        }
    }
}
