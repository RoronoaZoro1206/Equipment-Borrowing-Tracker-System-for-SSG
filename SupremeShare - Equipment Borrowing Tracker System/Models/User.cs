using System;

namespace SupremeShare___Equipment_Borrowing_Tracker_System.Models
{
    public class User
    {
        public int UserId { get; set; }
        public string UserUsername { get; set; } = string.Empty;
        public string UserHashedPassword { get; set; } = string.Empty;
        public string UserRole { get; set; } = string.Empty;
        public string UserPosition { get; set; } = string.Empty;
        public string UserLastName { get; set; } = string.Empty;
        public string UserFirstName { get; set; } = string.Empty;
        public string? UserMiddleName { get; set; }
        public string? UserSuffix { get; set; }
        public string? UserEmail { get; set; }
        public string? UserContactNo { get; set; }
        public bool UserStatus { get; set; } = true;
        public DateTime UserDateCreated { get; set; } = DateTime.Now;
        public DateTime UserLastUpdated { get; set; } = DateTime.Now;

        // Computed property for display name
        public string FullName => !string.IsNullOrEmpty(UserFirstName)
            ? $"{UserFirstName} {UserLastName}".Trim()
            : string.Empty;
    }
}
