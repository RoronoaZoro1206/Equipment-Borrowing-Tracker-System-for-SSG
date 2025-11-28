using System;
// using BCrypt.Net; // DISABLED FOR TESTING

namespace SupremeShare___Equipment_Borrowing_Tracker_System.Data
{
    public interface IPasswordHasher
    {
        string HashPassword(string password);
        bool VerifyPassword(string password, string hashedPassword);
    }

    public class PasswordHasher : IPasswordHasher
    {
        // Work factor of 12 is recommended (2^12 = 4096 iterations)
        // Higher = more secure but slower
        // private const int WorkFactor = 12; // DISABLED FOR TESTING

        public string HashPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password cannot be empty.");

            if (password.Length < 8)
                throw new ArgumentException("Password must be at least 8 characters long.");

            // ⚠️ TESTING MODE - Returns plaintext (DO NOT USE IN PRODUCTION!)
            return password;

            // ORIGINAL BCrypt implementation - UNCOMMENT FOR PRODUCTION:
            // return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
        }

        public bool VerifyPassword(string password, string hashedPassword)
        {
            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hashedPassword))
                return false;

            // ⚠️ TESTING MODE - Simple plaintext comparison (DO NOT USE IN PRODUCTION!)
            return password == hashedPassword;

            // ORIGINAL BCrypt implementation - UNCOMMENT FOR PRODUCTION:
            // try
            // {
            //     return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
            // }
            // catch
            // {
            //     return false;
            // }
        }
    }
}
