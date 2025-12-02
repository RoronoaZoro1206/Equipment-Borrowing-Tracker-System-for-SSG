using System.Net;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using SupremeShare___Equipment_Borrowing_Tracker_System.Data;
using SupremeShare___Equipment_Borrowing_Tracker_System.Models.DTOs;
using SupremeShare___Equipment_Borrowing_Tracker_System.Repositories.Interfaces;
using SupremeShare___Equipment_Borrowing_Tracker_System.Services;

namespace SupremeShare___Equipment_Borrowing_Tracker_System.Controllers
{
    /// <summary>
    /// Account management controller for Admin and Support Admin roles
    /// </summary>
    public class AccountController : Controller
    {
        private readonly DatabaseHelper _db;
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IEmailService _emailService;
        private readonly IAuditService _auditService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            DatabaseHelper db,
            IUserRepository userRepository,
            IPasswordHasher passwordHasher,
            IEmailService emailService,
            IAuditService auditService,
            ILogger<AccountController> logger)
        {
            _db = db;
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _emailService = emailService;
            _auditService = auditService;
            _logger = logger;
        }

        /// <summary>
        /// Simple test endpoint to verify routing works
        /// </summary>
        [HttpGet]
        [Route("Account/Test")]
        public IActionResult Test()
        {
            return Content("Account controller is working!", "text/plain");
        }

        /// <summary>
        /// Check if user is authenticated and has valid role
        /// </summary>
        private bool IsAuthorized(out int userId, out string role)
        {
            userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            role = HttpContext.Session.GetString("UserRole") ?? "";
            
            if (userId == 0) return false;
            
            var validRoles = new[] { "admin", "support_admin" };
            return validRoles.Contains(role.ToLower());
        }

        /// <summary>
        /// GET: /Account - Display account page
        /// </summary>
        [HttpGet]
        [Route("Account")]
        [Route("Admin/Account")]
        [Route("SupportAdmin/Account")]
        public async Task<IActionResult> Index()
        {
            if (!IsAuthorized(out int userId, out string role))
                return RedirectToAction("Login", "Auth");

            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                    return RedirectToAction("Login", "Auth");

                // Check for pending email verification
                var pendingEmail = await GetPendingEmailVerificationAsync(userId);

                var viewModel = new AccountViewModel
                {
                    UserId = user.UserId,
                    Username = user.UserUsername,
                    Email = user.UserEmail ?? "",
                    Role = user.UserRole,
                    Position = user.UserPosition,
                    LastName = user.UserLastName,
                    FirstName = user.UserFirstName,
                    MiddleName = user.UserMiddleName,
                    Suffix = user.UserSuffix,
                    ContactNo = user.UserContactNo,
                    LastUpdated = user.UserLastUpdated,
                    PendingEmail = pendingEmail,
                    HasPendingEmailVerification = !string.IsNullOrEmpty(pendingEmail)
                };

                ViewBag.FullName = HttpContext.Session.GetString("FullName");
                ViewBag.Role = role;
                ViewBag.UserId = userId;
                ViewBag.Username = HttpContext.Session.GetString("Username");

                return View("~/Views/Account/Index.cshtml", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading account page for user {UserId}", userId);
                TempData["Error"] = "An error occurred while loading your account information.";
                return RedirectToAction("Index", role.ToLower() == "admin" ? "Admin" : "SupportAdmin");
            }
        }

        /// <summary>
        /// GET: /Account/GetAccountInfo - Get current user's account info
        /// </summary>
        [HttpGet]
        [Route("Account/GetAccountInfo")]
        public async Task<IActionResult> GetAccountInfo()
        {
            if (!IsAuthorized(out int userId, out _))
                return Json(new { success = false, message = "Unauthorized" });

            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                    return Json(new { success = false, message = "User not found" });

                var pendingEmail = await GetPendingEmailVerificationAsync(userId);

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        userId = user.UserId,
                        username = user.UserUsername,
                        email = user.UserEmail ?? "",
                        role = user.UserRole,
                        position = user.UserPosition,
                        lastName = user.UserLastName,
                        firstName = user.UserFirstName,
                        middleName = user.UserMiddleName ?? "",
                        suffix = user.UserSuffix ?? "",
                        contactNo = user.UserContactNo ?? "",
                        lastUpdated = user.UserLastUpdated.ToString("MMMM dd, yyyy 'at' hh:mm tt"),
                        pendingEmail = pendingEmail ?? "",
                        hasPendingEmailVerification = !string.IsNullOrEmpty(pendingEmail)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting account info for user {UserId}", userId);
                return Json(new { success = false, message = "An error occurred" });
            }
        }

        /// <summary>
        /// POST: /Account/UpdateUsername - Update username only
        /// </summary>
        [HttpPost]
        [Route("Account/UpdateUsername")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUsername([FromBody] UpdateAccountInfoDto dto)
        {
            if (!IsAuthorized(out int userId, out _))
                return Json(new { success = false, message = "Unauthorized" });

            if (!ModelState.IsValid)
                return Json(new { success = false, message = GetModelErrors() });

            try
            {
                // Check if username is already taken by another user
                var existingUser = await _userRepository.GetByUsernameAsync(dto.Username);
                if (existingUser != null && existingUser.UserId != userId)
                    return Json(new { success = false, message = "Username is already taken" });

                var query = @"UPDATE ""USER"" SET 
                    user_username = @username, 
                    user_last_updated = CURRENT_TIMESTAMP 
                    WHERE user_id = @userId";

                var parameters = new NpgsqlParameter[]
                {
                    new("@username", dto.Username),
                    new("@userId", userId)
                };

                var result = await _db.ExecuteNonQueryAsync(query, parameters);

                if (result > 0)
                {
                    // Update session
                    HttpContext.Session.SetString("Username", dto.Username);
                    
                    await _auditService.LogActionAsync(userId, "USERNAME_UPDATED", null, new { newUsername = dto.Username });
                    _logger.LogInformation("User {UserId} updated username to {Username}", userId, dto.Username);
                    
                    return Json(new { success = true, message = "Username updated successfully" });
                }

                return Json(new { success = false, message = "Failed to update username" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating username for user {UserId}", userId);
                return Json(new { success = false, message = "An error occurred while updating username" });
            }
        }

        /// <summary>
        /// POST: /Account/RequestEmailChange - Request email change (sends verification)
        /// </summary>
        [HttpPost]
        [Route("Account/RequestEmailChange")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestEmailChange([FromBody] ChangeEmailDto dto)
        {
            if (!IsAuthorized(out int userId, out _))
                return Json(new { success = false, message = "Unauthorized" });

            if (!ModelState.IsValid)
                return Json(new { success = false, message = GetModelErrors() });

            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                    return Json(new { success = false, message = "User not found" });

                // Check if email is same as current
                if (user.UserEmail?.ToLower() == dto.NewEmail.ToLower())
                    return Json(new { success = false, message = "New email is the same as current email" });

                // Check if email is already used by another user
                var emailCheckQuery = @"SELECT user_id FROM ""USER"" WHERE LOWER(user_email) = LOWER(@email) AND user_id != @userId";
                var emailCheckResult = await _db.ExecuteQueryAsync(emailCheckQuery, new[]
                {
                    new NpgsqlParameter("@email", dto.NewEmail),
                    new NpgsqlParameter("@userId", userId)
                });
                if (emailCheckResult.Rows.Count > 0)
                    return Json(new { success = false, message = "This email is already registered to another user" });

                // Generate verification token
                var token = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
                var expiresAt = DateTime.UtcNow.AddHours(24);

                // Store pending email verification
                await StorePendingEmailVerificationAsync(userId, dto.NewEmail, token, expiresAt);

                // Send verification email
                var emailSent = await _emailService.SendEmailVerificationAsync(dto.NewEmail, token, user.UserUsername);

                if (emailSent)
                {
                    await _auditService.LogActionAsync(userId, "EMAIL_CHANGE_REQUESTED", 
                        new { currentEmail = user.UserEmail }, 
                        new { newEmail = dto.NewEmail });
                    
                    return Json(new { 
                        success = true, 
                        message = "Verification email sent! Please check your inbox and click the verification link." 
                    });
                }
                else
                {
                    return Json(new { 
                        success = false, 
                        message = "Failed to send verification email. Please check SMTP configuration or try again later." 
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting email change for user {UserId}", userId);
                return Json(new { success = false, message = "An error occurred while processing your request" });
            }
        }

        /// <summary>
        /// GET: /Account/VerifyEmail - Verify email from link
        /// </summary>
        [HttpGet]
        [Route("Account/VerifyEmail")]
        public async Task<IActionResult> VerifyEmail([FromQuery] string token)
        {
            // Return simple HTML directly to avoid any view rendering issues
            try
            {
                _logger.LogInformation("VerifyEmail called with token: {Token}", 
                    string.IsNullOrEmpty(token) ? "null" : token.Substring(0, Math.Min(10, token.Length)) + "...");

                bool success = false;
                string message = "";
                string? verifiedEmail = null;

                if (string.IsNullOrEmpty(token))
                {
                    message = "Invalid verification link.";
                }
                else
                {
                    // Get pending verification
                    var query = @"SELECT user_id, evt_new_email, evt_expires_at, evt_is_used 
                        FROM EMAIL_VERIFICATION_TOKEN 
                        WHERE evt_token = @token";

                    var dt = await _db.ExecuteQueryAsync(query, new[] { new NpgsqlParameter("@token", token) });

                    if (dt.Rows.Count == 0)
                    {
                        _logger.LogWarning("Token not found in database");
                        message = "Invalid or expired verification link.";
                    }
                    else
                    {
                        var row = dt.Rows[0];
                        var userId = Convert.ToInt32(row["user_id"]);
                        var newEmail = row["evt_new_email"]?.ToString() ?? "";
                        var isUsed = Convert.ToBoolean(row["evt_is_used"]);

                        // Handle timestamp
                        var expiresAtValue = row["evt_expires_at"];
                        DateTime expiresAt;
                        if (expiresAtValue is DateTimeOffset dto)
                            expiresAt = dto.UtcDateTime;
                        else if (expiresAtValue is DateTime dt2)
                            expiresAt = dt2.Kind == DateTimeKind.Utc ? dt2 : dt2.ToUniversalTime();
                        else
                            expiresAt = Convert.ToDateTime(expiresAtValue);

                        if (isUsed)
                        {
                            message = "This verification link has already been used.";
                        }
                        else if (DateTime.UtcNow > expiresAt)
                        {
                            message = "This verification link has expired. Please request a new one.";
                        }
                        else
                        {
                            // Update user email
                            var updateQuery = @"UPDATE ""USER"" SET 
                                user_email = @email, 
                                user_last_updated = CURRENT_TIMESTAMP 
                                WHERE user_id = @userId";
                            await _db.ExecuteNonQueryAsync(updateQuery, new[]
                            {
                                new NpgsqlParameter("@email", newEmail),
                                new NpgsqlParameter("@userId", userId)
                            });

                            // Mark token as used
                            var markUsedQuery = @"UPDATE EMAIL_VERIFICATION_TOKEN SET evt_is_used = TRUE WHERE evt_token = @token";
                            await _db.ExecuteNonQueryAsync(markUsedQuery, new[] { new NpgsqlParameter("@token", token) });

                            try
                            {
                                await _auditService.LogActionAsync(userId, "EMAIL_VERIFIED", null, new { newEmail });
                            }
                            catch { /* Ignore audit errors */ }

                            _logger.LogInformation("Email verified successfully for user {UserId}", userId);
                            success = true;
                            message = "Your email has been verified successfully!";
                            verifiedEmail = newEmail;
                        }
                    }
                }

                // Return inline HTML to avoid view rendering issues
                return Content(GenerateVerificationHtml(success, message, verifiedEmail), "text/html");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying email: {Message}", ex.Message);
                return Content(GenerateVerificationHtml(false, "An error occurred while verifying your email. Please try again.", null), "text/html");
            }
        }

        /// <summary>
        /// Generate verification result HTML
        /// </summary>
        private string GenerateVerificationHtml(bool success, string message, string? email)
        {
            var iconClass = success ? "icon-success" : "icon-error";
            var iconSymbol = success ? "&#10003;" : "&#10007;";
            var title = success ? "Email Verified!" : "Verification Failed";
            var encodedMessage = WebUtility.HtmlEncode(message);
            var emailBadge = !string.IsNullOrEmpty(email) 
                ? $"<div class=\"email-badge\">&#9993; {WebUtility.HtmlEncode(email)}</div>" 
                : "";

            return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>{title} - SupremeShare</title>
    <style>
        :root {{ --maroon: #8B0000; --maroon-dark: #6B0000; }}
        body {{ background: linear-gradient(135deg, #f5f7fa 0%, #e4e8ec 100%); min-height: 100vh; display: flex; align-items: center; justify-content: center; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; margin: 0; }}
        .card {{ background: white; border-radius: 16px; box-shadow: 0 10px 40px rgba(0,0,0,0.1); padding: 50px 40px; text-align: center; max-width: 480px; width: 100%; margin: 20px; }}
        .logo {{ color: var(--maroon); font-size: 32px; font-weight: 700; margin-bottom: 30px; }}
        .icon {{ width: 100px; height: 100px; border-radius: 50%; display: flex; align-items: center; justify-content: center; margin: 0 auto 25px; font-size: 50px; }}
        .icon-success {{ background: linear-gradient(135deg, #d4edda 0%, #c3e6cb 100%); color: #28a745; }}
        .icon-error {{ background: linear-gradient(135deg, #f8d7da 0%, #f5c6cb 100%); color: #dc3545; }}
        h1 {{ color: #333; font-size: 28px; font-weight: 600; margin-bottom: 15px; }}
        .message {{ color: #666; font-size: 16px; line-height: 1.6; margin-bottom: 30px; }}
        .email-badge {{ display: inline-block; background: #f8f9fa; border: 1px solid #e9ecef; padding: 8px 16px; border-radius: 20px; font-size: 14px; color: #495057; margin-bottom: 25px; }}
        .btn {{ background: var(--maroon); border: none; color: white; padding: 14px 35px; border-radius: 8px; font-size: 16px; font-weight: 600; text-decoration: none; display: inline-block; transition: all 0.3s ease; }}
        .btn:hover {{ background: var(--maroon-dark); color: white; transform: translateY(-2px); }}
        .footer {{ margin-top: 30px; color: #999; font-size: 13px; }}
    </style>
</head>
<body>
    <div class=""card"">
        <div class=""logo"">&#128230; SupremeShare</div>
        <div class=""icon {iconClass}"">{iconSymbol}</div>
        <h1>{title}</h1>
        <p class=""message"">{encodedMessage}</p>
        {emailBadge}
        <a href=""/Auth/Login"" class=""btn"">Go to Login</a>
        <p class=""footer"">&copy; 2025 SupremeShare - Equipment Borrowing Tracker System</p>
    </div>
</body>
</html>";
        }

        /// <summary>
        /// POST: /Account/ChangePassword - Change password
        /// </summary>
        [HttpPost]
        [Route("Account/ChangePassword")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            if (!IsAuthorized(out int userId, out _))
                return Json(new { success = false, message = "Unauthorized" });

            if (!ModelState.IsValid)
                return Json(new { success = false, message = GetModelErrors() });

            try
            {
                // Get user with password hash
                var query = @"SELECT user_hashed_password, user_username, user_email 
                    FROM ""USER"" WHERE user_id = @userId";
                var dt = await _db.ExecuteQueryAsync(query, new[] { new NpgsqlParameter("@userId", userId) });

                if (dt.Rows.Count == 0)
                    return Json(new { success = false, message = "User not found" });

                var currentHash = dt.Rows[0]["user_hashed_password"].ToString();
                var username = dt.Rows[0]["user_username"].ToString();
                var email = dt.Rows[0]["user_email"]?.ToString();

                // Verify current password
                if (!_passwordHasher.VerifyPassword(dto.CurrentPassword, currentHash!))
                    return Json(new { success = false, message = "Current password is incorrect" });

                // Check if new password is same as current
                if (_passwordHasher.VerifyPassword(dto.NewPassword, currentHash!))
                    return Json(new { success = false, message = "New password cannot be the same as current password" });

                // Hash new password
                var newHash = _passwordHasher.HashPassword(dto.NewPassword);

                // Update password
                var updateQuery = @"UPDATE ""USER"" SET 
                    user_hashed_password = @password, 
                    user_last_updated = CURRENT_TIMESTAMP 
                    WHERE user_id = @userId";
                var result = await _db.ExecuteNonQueryAsync(updateQuery, new[]
                {
                    new NpgsqlParameter("@password", newHash),
                    new NpgsqlParameter("@userId", userId)
                });

                if (result > 0)
                {
                    await _auditService.LogActionAsync(userId, "PASSWORD_CHANGED", null, null);
                    _logger.LogInformation("User {UserId} changed password", userId);

                    // Send notification email if email exists
                    if (!string.IsNullOrEmpty(email))
                    {
                        _ = _emailService.SendPasswordChangedNotificationAsync(email, username!);
                    }

                    return Json(new { success = true, message = "Password changed successfully" });
                }

                return Json(new { success = false, message = "Failed to change password" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user {UserId}", userId);
                return Json(new { success = false, message = "An error occurred while changing password" });
            }
        }

        /// <summary>
        /// POST: /Account/UpdatePersonalInfo - Update personal information
        /// </summary>
        [HttpPost]
        [Route("Account/UpdatePersonalInfo")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePersonalInfo([FromBody] UpdatePersonalInfoDto dto)
        {
            if (!IsAuthorized(out int userId, out _))
                return Json(new { success = false, message = "Unauthorized" });

            if (!ModelState.IsValid)
                return Json(new { success = false, message = GetModelErrors() });

            try
            {
                var query = @"UPDATE ""USER"" SET 
                    user_last_name = @lastName,
                    user_first_name = @firstName,
                    user_middle_name = @middleName,
                    user_suffix = @suffix,
                    user_contact_no = @contactNo,
                    user_last_updated = CURRENT_TIMESTAMP 
                    WHERE user_id = @userId
                    RETURNING user_last_updated";

                var parameters = new NpgsqlParameter[]
                {
                    new("@lastName", dto.LastName),
                    new("@firstName", dto.FirstName),
                    new("@middleName", (object?)dto.MiddleName ?? DBNull.Value),
                    new("@suffix", (object?)dto.Suffix ?? DBNull.Value),
                    new("@contactNo", (object?)dto.ContactNo ?? DBNull.Value),
                    new("@userId", userId)
                };

                var dt = await _db.ExecuteQueryAsync(query, parameters);

                if (dt.Rows.Count > 0)
                {
                    var lastUpdated = Convert.ToDateTime(dt.Rows[0]["user_last_updated"]);
                    
                    // Update session
                    HttpContext.Session.SetString("FullName", $"{dto.FirstName} {dto.LastName}");
                    
                    await _auditService.LogActionAsync(userId, "PERSONAL_INFO_UPDATED", null, 
                        new { dto.LastName, dto.FirstName, dto.MiddleName, dto.Suffix, dto.ContactNo });
                    
                    return Json(new { 
                        success = true, 
                        message = "Personal information updated successfully",
                        lastUpdated = lastUpdated.ToString("MMMM dd, yyyy 'at' hh:mm tt")
                    });
                }

                return Json(new { success = false, message = "Failed to update personal information" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating personal info for user {UserId}", userId);
                return Json(new { success = false, message = "An error occurred while updating personal information" });
            }
        }

        /// <summary>
        /// POST: /Account/CancelEmailChange - Cancel pending email verification
        /// </summary>
        [HttpPost]
        [Route("Account/CancelEmailChange")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelEmailChange()
        {
            if (!IsAuthorized(out int userId, out _))
                return Json(new { success = false, message = "Unauthorized" });

            try
            {
                var query = @"DELETE FROM EMAIL_VERIFICATION_TOKEN WHERE user_id = @userId AND evt_is_used = FALSE";
                await _db.ExecuteNonQueryAsync(query, new[] { new NpgsqlParameter("@userId", userId) });

                await _auditService.LogActionAsync(userId, "EMAIL_CHANGE_CANCELLED", null, null);
                
                return Json(new { success = true, message = "Email change request cancelled" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling email change for user {UserId}", userId);
                return Json(new { success = false, message = "An error occurred" });
            }
        }

        #region Helper Methods

        private async Task<string?> GetPendingEmailVerificationAsync(int userId)
        {
            try
            {
                var query = @"SELECT evt_new_email FROM EMAIL_VERIFICATION_TOKEN 
                    WHERE user_id = @userId AND evt_is_used = FALSE AND evt_expires_at > @now
                    ORDER BY evt_date_created DESC LIMIT 1";
                var dt = await _db.ExecuteQueryAsync(query, new[]
                {
                    new NpgsqlParameter("@userId", userId),
                    new NpgsqlParameter("@now", DateTime.UtcNow)
                });

                return dt.Rows.Count > 0 ? dt.Rows[0]["evt_new_email"].ToString() : null;
            }
            catch
            {
                return null;
            }
        }

        private async Task StorePendingEmailVerificationAsync(int userId, string newEmail, string token, DateTime expiresAt)
        {
            // Delete any existing pending verifications for this user
            var deleteQuery = @"DELETE FROM EMAIL_VERIFICATION_TOKEN WHERE user_id = @userId AND evt_is_used = FALSE";
            await _db.ExecuteNonQueryAsync(deleteQuery, new[] { new NpgsqlParameter("@userId", userId) });

            // Insert new verification token
            var insertQuery = @"INSERT INTO EMAIL_VERIFICATION_TOKEN 
                (user_id, evt_token, evt_new_email, evt_expires_at, evt_date_created, evt_is_used)
                VALUES (@userId, @token, @newEmail, @expiresAt, @createdAt, FALSE)";
            await _db.ExecuteNonQueryAsync(insertQuery, new[]
            {
                new NpgsqlParameter("@userId", userId),
                new NpgsqlParameter("@token", token),
                new NpgsqlParameter("@newEmail", newEmail),
                new NpgsqlParameter("@expiresAt", expiresAt),
                new NpgsqlParameter("@createdAt", DateTime.UtcNow)
            });
        }

        private string GetModelErrors()
        {
            return string.Join("; ", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage));
        }

        #endregion
    }
}
