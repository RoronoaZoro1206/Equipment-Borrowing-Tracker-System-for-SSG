using Microsoft.AspNetCore.Mvc;
using SupremeShare___Equipment_Borrowing_Tracker_System.Attributes;
using SupremeShare___Equipment_Borrowing_Tracker_System.Data;
using SupremeShare___Equipment_Borrowing_Tracker_System.Models;
using SupremeShare___Equipment_Borrowing_Tracker_System.Repositories.Interfaces;

namespace SupremeShare___Equipment_Borrowing_Tracker_System.Controllers.Admin
{
    [Route("Admin/Users")]
    [AdminOnly]
    public class AdminUserController : Controller
    {
        private readonly IUserRepository _userRepo;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ILogger<AdminUserController> _logger;

        public AdminUserController(
            IUserRepository userRepo,
            IPasswordHasher passwordHasher,
            ILogger<AdminUserController> logger)
        {
            _userRepo = userRepo;
            _passwordHasher = passwordHasher;
            _logger = logger;
        }

        // GET: Admin/Users
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var users = await _userRepo.GetAllAsync();
            return View("~/Views/Admin/Users/Index.cshtml", users);
        }

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

        // POST: Admin/Users/Create
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
                    _logger.LogInformation("User created: {Username} by admin {AdminId}", 
                        request.Username, HttpContext.Session.GetInt32("UserId"));
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

                _logger.LogWarning("Password reset for user {UserId} by admin {AdminId}", 
                    userId, HttpContext.Session.GetInt32("UserId"));

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
                _logger.LogInformation("User {UserId} {Action} by admin {AdminId}", 
                    userId, action, currentUserId);

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
}
