using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace SupremeShare___Equipment_Borrowing_Tracker_System.Data
{
    public class SupportAdminInitializer
    {
        private readonly DatabaseHelper _db;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ILogger<SupportAdminInitializer> _logger;

        public SupportAdminInitializer(
            DatabaseHelper db,
            IPasswordHasher passwordHasher,
            ILogger<SupportAdminInitializer> logger)
        {
            _db = db;
            _passwordHasher = passwordHasher;
            _logger = logger;
        }

        public async Task InitializeAsync()
        {
            try
            {
                _logger.LogInformation("Checking for support admin account...");

                // Check if support admin user already exists
                var supportAdminExists = await SupportAdminExistsAsync();
                if (supportAdminExists)
                {
                    _logger.LogInformation("Support Admin user(s) already exist. Skipping creation.");
                    return;
                }

                _logger.LogInformation("No support admin users found. Creating support admin account...");

                // Read support admin credentials from environment variables
                var supportUsername = Environment.GetEnvironmentVariable("SUPPORT_ADMIN_USERNAME") ?? "support_admin";
                var supportPassword = Environment.GetEnvironmentVariable("SUPPORT_ADMIN_PASSWORD") ?? "Support@123";
                var supportFirstName = Environment.GetEnvironmentVariable("SUPPORT_ADMIN_FIRST_NAME") ?? "Support";
                var supportLastName = Environment.GetEnvironmentVariable("SUPPORT_ADMIN_LAST_NAME") ?? "Administrator";
                var supportEmail = Environment.GetEnvironmentVariable("SUPPORT_ADMIN_EMAIL") ?? "support@supremeshare.com";
                var supportPosition = Environment.GetEnvironmentVariable("SUPPORT_ADMIN_POSITION") ?? "Support Administrator";

                // Check if username already exists
                var usernameExists = await UsernameExistsAsync(supportUsername);
                if (usernameExists)
                {
                    _logger.LogWarning("User with username '{Username}' already exists. Skipping support admin creation.", supportUsername);
                    return;
                }

                // Hash the password
                var hashedPassword = _passwordHasher.HashPassword(supportPassword);

                // Insert directly into database - note lowercase column names for PostgreSQL
                var query = @"INSERT INTO ""USER"" 
                    (user_username, user_hashed_password, user_role, user_position, 
                     user_last_name, user_first_name, user_email, user_status)
                    VALUES (@username, @password, 'support_admin'::user_role_enum, @position, 
                            @lastName, @firstName, @email, true)
                    RETURNING user_id";

                var parameters = new NpgsqlParameter[]
                {
                    new("@username", supportUsername),
                    new("@password", hashedPassword),
                    new("@position", supportPosition),
                    new("@lastName", supportLastName),
                    new("@firstName", supportFirstName),
                    new("@email", supportEmail)
                };

                var result = await _db.ExecuteScalarAsync(query, parameters);
                var userId = Convert.ToInt32(result);

                if (userId > 0)
                {
                    // Log to audit table with the actual user_id
                    await LogAuditAsync(userId, "SUPPORT_ADMIN_INITIALIZED", supportUsername);

                    _logger.LogInformation("═══════════════════════════════════════════");
                    _logger.LogInformation("   Support Admin Account Created!");
                    _logger.LogInformation("═══════════════════════════════════════════");
                    _logger.LogInformation("   Username:  {Username}", supportUsername);
                    _logger.LogInformation("   Password:  {Password}", supportPassword);
                    _logger.LogInformation("   Role:      support_admin");
                    _logger.LogInformation("   Position:  {Position}", supportPosition);
                    _logger.LogInformation("═══════════════════════════════════════════");
                    _logger.LogWarning("⚠  IMPORTANT: Change the password after first login!");
                    _logger.LogInformation("═══════════════════════════════════════════");
                }
                else
                {
                    _logger.LogError("Failed to create support admin user account.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing support admin account: {Message}", ex.Message);
            }
        }

        private async Task<bool> SupportAdminExistsAsync()
        {
            try
            {
                var query = @"SELECT COUNT(1) FROM ""USER"" WHERE user_role = 'support_admin' AND user_status = true";
                var result = await _db.ExecuteScalarAsync(query);
                return Convert.ToInt32(result) > 0;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error checking if support admin exists, assuming none exists");
                return false;
            }
        }

        private async Task<bool> UsernameExistsAsync(string username)
        {
            try
            {
                var query = @"SELECT COUNT(1) FROM ""USER"" WHERE user_username = @username";
                var parameters = new NpgsqlParameter[] { new("@username", username) };
                var result = await _db.ExecuteScalarAsync(query, parameters);
                return Convert.ToInt32(result) > 0;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error checking if username exists");
                return false;
            }
        }

        private async Task LogAuditAsync(int userId, string action, string username)
        {
            try
            {
                var query = @"INSERT INTO audit_log (user_id, audit_action, audit_new_data)
                              VALUES (@userId, @action, @data)";
                var parameters = new NpgsqlParameter[]
                {
                    new("@userId", userId),
                    new("@action", action),
                    new("@data", $"{{\"username\":\"{username}\",\"role\":\"support_admin\",\"created_at\":\"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\"}}")
                };
                await _db.ExecuteNonQueryAsync(query, parameters);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to log audit for support admin initialization (non-critical)");
            }
        }
    }
}
