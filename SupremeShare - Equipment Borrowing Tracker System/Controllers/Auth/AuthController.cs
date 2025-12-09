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
        private const int MaxFailedAttempts = 10;
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
            // Get client IP for rate limiting
            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            // Check if IP is locked out due to too many failed attempts
            if (IsLockedOut(clientIp, out int remainingSeconds))
            {
                _logger.LogWarning("Login blocked for IP {IP} due to rate limiting", clientIp);
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
            catch
            {
                RecordFailedAttempt(clientIp);
                ViewBag.Error = "Invalid username format.";
                return View();
            }

            try
            {
                var user = await _userRepository.GetByUsernameAsync(username);

                if (user == null || !_passwordHasher.VerifyPassword(password, user.UserHashedPassword))
                {
                    RecordFailedAttempt(clientIp);
                    _logger.LogWarning("Failed login attempt for username: {Username} from IP: {IP}", username, clientIp);
                    ViewBag.Error = "Invalid username or password.";
                    return View();
                }

                if (!user.UserStatus)
                {
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

                // Log the login
                await _auditService.LogActionAsync(user.UserId, "LOGIN", null, new { Username = user.UserUsername, IP = clientIp });

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

            if (userId.HasValue)
            {
                await _auditService.LogActionAsync(userId.Value, "LOGOUT", null, new { Username = username });
            }

            _logger.LogInformation("User {Username} logged out", username ?? "Unknown");

            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}
