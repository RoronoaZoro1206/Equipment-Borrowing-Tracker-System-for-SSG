using System;
using BCrypt.Net;

namespace SupremeShare___Equipment_Borrowing_Tracker_System.Data
{
    /// <summary>
    /// Interface for password hashing operations
    /// </summary>
    public interface IPasswordHasher
    {
        /// <summary>
        /// Hashes a plaintext password using BCrypt
        /// </summary>
        string HashPassword(string password);
        
        /// <summary>
        /// Verifies a plaintext password against a BCrypt hash
        /// </summary>
        bool VerifyPassword(string password, string hashedPassword);
        
        /// <summary>
        /// Checks if a hash needs to be upgraded (work factor changed)
        /// </summary>
        bool NeedsRehash(string hashedPassword);
    }

    /// <summary>
    /// BCrypt password hasher implementation
    /// 
    /// Why BCrypt in Application Layer (not PostgreSQL pgcrypto)?
    /// 1. Security: Password never sent to DB in plaintext
    /// 2. Portability: Works with any database
    /// 3. Testability: Easy to mock in unit tests
    /// 4. Performance: Offloads CPU-intensive hashing from DB server
    /// 5. Separation of Concerns: Auth logic stays in application layer
    /// </summary>
    public class PasswordHasher : IPasswordHasher
    {
        // Work factor of 12 is recommended for 2024 (2^12 = 4096 iterations)
        // Increase by 1 every ~2 years as hardware improves
        // 12 = ~250ms on modern hardware, good balance of security/UX
        private const int WorkFactor = 12;

        /// <summary>
        /// Hashes a password using BCrypt with automatic salt generation
        /// </summary>
        /// <param name="password">Plaintext password to hash</param>
        /// <returns>BCrypt hash string (includes algorithm, cost, salt, and hash)</returns>
        /// <exception cref="ArgumentException">If password is null, empty, or too short</exception>
        public string HashPassword(string password)
        {
            ValidatePassword(password);
            
            // BCrypt.HashPassword automatically:
            // 1. Generates a cryptographically secure random salt
            // 2. Applies the work factor (cost)
            // 3. Returns format: $2a$12$[22-char salt][31-char hash]
            return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
        }

        /// <summary>
        /// Verifies a plaintext password against a stored BCrypt hash
        /// </summary>
        /// <param name="password">Plaintext password to verify</param>
        /// <param name="hashedPassword">Stored BCrypt hash</param>
        /// <returns>True if password matches, false otherwise</returns>
        public bool VerifyPassword(string password, string hashedPassword)
        {
            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hashedPassword))
                return false;

            try
            {
                // BCrypt.Verify extracts salt from hash and compares
                // Timing-safe comparison prevents timing attacks
                return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
            }
            catch (SaltParseException)
            {
                // Invalid hash format (not a BCrypt hash)
                return false;
            }
            catch (Exception)
            {
                // Any other error = verification failed
                return false;
            }
        }

        /// <summary>
        /// Checks if a password hash needs to be upgraded
        /// (e.g., if work factor has been increased)
        /// </summary>
        /// <param name="hashedPassword">Existing BCrypt hash</param>
        /// <returns>True if hash should be regenerated with new work factor</returns>
        public bool NeedsRehash(string hashedPassword)
        {
            if (string.IsNullOrWhiteSpace(hashedPassword))
                return true;

            try
            {
                // Extract work factor from hash (format: $2a$XX$...)
                // BCrypt hash format: $[version]$[cost]$[22-char salt][31-char hash]
                var parts = hashedPassword.Split('$');
                if (parts.Length < 3)
                    return true;

                if (int.TryParse(parts[2], out int currentWorkFactor))
                {
                    return currentWorkFactor < WorkFactor;
                }
                
                return true;
            }
            catch
            {
                return true;
            }
        }

        /// <summary>
        /// Validates password meets minimum requirements
        /// </summary>
        private static void ValidatePassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password cannot be empty.");

            if (password.Length < 8)
                throw new ArgumentException("Password must be at least 8 characters long.");

            if (password.Length > 72)
                throw new ArgumentException("Password cannot exceed 72 characters (BCrypt limit).");
        }
    }
}
