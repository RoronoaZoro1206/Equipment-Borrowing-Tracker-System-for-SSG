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
    app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
}

// Security middleware (order matters!)
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<RateLimitingMiddleware>();

app.UseHttpsRedirection();
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
