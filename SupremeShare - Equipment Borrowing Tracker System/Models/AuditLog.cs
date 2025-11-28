using System;

namespace SupremeShare___Equipment_Borrowing_Tracker_System.Models
{
    public class AuditLog
    {
        public int AuditId { get; set; }
        public int UserId { get; set; }
        public string AuditAction { get; set; } = string.Empty;
        public string? AuditOldData { get; set; }
        public string? AuditNewData { get; set; }
        public DateTime AuditLoggedAt { get; set; } = DateTime.Now;

        // Navigation property
        public User? User { get; set; }
    }
}
