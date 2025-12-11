using System;
using System.ComponentModel.DataAnnotations;

namespace SupremeShare___Equipment_Borrowing_Tracker_System.Models
{
    public class AuditLog
    {
        public int AuditId { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "User ID must be a non-negative number")]
        [Display(Name = "User")]
        public int UserId { get; set; }

        [Required(ErrorMessage = "Action is required")]
        [StringLength(255, ErrorMessage = "Action cannot exceed 255 characters")]
        [Display(Name = "Action")]
        public string AuditAction { get; set; } = string.Empty;

        [Display(Name = "Previous Data")]
        public string? AuditOldData { get; set; }

        [Display(Name = "New Data")]
        public string? AuditNewData { get; set; }

        [Display(Name = "Logged At")]
        public DateTime AuditLoggedAt { get; set; } = DateTime.Now;

        // Navigation property
        public User? User { get; set; }
    }
}
