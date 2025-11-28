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
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Username and password are required.";
                return View();
            }

            try
            {
                // Validate username format
                InputValidator.ValidateUsername(username);
            }
            catch
            {
                ViewBag.Error = "Invalid username format.";
                return View();
            }

            try
            {
                var user = await _userRepository.GetByUsernameAsync(username);

                if (user == null || !_passwordHasher.VerifyPassword(password, user.UserHashedPassword))
                {
                    _logger.LogWarning("Failed login attempt for username: {Username}", username);
                    ViewBag.Error = "Invalid username or password.";
                    return View();
                }

                if (!user.UserStatus)
                {
                    ViewBag.Error = "Your account has been deactivated. Please contact an administrator.";
                    return View();
                }

                // Set session
                HttpContext.Session.SetInt32("UserId", user.UserId);
                HttpContext.Session.SetString("Username", user.UserUsername);
                HttpContext.Session.SetString("FullName", user.FullName);
                HttpContext.Session.SetString("UserRole", user.UserRole);

                // Log the login
                await _auditService.LogActionAsync(user.UserId, "LOGIN", null, new { Username = user.UserUsername });

                _logger.LogInformation("User {Username} logged in successfully", username);

                return RedirectToAction("Index", "Dashboard");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for username: {Username}", username);
                ViewBag.Error = "An error occurred during login. Please try again.";
                return View();
            }
        }

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
