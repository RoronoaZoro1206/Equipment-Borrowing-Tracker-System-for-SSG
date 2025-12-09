using Microsoft.AspNetCore.Mvc;
using SupremeShare___Equipment_Borrowing_Tracker_System.Attributes;
using SupremeShare___Equipment_Borrowing_Tracker_System.Data;
using SupremeShare___Equipment_Borrowing_Tracker_System.Models;
using SupremeShare___Equipment_Borrowing_Tracker_System.Repositories.Interfaces;
using SupremeShare___Equipment_Borrowing_Tracker_System.Services;
using System.Security.Cryptography;

namespace SupremeShare___Equipment_Borrowing_Tracker_System.Controllers.Admin
{
    [Route("Admin/Users")]
    [AdminOnly]
    public class AdminUserController : Controller
    {
        private readonly IUserRepository _userRepo;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IAuditService _auditService;
        private readonly IDashboardNotificationService _notificationService;
        private readonly ILogger<AdminUserController> _logger;

        public AdminUserController(
            IUserRepository userRepo,
            IPasswordHasher passwordHasher,
            IAuditService auditService,
            IDashboardNotificationService notificationService,
            ILogger<AdminUserController> logger)
        {
            _userRepo = userRepo;
            _passwordHasher = passwordHasher;
            _auditService = auditService;
            _notificationService = notificationService;
            _logger = logger;
        }

        #region Views - Admin Management
        
        // GET: Admin/Users - Original user management (all users)
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var users = await _userRepo.GetAllAsync();
            return View("~/Views/Admin/Users/Index.cshtml", users);
        }

        // GET: Admin/Users/ManageAdmin - Active Admins only
        [HttpGet("ManageAdmin")]
        public async Task<IActionResult> ManageAdmin()
        {
            return View("~/Views/Admin/Users/ManageAdmin.cshtml");
        }

        // GET: Admin/Users/DeactivatedAdmin - Inactive Admins only
        [HttpGet("DeactivatedAdmin")]
        public async Task<IActionResult> DeactivatedAdmin()
        {
            return View("~/Views/Admin/Users/DeactivatedAdmin.cshtml");
        }

        // GET: Admin/Users/ManageSupportAdmin - Active Support Admins only
        [HttpGet("ManageSupportAdmin")]
        public async Task<IActionResult> ManageSupportAdmin()
        {
            return View("~/Views/Admin/Users/ManageSupportAdmin.cshtml");
        }

        // GET: Admin/Users/DeactivatedSupportAdmin - Inactive Support Admins only
        [HttpGet("DeactivatedSupportAdmin")]
        public async Task<IActionResult> DeactivatedSupportAdmin()
        {
            return View("~/Views/Admin/Users/DeactivatedSupportAdmin.cshtml");
        }

        #endregion

        #region API Endpoints - Admin Management

        // GET: Admin/Users/GetAll (API)
        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var users = await _userRepo.GetAllAsync();
                var result = users.Select(u => new
                {
                    u.UserId,
                    u.UserUsername,
                    u.UserRole,
                    u.UserPosition,
                    u.FullName,
                    u.UserEmail,
                    u.UserContactNo,
                    u.UserStatus,
                    UserDateCreated = u.UserDateCreated.ToString("yyyy-MM-dd HH:mm")
                });
                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching users");
                return Json(new { success = false, message = "Failed to fetch users" });
            }
        }

        // GET: Admin/Users/GetActiveAdmins - API for active admins
        [HttpGet("GetActiveAdmins")]
        public async Task<IActionResult> GetActiveAdmins()
        {
            try
            {
                var users = await _userRepo.GetActiveByRoleAsync("admin");
                var result = users.Select(u => new
                {
                    u.UserId,
                    u.UserUsername,
                    u.UserRole,
                    u.UserPosition,
                    u.FullName,
                    UserFirstName = u.UserFirstName,
                    UserLastName = u.UserLastName,
                    u.UserEmail,
                    u.UserContactNo,
                    u.UserStatus,
                    UserDateCreated = u.UserDateCreated.ToString("yyyy-MM-dd HH:mm")
                });
                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching active admins");
                return Json(new { success = false, message = "Failed to fetch active admins" });
            }
        }

        // GET: Admin/Users/GetInactiveAdmins - API for inactive/deactivated admins
        [HttpGet("GetInactiveAdmins")]
        public async Task<IActionResult> GetInactiveAdmins()
        {
            try
            {
                var users = await _userRepo.GetInactiveByRoleAsync("admin");
                var result = users.Select(u => new
                {
                    u.UserId,
                    u.UserUsername,
                    u.UserRole,
                    u.UserPosition,
                    u.FullName,
                    UserFirstName = u.UserFirstName,
                    UserLastName = u.UserLastName,
                    u.UserEmail,
                    u.UserContactNo,
                    u.UserStatus,
                    UserDateCreated = u.UserDateCreated.ToString("yyyy-MM-dd HH:mm")
                });
                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching inactive admins");
                return Json(new { success = false, message = "Failed to fetch inactive admins" });
            }
        }

        // GET: Admin/Users/GetActiveSupportAdmins - API for active support admins
        [HttpGet("GetActiveSupportAdmins")]
        public async Task<IActionResult> GetActiveSupportAdmins()
        {
            try
            {
                var users = await _userRepo.GetActiveByRoleAsync("support_admin");
                var result = users.Select(u => new
                {
                    u.UserId,
                    u.UserUsername,
                    u.UserRole,
                    u.UserPosition,
                    u.FullName,
                    UserFirstName = u.UserFirstName,
                    UserLastName = u.UserLastName,
                    u.UserEmail,
                    u.UserContactNo,
                    u.UserStatus,
                    UserDateCreated = u.UserDateCreated.ToString("yyyy-MM-dd HH:mm")
                });
                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching active support admins");
                return Json(new { success = false, message = "Failed to fetch active support admins" });
            }
        }

        // GET: Admin/Users/GetInactiveSupportAdmins - API for inactive support admins
        [HttpGet("GetInactiveSupportAdmins")]
        public async Task<IActionResult> GetInactiveSupportAdmins()
        {
            try
            {
                var users = await _userRepo.GetInactiveByRoleAsync("support_admin");
                var result = users.Select(u => new
                {
                    u.UserId,
                    u.UserUsername,
                    u.UserRole,
                    u.UserPosition,
                    u.FullName,
                    UserFirstName = u.UserFirstName,
                    UserLastName = u.UserLastName,
                    u.UserEmail,
                    u.UserContactNo,
                    u.UserStatus,
                    UserDateCreated = u.UserDateCreated.ToString("yyyy-MM-dd HH:mm")
                });
                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching inactive support admins");
                return Json(new { success = false, message = "Failed to fetch inactive support admins" });
            }
        }

        #endregion

        #region CRUD Operations

        // GET: Admin/Users/Get/{id}
        [HttpGet("Get/{id}")]
        public async Task<IActionResult> Get(int id)
        {
            try
            {
                var user = await _userRepo.GetByIdAsync(id);
                if (user == null)
                    return Json(new { success = false, message = "User not found" });

                return Json(new { success = true, data = user });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user {UserId}", id);
                return Json(new { success = false, message = "Failed to fetch user" });
            }
        }

        // POST: Admin/Users/Create - Original create (active user with password)
        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromForm] CreateUserRequest request)
        {
            try
            {
                // Validate
                if (string.IsNullOrWhiteSpace(request.Username))
                    return Json(new { success = false, message = "Username is required" });
                if (string.IsNullOrWhiteSpace(request.Password))
                    return Json(new { success = false, message = "Password is required" });
                if (request.Password.Length < 8)
                    return Json(new { success = false, message = "Password must be at least 8 characters" });

                // Check if username exists
                var existing = await _userRepo.GetByUsernameAsync(request.Username);
                if (existing != null)
                    return Json(new { success = false, message = "Username already exists" });

                var user = new User
                {
                    UserUsername = request.Username,
                    UserHashedPassword = _passwordHasher.HashPassword(request.Password),
                    UserRole = request.Role ?? "support_admin",
                    UserPosition = request.Position ?? "Support Staff",
                    UserFirstName = request.FirstName ?? "",
                    UserLastName = request.LastName ?? "",
                    UserMiddleName = request.MiddleName,
                    UserSuffix = request.Suffix,
                    UserEmail = request.Email,
                    UserContactNo = request.ContactNo,
                    UserStatus = true
                };

                var result = await _userRepo.AddAsync(user);
                if (result)
                {
                    var adminId = HttpContext.Session.GetInt32("UserId") ?? 0;
                    _logger.LogInformation("User created: {Username} by admin {AdminId}", request.Username, adminId);
                    
                    await _auditService.LogActionAsync(adminId, "USER_CREATED", null, new { request.Username, request.Role });
                    await _notificationService.NotifyUserUpdated(request.Username, "CREATED");
                    
                    return Json(new { success = true, message = "User created successfully" });
                }

                return Json(new { success = false, message = "Failed to create user" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Admin/Users/AddAdmin - Add admin as INACTIVE (no password yet)
        [HttpPost("AddAdmin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAdmin([FromForm] AddAdminRequest request)
        {
            try
            {
                // Validate required fields
                if (string.IsNullOrWhiteSpace(request.FirstName))
                    return Json(new { success = false, message = "First name is required" });
                if (string.IsNullOrWhiteSpace(request.LastName))
                    return Json(new { success = false, message = "Last name is required" });
                if (string.IsNullOrWhiteSpace(request.Email))
                    return Json(new { success = false, message = "Email is required" });
                if (string.IsNullOrWhiteSpace(request.Position))
                    return Json(new { success = false, message = "Position is required" });

                // Generate username from email (before @)
                var username = request.Email.Split('@')[0].ToLower().Replace(".", "").Replace("-", "").Replace("_", "");
                
                // Check if username already exists, append number if needed
                var baseUsername = username;
                var counter = 1;
                while (await _userRepo.GetByUsernameAsync(username) != null)
                {
                    username = $"{baseUsername}{counter}";
                    counter++;
                }

                // Check if email already exists
                var existingEmail = await _userRepo.GetByEmailAsync(request.Email);
                if (existingEmail != null)
                    return Json(new { success = false, message = "Email is already registered to another account" });

                // Create user as INACTIVE with placeholder password
                var user = new User
                {
                    UserUsername = username,
                    UserHashedPassword = _passwordHasher.HashPassword(Guid.NewGuid().ToString()), // Placeholder - will be replaced on activation
                    UserRole = request.Role ?? "admin",
                    UserPosition = request.Position,
                    UserFirstName = request.FirstName,
                    UserLastName = request.LastName,
                    UserMiddleName = request.MiddleName,
                    UserSuffix = request.Suffix,
                    UserEmail = request.Email,
                    UserContactNo = request.ContactNo,
                    UserStatus = false // INACTIVE - waiting for activation
                };

                var result = await _userRepo.AddAsync(user);
                if (result)
                {
                    var adminId = HttpContext.Session.GetInt32("UserId") ?? 0;
                    _logger.LogInformation("Admin added (pending activation): {Email} by admin {AdminId}", request.Email, adminId);
                    
                    await _auditService.LogActionAsync(adminId, "ADMIN_ADDED_PENDING", null, new { 
                        Username = username, 
                        request.Email, 
                        request.Role,
                        Status = "Pending Activation"
                    });
                    
                    return Json(new { 
                        success = true, 
                        message = "Admin added successfully", 
                        data = new {
                            fullName = $"{request.FirstName} {request.LastName}".Trim(),
                            position = request.Position,
                            email = request.Email,
                            username = username,
                            status = "Deactivated (Pending Activation)"
                        }
                    });
                }

                return Json(new { success = false, message = "Failed to add admin" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding admin");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Admin/Users/ActivateAdmin - Activate admin and generate password
        [HttpPost("ActivateAdmin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActivateAdmin([FromForm] int userId)
        {
            try
            {
                var user = await _userRepo.GetByIdAsync(userId);
                if (user == null)
                    return Json(new { success = false, message = "User not found" });

                if (user.UserStatus)
                    return Json(new { success = false, message = "User is already active" });

                // Generate secure random password
                var temporaryPassword = GenerateSecurePassword(12);
                var hashedPassword = _passwordHasher.HashPassword(temporaryPassword);

                var result = await _userRepo.ActivateUserAsync(userId, hashedPassword);
                
                if (result)
                {
                    var adminId = HttpContext.Session.GetInt32("UserId") ?? 0;
                    _logger.LogInformation("Admin activated: {UserId} by admin {AdminId}", userId, adminId);
                    
                    await _auditService.LogActionAsync(adminId, "ADMIN_ACTIVATED", 
                        new { user.UserUsername, OldStatus = "Inactive" }, 
                        new { user.UserUsername, NewStatus = "Active" });
                    await _notificationService.NotifyUserUpdated(user.UserUsername, "ACTIVATED");
                    
                    return Json(new { 
                        success = true, 
                        message = "Admin activated successfully",
                        data = new {
                            email = user.UserEmail,
                            temporaryPassword = temporaryPassword, // Show ONCE only
                            status = "Active - Ready to Login"
                        }
                    });
                }

                return Json(new { success = false, message = "Failed to activate admin" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating admin");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Admin/Users/DeactivateAdmin - Deactivate admin
        [HttpPost("DeactivateAdmin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactivateAdmin([FromForm] int userId)
        {
            try
            {
                var user = await _userRepo.GetByIdAsync(userId);
                if (user == null)
                    return Json(new { success = false, message = "User not found" });

                // Prevent deactivating own account
                var currentUserId = HttpContext.Session.GetInt32("UserId");
                if (user.UserId == currentUserId)
                    return Json(new { success = false, message = "Cannot deactivate your own account" });

                if (!user.UserStatus)
                    return Json(new { success = false, message = "User is already inactive" });

                var result = await _userRepo.DeactivateUserAsync(userId);
                
                if (result)
                {
                    var adminId = HttpContext.Session.GetInt32("UserId") ?? 0;
                    _logger.LogInformation("Admin deactivated: {UserId} by admin {AdminId}", userId, adminId);
                    
                    await _auditService.LogActionAsync(adminId, "ADMIN_DEACTIVATED", 
                        new { user.UserUsername, OldStatus = "Active" }, 
                        new { user.UserUsername, NewStatus = "Inactive" });
                    await _notificationService.NotifyUserUpdated(user.UserUsername, "DEACTIVATED");
                    
                    return Json(new { 
                        success = true, 
                        message = "Admin deactivated successfully. They can no longer log in."
                    });
                }

                return Json(new { success = false, message = "Failed to deactivate admin" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating admin");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Admin/Users/Activate - Generic activate for any user (from Index page)
        [HttpPost("Activate")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate([FromForm] int userId)
        {
            try
            {
                var user = await _userRepo.GetByIdAsync(userId);
                if (user == null)
                    return Json(new { success = false, message = "User not found" });

                if (user.UserStatus)
                    return Json(new { success = false, message = "User is already active" });

                // Generate secure random password
                var temporaryPassword = GenerateSecurePassword(12);
                var hashedPassword = _passwordHasher.HashPassword(temporaryPassword);

                var result = await _userRepo.ActivateUserAsync(userId, hashedPassword);
                
                if (result)
                {
                    var adminId = HttpContext.Session.GetInt32("UserId") ?? 0;
                    _logger.LogInformation("User activated: {UserId} by admin {AdminId}", userId, adminId);
                    
                    await _auditService.LogActionAsync(adminId, "USER_ACTIVATED", 
                        new { user.UserUsername, OldStatus = "Inactive" }, 
                        new { user.UserUsername, NewStatus = "Active" });
                    await _notificationService.NotifyUserUpdated(user.UserUsername, "ACTIVATED");
                    
                    return Json(new { success = true, message = "User activated successfully" });
                }

                return Json(new { success = false, message = "Failed to activate user" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating user");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Admin/Users/Deactivate - Generic deactivate for any user (from Index page)
        [HttpPost("Deactivate")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate([FromForm] int userId)
        {
            try
            {
                var user = await _userRepo.GetByIdAsync(userId);
                if (user == null)
                    return Json(new { success = false, message = "User not found" });

                // Prevent deactivating own account
                var currentUserId = HttpContext.Session.GetInt32("UserId");
                if (user.UserId == currentUserId)
                    return Json(new { success = false, message = "Cannot deactivate your own account" });

                if (!user.UserStatus)
                    return Json(new { success = false, message = "User is already inactive" });

                var result = await _userRepo.DeactivateUserAsync(userId);
                
                if (result)
                {
                    var adminId = HttpContext.Session.GetInt32("UserId") ?? 0;
                    _logger.LogInformation("User deactivated: {UserId} by admin {AdminId}", userId, adminId);
                    
                    await _auditService.LogActionAsync(adminId, "USER_DEACTIVATED", 
                        new { user.UserUsername, OldStatus = "Active" }, 
                        new { user.UserUsername, NewStatus = "Inactive" });
                    await _notificationService.NotifyUserUpdated(user.UserUsername, "DEACTIVATED");
                    
                    return Json(new { success = true, message = "User deactivated successfully" });
                }

                return Json(new { success = false, message = "Failed to deactivate user" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating user");
                return Json(new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Existing Methods (Update, ResetPassword, ToggleStatus, Delete)

        // POST: Admin/Users/Update
        [HttpPost("Update")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update([FromForm] UpdateUserRequest request)
        {
            try
            {
                var user = await _userRepo.GetByIdAsync(request.UserId);
                if (user == null)
                    return Json(new { success = false, message = "User not found" });

                // Prevent editing own account's role
                var currentUserId = HttpContext.Session.GetInt32("UserId");
                if (user.UserId == currentUserId && user.UserRole != request.Role)
                    return Json(new { success = false, message = "Cannot change your own role" });

                user.UserUsername = request.Username ?? user.UserUsername;
                user.UserRole = request.Role ?? user.UserRole;
                user.UserPosition = request.Position ?? user.UserPosition;
                user.UserFirstName = request.FirstName ?? user.UserFirstName;
                user.UserLastName = request.LastName ?? user.UserLastName;
                user.UserMiddleName = request.MiddleName;
                user.UserSuffix = request.Suffix;
                user.UserEmail = request.Email;
                user.UserContactNo = request.ContactNo;

                var result = await _userRepo.UpdateAsync(user);
                if (result)
                {
                    var adminId = HttpContext.Session.GetInt32("UserId") ?? 0;
                    await _auditService.LogActionAsync(adminId, "USER_UPDATED", null, new { user.UserUsername, user.UserRole });
                    await _notificationService.NotifyUserUpdated(user.UserUsername, "UPDATED");
                }
                return Json(new { success = result, message = result ? "User updated" : "Update failed" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Admin/Users/ResetPassword
        [HttpPost("ResetPassword")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword([FromForm] int userId, [FromForm] string newPassword)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
                    return Json(new { success = false, message = "Password must be at least 8 characters" });

                var user = await _userRepo.GetByIdAsync(userId);
                if (user == null)
                    return Json(new { success = false, message = "User not found" });

                user.UserHashedPassword = _passwordHasher.HashPassword(newPassword);
                var result = await _userRepo.UpdateAsync(user);

                var adminId = HttpContext.Session.GetInt32("UserId") ?? 0;
                _logger.LogWarning("Password reset for user {UserId} by admin {AdminId}", userId, adminId);

                if (result)
                {
                    await _auditService.LogActionAsync(adminId, "USER_PASSWORD_RESET", null, new { user.UserUsername });
                    await _notificationService.NotifyUserUpdated(user.UserUsername, "PASSWORD_RESET");
                }

                return Json(new { success = result, message = result ? "Password reset" : "Reset failed" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Admin/Users/ToggleStatus
        [HttpPost("ToggleStatus")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus([FromForm] int userId)
        {
            try
            {
                var user = await _userRepo.GetByIdAsync(userId);
                if (user == null)
                    return Json(new { success = false, message = "User not found" });

                // Prevent deactivating own account
                var currentUserId = HttpContext.Session.GetInt32("UserId");
                if (user.UserId == currentUserId)
                    return Json(new { success = false, message = "Cannot deactivate your own account" });

                user.UserStatus = !user.UserStatus;
                var result = await _userRepo.UpdateAsync(user);

                var action = user.UserStatus ? "activated" : "deactivated";
                _logger.LogInformation("User {UserId} {Action} by admin {AdminId}", userId, action, currentUserId);

                if (result)
                {
                    await _auditService.LogActionAsync(currentUserId ?? 0, $"USER_{action.ToUpper()}", null, new { user.UserUsername });
                    await _notificationService.NotifyUserUpdated(user.UserUsername, action.ToUpper());
                }

                return Json(new { success = result, message = $"User {action}", status = user.UserStatus });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling user status");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Admin/Users/Delete
        [HttpPost("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete([FromForm] int userId)
        {
            try
            {
                var currentUserId = HttpContext.Session.GetInt32("UserId");
                if (userId == currentUserId)
                    return Json(new { success = false, message = "Cannot delete your own account" });

                var result = await _userRepo.DeleteAsync(userId);
                return Json(new { success = result, message = result ? "User deactivated" : "Delete failed" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user");
                return Json(new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Generates a secure random password with specified length
        /// </summary>
        private static string GenerateSecurePassword(int length)
        {
            const string upperCase = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            const string lowerCase = "abcdefghjkmnpqrstuvwxyz";
            const string digits = "23456789";
            const string special = "!@#$%";
            
            var allChars = upperCase + lowerCase + digits + special;
            var password = new char[length];
            
            using var rng = RandomNumberGenerator.Create();
            var randomBytes = new byte[length];
            rng.GetBytes(randomBytes);
            
            // Ensure at least one of each type
            password[0] = upperCase[randomBytes[0] % upperCase.Length];
            password[1] = lowerCase[randomBytes[1] % lowerCase.Length];
            password[2] = digits[randomBytes[2] % digits.Length];
            
            // Fill the rest
            for (int i = 3; i < length; i++)
            {
                password[i] = allChars[randomBytes[i] % allChars.Length];
            }
            
            // Shuffle the password
            for (int i = password.Length - 1; i > 0; i--)
            {
                int j = randomBytes[i] % (i + 1);
                (password[i], password[j]) = (password[j], password[i]);
            }
            
            return new string(password);
        }

        #endregion
    }

    // Request DTOs
    public class CreateUserRequest
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string? Role { get; set; }
        public string? Position { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? MiddleName { get; set; }
        public string? Suffix { get; set; }
        public string? Email { get; set; }
        public string? ContactNo { get; set; }
    }

    public class UpdateUserRequest
    {
        public int UserId { get; set; }
        public string? Username { get; set; }
        public string? Role { get; set; }
        public string? Position { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? MiddleName { get; set; }
        public string? Suffix { get; set; }
        public string? Email { get; set; }
        public string? ContactNo { get; set; }
    }

    // New DTO for adding admin (inactive, without password)
    public class AddAdminRequest
    {
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string? MiddleName { get; set; }
        public string? Suffix { get; set; }
        public string Email { get; set; } = "";
        public string Position { get; set; } = "";
        public string? ContactNo { get; set; }
        public string? Role { get; set; } // admin or support_admin
    }
}
