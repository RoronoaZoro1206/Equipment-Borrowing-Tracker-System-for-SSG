using System;
using System.ComponentModel.DataAnnotations;

namespace SupremeShare___Equipment_Borrowing_Tracker_System.Models
{
    public class User
    {
        public int UserId { get; set; }

        [Required(ErrorMessage = "Username is required")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters")]
        [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "Username can only contain letters, numbers, and underscores")]
        [Display(Name = "Username")]
        public string UserUsername { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [StringLength(255, ErrorMessage = "Password hash cannot exceed 255 characters")]
        public string UserHashedPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Role is required")]
        [RegularExpression(@"^(admin|support_admin)$", ErrorMessage = "Role must be 'admin' or 'support_admin'")]
        [Display(Name = "Role")]
        public string UserRole { get; set; } = string.Empty;

        [Required(ErrorMessage = "Position is required")]
        [StringLength(100, ErrorMessage = "Position cannot exceed 100 characters")]
        [Display(Name = "Position")]
        public string UserPosition { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(100, ErrorMessage = "Last name cannot exceed 100 characters")]
        [RegularExpression(@"^[a-zA-Z\s\-'\.]+$", ErrorMessage = "Last name contains invalid characters")]
        [Display(Name = "Last Name")]
        public string UserLastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "First name is required")]
        [StringLength(100, ErrorMessage = "First name cannot exceed 100 characters")]
        [RegularExpression(@"^[a-zA-Z\s\-'\.]+$", ErrorMessage = "First name contains invalid characters")]
        [Display(Name = "First Name")]
        public string UserFirstName { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "Middle name cannot exceed 100 characters")]
        [RegularExpression(@"^[a-zA-Z\s\-'\.]*$", ErrorMessage = "Middle name contains invalid characters")]
        [Display(Name = "Middle Name")]
        public string? UserMiddleName { get; set; }

        [StringLength(20, ErrorMessage = "Suffix cannot exceed 20 characters")]
        [Display(Name = "Suffix")]
        public string? UserSuffix { get; set; }

        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
        [Display(Name = "Email")]
        public string? UserEmail { get; set; }

        [Phone(ErrorMessage = "Invalid contact number format")]
        [StringLength(20, ErrorMessage = "Contact number cannot exceed 20 characters")]
        [RegularExpression(@"^[\d\s\-\+\(\)]*$", ErrorMessage = "Contact number contains invalid characters")]
        [Display(Name = "Contact Number")]
        public string? UserContactNo { get; set; }

        [Display(Name = "Active Status")]
        public bool UserStatus { get; set; } = true;

        [Display(Name = "Date Created")]
        public DateTime UserDateCreated { get; set; } = DateTime.Now;

        [Display(Name = "Last Updated")]
        public DateTime UserLastUpdated { get; set; } = DateTime.Now;

        // Computed property for display name
        public string FullName => !string.IsNullOrEmpty(UserFirstName)
            ? $"{UserFirstName} {UserLastName}".Trim()
            : string.Empty;
    }
}
