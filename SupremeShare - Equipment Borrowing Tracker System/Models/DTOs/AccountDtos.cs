using System.ComponentModel.DataAnnotations;

namespace SupremeShare___Equipment_Borrowing_Tracker_System.Models.DTOs
{
    /// <summary>
    /// DTO for updating account information (username only directly editable)
    /// </summary>
    public class UpdateAccountInfoDto
    {
        [Required(ErrorMessage = "Username is required")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters")]
        [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "Username can only contain letters, numbers, and underscores")]
        public string Username { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for email change request (triggers verification flow)
    /// </summary>
    public class ChangeEmailDto
    {
        [Required(ErrorMessage = "New email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
        public string NewEmail { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for password change
    /// </summary>
    public class ChangePasswordDto
    {
        [Required(ErrorMessage = "Current password is required")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "New password is required")]
        [StringLength(72, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 72 characters")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$", 
            ErrorMessage = "Password must contain at least one uppercase, one lowercase, one number, and one special character")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirm password is required")]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for updating personal information
    /// </summary>
    public class UpdatePersonalInfoDto
    {
        [Required(ErrorMessage = "Last name is required")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "Last name must be between 1 and 100 characters")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "First name is required")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "First name must be between 1 and 100 characters")]
        public string FirstName { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "Middle name cannot exceed 100 characters")]
        public string? MiddleName { get; set; }

        [StringLength(20, ErrorMessage = "Suffix cannot exceed 20 characters")]
        public string? Suffix { get; set; }

        [StringLength(20, ErrorMessage = "Contact number cannot exceed 20 characters")]
        [RegularExpression(@"^[\d\s\-\+\(\)]*$", ErrorMessage = "Invalid contact number format")]
        public string? ContactNo { get; set; }
    }

    /// <summary>
    /// DTO for account view model
    /// </summary>
    public class AccountViewModel
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string? MiddleName { get; set; }
        public string? Suffix { get; set; }
        public string? ContactNo { get; set; }
        public DateTime LastUpdated { get; set; }
        public string? PendingEmail { get; set; }
        public bool HasPendingEmailVerification { get; set; }
    }

    /// <summary>
    /// Email verification token model
    /// </summary>
    public class EmailVerificationToken
    {
        public int UserId { get; set; }
        public string Token { get; set; } = string.Empty;
        public string NewEmail { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsUsed { get; set; }
    }

    /// <summary>
    /// Model for email verification result page
    /// </summary>
    public class EmailVerificationResultModel
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "An error occurred.";
        public string? Email { get; set; }
        public string Title => Success ? "Email Verified!" : "Verification Failed";
    }
}
