using System;
using System.Security.Cryptography;

namespace SupremeShare___Equipment_Borrowing_Tracker_System.Data
{
    public interface IPasswordHasher
    {
        string HashPassword(string password);
        bool VerifyPassword(string password, string hashedPassword);
    }

    public class PasswordHasher : IPasswordHasher
    {
        private const int SaltSize = 16; // 128 bits
        private const int KeySize = 32; // 256 bits
        private const int Iterations = 100000; // OWASP recommended minimum
        private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

        private const char Delimiter = ':';

        public string HashPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password cannot be empty.");

            if (password.Length < 8)
                throw new ArgumentException("Password must be at least 8 characters long.");

            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, KeySize);

            return string.Join(Delimiter, 
                Convert.ToBase64String(salt), 
                Convert.ToBase64String(hash),
                Iterations,
                Algorithm.Name);
        }

        public bool VerifyPassword(string password, string hashedPassword)
        {
            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hashedPassword))
                return false;

            try
            {
                var parts = hashedPassword.Split(Delimiter);
                if (parts.Length != 4)
                    return false;

                var salt = Convert.FromBase64String(parts[0]);
                var hash = Convert.FromBase64String(parts[1]);
                var iterations = int.Parse(parts[2]);
                var algorithm = new HashAlgorithmName(parts[3]);

                var inputHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, algorithm, hash.Length);

                return CryptographicOperations.FixedTimeEquals(inputHash, hash);
            }
            catch
            {
                return false;
            }
        }
    }
}
