using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace SupremeShare___Equipment_Borrowing_Tracker_System.Data
{
    public class AdminInitializer
    {
        private readonly DatabaseHelper _db;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ILogger<AdminInitializer> _logger;

        public AdminInitializer(
            DatabaseHelper db,
            IPasswordHasher passwordHasher,
            ILogger<AdminInitializer> logger)
        {
            _db = db;
            _passwordHasher = passwordHasher;
            _logger = logger;
        }

        public async Task InitializeAsync()
        {
            try
            {
                _logger.LogInformation("Checking for admin account...");

                // Check if admin user already exists
                var adminExists = await AdminExistsAsync();
                if (adminExists)
                {
                    _logger.LogInformation("Admin user(s) already exist. Skipping admin creation.");
                    return;
                }

                _logger.LogInformation("No admin users found. Creating admin account...");

                // Read admin credentials from environment variables
                var adminUsername = Environment.GetEnvironmentVariable("ADMIN_USERNAME") ?? "admin";
                var adminPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD") ?? "Admin@123";
                var adminFirstName = Environment.GetEnvironmentVariable("ADMIN_FIRST_NAME") ?? "System";
                var adminLastName = Environment.GetEnvironmentVariable("ADMIN_LAST_NAME") ?? "Administrator";
                var adminEmail = Environment.GetEnvironmentVariable("ADMIN_EMAIL") ?? "admin@supremeshare.com";
                var adminPosition = Environment.GetEnvironmentVariable("ADMIN_POSITION") ?? "System Administrator";

                // Check if username already exists
                var usernameExists = await UsernameExistsAsync(adminUsername);
                if (usernameExists)
                {
                    _logger.LogWarning("User with username '{Username}' already exists. Skipping admin creation.", adminUsername);
                    return;
                }

                // Hash the password
                var hashedPassword = _passwordHasher.HashPassword(adminPassword);

                // Insert directly into database - note lowercase column names for PostgreSQL
                var query = @"INSERT INTO ""USER"" 
                    (user_username, user_hashed_password, user_role, user_position, 
                     user_last_name, user_first_name, user_email, user_status)
                    VALUES (@username, @password, 'admin'::user_role_enum, @position, 
                            @lastName, @firstName, @email, true)
                    RETURNING user_id";

                var parameters = new NpgsqlParameter[]
                {
                    new("@username", adminUsername),
                    new("@password", hashedPassword),
                    new("@position", adminPosition),
                    new("@lastName", adminLastName),
                    new("@firstName", adminFirstName),
                    new("@email", adminEmail)
                };

                var result = await _db.ExecuteScalarAsync(query, parameters);
                var userId = Convert.ToInt32(result);

                if (userId > 0)
                {
                    // Log to audit table with the actual user_id
                    await LogAuditAsync(userId, "ADMIN_INITIALIZED", adminUsername);

                    _logger.LogInformation("═══════════════════════════════════════════");
                    _logger.LogInformation("   Admin Account Created Successfully!");
                    _logger.LogInformation("═══════════════════════════════════════════");
                    _logger.LogInformation("   Username:  {Username}", adminUsername);
                    _logger.LogInformation("   Password:  {Password}", adminPassword);
                    _logger.LogInformation("   Role:      admin");
                    _logger.LogInformation("   Position:  {Position}", adminPosition);
                    _logger.LogInformation("═══════════════════════════════════════════");
                    _logger.LogWarning("⚠  IMPORTANT: Change the password after first login!");
                    _logger.LogInformation("═══════════════════════════════════════════");
                }
                else
                {
                    _logger.LogError("Failed to create admin user account.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing admin account: {Message}", ex.Message);
            }
        }

        private async Task<bool> AdminExistsAsync()
        {
            try
            {
                var query = @"SELECT COUNT(1) FROM ""USER"" WHERE user_role = 'admin' AND user_status = true";
                var result = await _db.ExecuteScalarAsync(query);
                return Convert.ToInt32(result) > 0;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error checking if admin exists, assuming no admin exists");
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
                    new("@data", $"{{\"username\":\"{username}\",\"role\":\"admin\",\"created_at\":\"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\"}}")
                };
                await _db.ExecuteNonQueryAsync(query, parameters);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to log audit for admin initialization (non-critical)");
            }
        }
    }
}
