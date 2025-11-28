using System;
using System.Text.RegularExpressions;
using SupremeShare___Equipment_Borrowing_Tracker_System.Exceptions;

namespace SupremeShare___Equipment_Borrowing_Tracker_System.Data
{
    public static class InputValidator
    {
        // Regex patterns for validation
        private static readonly Regex EmailPattern = new(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", RegexOptions.Compiled);
        private static readonly Regex PhonePattern = new(@"^[\d\s\-\+\(\)]{7,20}$", RegexOptions.Compiled);
        private static readonly Regex UsernamePattern = new(@"^[a-zA-Z0-9_]{3,50}$", RegexOptions.Compiled);
        private static readonly Regex AlphaNumericPattern = new(@"^[a-zA-Z0-9\s\-_\.]+$", RegexOptions.Compiled);
        private static readonly Regex NamePattern = new(@"^[a-zA-Z\s\-'\.]+$", RegexOptions.Compiled);
        private static readonly Regex SqlInjectionPattern = new(@"('|--|;|/\*|\*/|xp_|sp_|exec|execute|insert|update|delete|drop|alter|create|truncate|union|select)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static string SanitizeString(string? input, int maxLength = 255)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            // Trim and limit length
            var sanitized = input.Trim();
            if (sanitized.Length > maxLength)
                sanitized = sanitized.Substring(0, maxLength);

            // Check for SQL injection patterns
            if (SqlInjectionPattern.IsMatch(sanitized))
                throw new ValidationException("Invalid characters detected in input.");

            return sanitized;
        }

        public static void ValidateEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email)) return;
            
            if (!EmailPattern.IsMatch(email))
                throw new ValidationException("Invalid email format.");
        }

        public static void ValidatePhone(string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return;
            
            if (!PhonePattern.IsMatch(phone))
                throw new ValidationException("Invalid phone number format.");
        }

        public static void ValidateUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new ValidationException("Username is required.");
            
            if (!UsernamePattern.IsMatch(username))
                throw new ValidationException("Username must be 3-50 characters and contain only letters, numbers, and underscores.");
        }

        public static void ValidateName(string? name, string fieldName, bool required = true)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                if (required)
                    throw new ValidationException($"{fieldName} is required.");
                return;
            }
            
            if (!NamePattern.IsMatch(name))
                throw new ValidationException($"{fieldName} contains invalid characters.");
        }

        public static void ValidateId(int id, string fieldName)
        {
            if (id <= 0)
                throw new ValidationException($"{fieldName} must be a positive number.");
        }

        public static void ValidateSerialNumber(string serialNo)
        {
            if (string.IsNullOrWhiteSpace(serialNo))
                throw new ValidationException("Serial number is required.");
            
            if (!AlphaNumericPattern.IsMatch(serialNo))
                throw new ValidationException("Serial number contains invalid characters.");
        }

        public static void ValidateRole(string role)
        {
            var validRoles = new[] { "admin", "support_admin" };
            if (!Array.Exists(validRoles, r => r.Equals(role, StringComparison.OrdinalIgnoreCase)))
                throw new ValidationException("Invalid user role.");
        }

        public static void ValidateBorrowStatus(string status)
        {
            var validStatuses = new[] { "borrowed", "returned", "overdue", "lost" };
            if (!Array.Exists(validStatuses, s => s.Equals(status, StringComparison.OrdinalIgnoreCase)))
                throw new ValidationException("Invalid borrow status.");
        }

        public static void ValidateEquipmentStatus(string status)
        {
            var validStatuses = new[] { "available", "borrowed", "for review", "lost", "maintenance" };
            if (!Array.Exists(validStatuses, s => s.Equals(status, StringComparison.OrdinalIgnoreCase)))
                throw new ValidationException("Invalid equipment status.");
        }
    }
}
