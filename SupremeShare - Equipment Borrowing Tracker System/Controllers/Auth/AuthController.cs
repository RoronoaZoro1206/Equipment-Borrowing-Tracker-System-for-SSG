using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc;
using SupremeShare___Equipment_Borrowing_Tracker_System.Data;
using SupremeShare___Equipment_Borrowing_Tracker_System.Repositories.Interfaces;

namespace SupremeShare___Equipment_Borrowing_Tracker_System.Controllers.Auth
{
    public class AuthController : Controller
    {
        private readonly ILogger<AuthController> _logger;
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IAuditService _auditService;

        // Rate limiting: Track failed login attempts per IP
        private static readonly ConcurrentDictionary<string, (int Count, DateTime LastAttempt)> _failedAttempts = new();
        
        // Security settings - reduced from 10 to 5 for stricter security
        private const int MaxFailedAttempts = 5;
        private const int LockoutSeconds = 20;

        public AuthController(
            ILogger<AuthController> logger,
            IUserRepository userRepository,
            IPasswordHasher passwordHasher,
            IAuditService auditService)
        {
            _logger = logger;
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _auditService = auditService;
        }

        [HttpGet]
        public IActionResult Login()
        {
            // If already logged in, redirect to dashboard
            if (HttpContext.Session.GetInt32("UserId") != null)
            {
                return RedirectToAction("Index", "Dashboard");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password)
        {
            // Get client IP for rate limiting and audit logging
            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            // Check if IP is locked out due to too many failed attempts
            if (IsLockedOut(clientIp, out int remainingSeconds))
            {
                _logger.LogWarning("Login blocked for IP {IP} due to rate limiting", clientIp);
                
                // Log the lockout event (security monitoring)
                await LogSecurityEventAsync("LOGIN_BLOCKED_RATE_LIMIT", null, clientIp, 
                    $"IP blocked after {MaxFailedAttempts} failed attempts");
                
                ViewBag.LockoutSeconds = remainingSeconds;
                return View();
            }

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Username and password are required.";
                return View();
            }

            try
            {
                // Validate username format - prevents SQL injection patterns
                InputValidator.ValidateUsername(username);
            }
            catch (Exception ex)
            {
                RecordFailedAttempt(clientIp);
                
                // Log potential SQL injection attempt - HIGH PRIORITY SECURITY EVENT
                await LogSecurityEventAsync("LOGIN_FAILED_SQL_INJECTION", username, clientIp, 
                    $"Suspicious input detected: {ex.Message}");
                
                _logger.LogWarning("Potential SQL injection attempt from IP {IP} with username pattern", clientIp);
                ViewBag.Error = "Invalid username format.";
                return View();
            }

            try
            {
                var user = await _userRepository.GetByUsernameAsync(username);

                if (user == null)
                {
                    // User not found - log failed attempt
                    RecordFailedAttempt(clientIp);
                    await LogFailedLoginAsync(username, clientIp, "USER_NOT_FOUND");
                    
                    _logger.LogWarning("Failed login attempt for non-existent username: {Username} from IP: {IP}", username, clientIp);
                    ViewBag.Error = "Invalid username or password.";
                    return View();
                }

                if (!_passwordHasher.VerifyPassword(password, user.UserHashedPassword))
                {
                    // Wrong password - log failed attempt with user ID for tracking
                    RecordFailedAttempt(clientIp);
                    await LogFailedLoginAsync(username, clientIp, "INVALID_PASSWORD", user.UserId);
                    
                    _logger.LogWarning("Failed login attempt (wrong password) for username: {Username} from IP: {IP}", username, clientIp);
                    ViewBag.Error = "Invalid username or password.";
                    return View();
                }

                if (!user.UserStatus)
                {
                    // Account deactivated - log attempt
                    await LogFailedLoginAsync(username, clientIp, "ACCOUNT_DEACTIVATED", user.UserId);
                    
                    ViewBag.Error = "Your account has been deactivated. Please contact an administrator.";
                    return View();
                }

                // Successful login - clear failed attempts for this IP
                ClearFailedAttempts(clientIp);

                // Set session
                HttpContext.Session.SetInt32("UserId", user.UserId);
                HttpContext.Session.SetString("Username", user.UserUsername);
                HttpContext.Session.SetString("FullName", user.FullName);
                HttpContext.Session.SetString("UserRole", user.UserRole);

                // Log successful login
                await _auditService.LogActionAsync(user.UserId, "LOGIN", null, 
                    new { Username = user.UserUsername, IP = clientIp });

                _logger.LogInformation("User {Username} logged in successfully from IP: {IP}", username, clientIp);

                return RedirectToAction("Index", "Dashboard");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for username: {Username}", username);
                ViewBag.Error = "An error occurred during login. Please try again.";
                return View();
            }
        }

        #region Security Logging Helpers

        /// <summary>
        /// Logs failed login attempts to audit log for security monitoring.
        /// Uses distinct action types to allow filtering in audit log view.
        /// </summary>
        private async Task LogFailedLoginAsync(string attemptedUsername, string clientIp, string reason, int? userId = null)
        {
            try
            {
                // Get current failed attempt count for this IP
                var attemptCount = _failedAttempts.TryGetValue(clientIp, out var attempt) ? attempt.Count : 1;
                
                await _auditService.LogActionAsync(
                    userId ?? 0, // Use 0 for system/unknown user
                    "LOGIN_FAILED",
                    null,
                    new
                    {
                        AttemptedUsername = attemptedUsername,
                        IP = clientIp,
                        Reason = reason,
                        AttemptNumber = attemptCount,
                        MaxAttempts = MaxFailedAttempts,
                        Timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
                    }
                );
            }
            catch (Exception ex)
            {
                // Don't let audit logging failure break login flow
                _logger.LogError(ex, "Failed to log failed login attempt for {Username}", attemptedUsername);
            }
        }

        /// <summary>
        /// Logs high-priority security events (SQL injection, rate limiting, etc.)
        /// </summary>
        private async Task LogSecurityEventAsync(string action, string? attemptedUsername, string clientIp, string details)
        {
            try
            {
                await _auditService.LogActionAsync(
                    0, // System user for security events
                    action,
                    null,
                    new
                    {
                        AttemptedUsername = attemptedUsername ?? "N/A",
                        IP = clientIp,
                        Details = details,
                        Severity = action.Contains("SQL_INJECTION") ? "HIGH" : "MEDIUM",
                        Timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log security event: {Action}", action);
            }
        }

        #endregion

        #region Rate Limiting Helpers

        private bool IsLockedOut(string clientIp, out int remainingSeconds)
        {
            remainingSeconds = 0;
            if (_failedAttempts.TryGetValue(clientIp, out var attempt))
            {
                // Check if lockout period has expired
                if (attempt.Count >= MaxFailedAttempts)
                {
                    var lockoutEnd = attempt.LastAttempt.AddSeconds(LockoutSeconds);
                    if (DateTime.UtcNow < lockoutEnd)
                    {
                        remainingSeconds = (int)Math.Ceiling((lockoutEnd - DateTime.UtcNow).TotalSeconds);
                        return true;
                    }
                    // Lockout expired, clear the record
                    _failedAttempts.TryRemove(clientIp, out _);
                }
            }
            return false;
        }

        private void RecordFailedAttempt(string clientIp)
        {
            _failedAttempts.AddOrUpdate(
                clientIp,
                (1, DateTime.UtcNow),
                (key, existing) => (existing.Count + 1, DateTime.UtcNow)
            );
        }

        private void ClearFailedAttempts(string clientIp)
        {
            _failedAttempts.TryRemove(clientIp, out _);
        }

        #endregion

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var username = HttpContext.Session.GetString("Username");
            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            if (userId.HasValue)
            {
                await _auditService.LogActionAsync(userId.Value, "LOGOUT", null, 
                    new { Username = username, IP = clientIp });
            }

            _logger.LogInformation("User {Username} logged out", username ?? "Unknown");

            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}
