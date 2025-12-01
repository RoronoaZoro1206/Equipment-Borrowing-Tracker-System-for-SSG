using SupremeShare___Equipment_Borrowing_Tracker_System.Data;
using SupremeShare___Equipment_Borrowing_Tracker_System.Middleware;
using SupremeShare___Equipment_Borrowing_Tracker_System.Repositories.Interfaces;
using SupremeShare___Equipment_Borrowing_Tracker_System.Repositories.Implementations;
using SupremeShare___Equipment_Borrowing_Tracker_System.Hubs;
using SupremeShare___Equipment_Borrowing_Tracker_System.Services;

// Load environment variables from .env file
DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

// ===========================================
// Database Connection Configuration
// ===========================================
var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING");

if (string.IsNullOrEmpty(connectionString))
{
    // Build connection string from individual env variables
    var host = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
    var port = Environment.GetEnvironmentVariable("DB_PORT") ?? "5433";
    var database = Environment.GetEnvironmentVariable("DB_NAME") ?? "supremeshare_db";
    var username = Environment.GetEnvironmentVariable("DB_USER") ?? "postgres";
    var password = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "!*Lawrence1206";
    
    connectionString = $"Host={host};Port={port};Database={database};Username={username};Password={password}";
}

// Add connection string to configuration
builder.Configuration["ConnectionStrings:DefaultConnection"] = connectionString;

// ===========================================
// Services Configuration
// ===========================================
builder.Services.AddControllersWithViews();

// Add SignalR for real-time updates
builder.Services.AddSignalR();

// Determine if we're in development
var isDevelopment = builder.Environment.IsDevelopment();

// Configure session with security settings
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = isDevelopment ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.Name = ".SupremeShare.Session";
});

// Configure anti-forgery
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.SecurePolicy = isDevelopment ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// ===========================================
// Register Database & Services
// ===========================================
builder.Services.AddSingleton<DatabaseHelper>();
builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IAuditService, AuditService>();

// ===========================================
// Register Repositories
// ===========================================
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IStudentRepository, StudentRepository>();
builder.Services.AddScoped<IEquipmentRepository, EquipmentRepository>();
builder.Services.AddScoped<IEquipmentUnitRepository, EquipmentUnitRepository>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<IBorrowRecordRepository, BorrowRecordRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();

// ===========================================
// Register SignalR Services
// ===========================================
builder.Services.AddScoped<IDashboardNotificationService, DashboardNotificationService>();

// ===========================================
// Register Advanced Feature Services (Phase 8)
// ===========================================
builder.Services.AddScoped<IBulkOperationsService, BulkOperationsService>();
builder.Services.AddScoped<IEquipmentConditionTrackingService, EquipmentConditionTrackingService>();
builder.Services.AddScoped<ILostItemProtocolService, LostItemProtocolService>();
builder.Services.AddScoped<IExportService, ExportService>();

// ===========================================
// Register Email Service
// ===========================================
builder.Services.AddScoped<IEmailService, EmailService>();

// Register Background Service for Auto-Overdue Updates
builder.Services.AddHostedService<OverdueStatusBackgroundService>();

// ===========================================
// Register Initializers & Migration Services
// ===========================================
builder.Services.AddScoped<AdminInitializer>();
builder.Services.AddScoped<SupportAdminInitializer>();
builder.Services.AddScoped<PasswordMigrationService>();

// ===========================================
// Logging Configuration
// ===========================================
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

// ===========================================
// Middleware Pipeline
// ===========================================
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// TEMPORARILY DISABLED FOR DEBUGGING - uncomment after fixing
// app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
// app.UseMiddleware<SecurityHeadersMiddleware>();
// app.UseMiddleware<RateLimitingMiddleware>();

// Only redirect to HTTPS in production
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseStaticFiles();

// Serve files from File_Images folder
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
        Path.Combine(builder.Environment.ContentRootPath, "File_Images")),
    RequestPath = "/File_Images"
});

app.UseRouting();

app.UseSession();
app.UseAuthorization();

// Minimal API test endpoint - bypasses all controllers
app.MapGet("/ping", () => "pong - server is running!");

// Email verification endpoint - bypasses AccountController
app.MapGet("/verify-email", async (string? token, DatabaseHelper db, ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("Verify email called with token: {Token}", token?.Substring(0, Math.Min(10, token?.Length ?? 0)));
        
        if (string.IsNullOrEmpty(token))
        {
            return Results.Content(GenerateVerificationHtml(false, "Invalid verification link.", null), "text/html");
        }

        var query = @"SELECT user_id, new_email, expires_at, is_used FROM email_verification_tokens WHERE token = @token";
        var dt = await db.ExecuteQueryAsync(query, new[] { new Npgsql.NpgsqlParameter("@token", token) });

        if (dt.Rows.Count == 0)
        {
            return Results.Content(GenerateVerificationHtml(false, "Invalid or expired verification link.", null), "text/html");
        }

        var row = dt.Rows[0];
        var userId = Convert.ToInt32(row["user_id"]);
        var newEmail = row["new_email"]?.ToString() ?? "";
        var isUsed = Convert.ToBoolean(row["is_used"]);
        var expiresAt = row["expires_at"] is DateTimeOffset dto ? dto.UtcDateTime : Convert.ToDateTime(row["expires_at"]);

        if (isUsed)
        {
            return Results.Content(GenerateVerificationHtml(false, "This verification link has already been used.", null), "text/html");
        }

        if (DateTime.UtcNow > expiresAt)
        {
            return Results.Content(GenerateVerificationHtml(false, "This verification link has expired.", null), "text/html");
        }

        // Update user email
        var updateQuery = @"UPDATE ""USER"" SET user_email = @email, user_last_updated = CURRENT_TIMESTAMP WHERE user_id = @userId";
        await db.ExecuteNonQueryAsync(updateQuery, new[] {
            new Npgsql.NpgsqlParameter("@email", newEmail),
            new Npgsql.NpgsqlParameter("@userId", userId)
        });

        // Mark token as used
        await db.ExecuteNonQueryAsync("UPDATE email_verification_tokens SET is_used = TRUE WHERE token = @token", 
            new[] { new Npgsql.NpgsqlParameter("@token", token) });

        logger.LogInformation("Email verified successfully for user {UserId}", userId);
        return Results.Content(GenerateVerificationHtml(true, "Your email has been verified successfully!", newEmail), "text/html");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error verifying email");
        return Results.Content(GenerateVerificationHtml(false, "An error occurred. Please try again.", null), "text/html");
    }
});

static string GenerateVerificationHtml(bool success, string message, string? email)
{
    var iconClass = success ? "icon-success" : "icon-error";
    var iconSymbol = success ? "&#10003;" : "&#10007;";
    var title = success ? "Email Verified!" : "Verification Failed";
    var emailBadge = !string.IsNullOrEmpty(email) ? $"<div class='email-badge'>&#9993; {System.Net.WebUtility.HtmlEncode(email)}</div>" : "";

    return $@"<!DOCTYPE html>
<html><head><meta charset='UTF-8'><meta name='viewport' content='width=device-width, initial-scale=1.0'><title>{title}</title>
<style>
body {{ background: linear-gradient(135deg, #f5f7fa 0%, #e4e8ec 100%); min-height: 100vh; display: flex; align-items: center; justify-content: center; font-family: 'Segoe UI', sans-serif; margin: 0; }}
.card {{ background: white; border-radius: 16px; box-shadow: 0 10px 40px rgba(0,0,0,0.1); padding: 50px 40px; text-align: center; max-width: 480px; margin: 20px; }}
.logo {{ color: #8B0000; font-size: 32px; font-weight: 700; margin-bottom: 30px; }}
.icon {{ width: 100px; height: 100px; border-radius: 50%; display: flex; align-items: center; justify-content: center; margin: 0 auto 25px; font-size: 50px; }}
.icon-success {{ background: linear-gradient(135deg, #d4edda 0%, #c3e6cb 100%); color: #28a745; }}
.icon-error {{ background: linear-gradient(135deg, #f8d7da 0%, #f5c6cb 100%); color: #dc3545; }}
h1 {{ color: #333; font-size: 28px; margin-bottom: 15px; }}
.message {{ color: #666; font-size: 16px; margin-bottom: 30px; }}
.email-badge {{ background: #f8f9fa; border: 1px solid #e9ecef; padding: 8px 16px; border-radius: 20px; font-size: 14px; margin-bottom: 25px; display: inline-block; }}
.btn {{ background: #8B0000; color: white; padding: 14px 35px; border-radius: 8px; text-decoration: none; display: inline-block; }}
.btn:hover {{ background: #6B0000; }}
</style></head>
<body><div class='card'>
<div class='logo'>&#128230; SupremeShare</div>
<div class='icon {iconClass}'>{iconSymbol}</div>
<h1>{title}</h1>
<p class='message'>{System.Net.WebUtility.HtmlEncode(message)}</p>
{emailBadge}
<a href='/Auth/Login' class='btn'>Go to Login</a>
</div></body></html>";
}

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Map SignalR Hub
app.MapHub<DashboardHub>("/hubs/dashboard");

// ===========================================
// Initialize Admin Accounts & Migrate Passwords
// ===========================================
try
{
    using var scope = app.Services.CreateScope();
    
    // Step 1: Migrate any existing plaintext passwords to BCrypt
    var passwordMigration = scope.ServiceProvider.GetRequiredService<PasswordMigrationService>();
    if (await passwordMigration.HasPlaintextPasswordsAsync())
    {
        app.Logger.LogWarning("Plaintext passwords detected! Running migration to BCrypt...");
        await passwordMigration.MigratePasswordsAsync();
    }

    // Step 2: Initialize admin accounts (will use BCrypt for new accounts)
    var adminInitializer = scope.ServiceProvider.GetRequiredService<AdminInitializer>();
    await adminInitializer.InitializeAsync();

    var supportAdminInitializer = scope.ServiceProvider.GetRequiredService<SupportAdminInitializer>();
    await supportAdminInitializer.InitializeAsync();
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "Error during admin initialization. App will continue without seeding admin accounts.");
}

// ===========================================
// Startup Message
// ===========================================
app.Logger.LogInformation("===========================================");
app.Logger.LogInformation("SupremeShare - Equipment Borrowing Tracker");
app.Logger.LogInformation("Database: {Database}", 
    Environment.GetEnvironmentVariable("DB_NAME") ?? "supremeshare_db");
app.Logger.LogInformation("Environment: {Environment}", 
    app.Environment.EnvironmentName);
app.Logger.LogInformation("===========================================");

app.Run();
