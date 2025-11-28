using SupremeShare___Equipment_Borrowing_Tracker_System.Data;
using SupremeShare___Equipment_Borrowing_Tracker_System.Middleware;
using SupremeShare___Equipment_Borrowing_Tracker_System.Repositories.Interfaces;
using SupremeShare___Equipment_Borrowing_Tracker_System.Repositories.Implementations;

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
    var port = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";
    var database = Environment.GetEnvironmentVariable("DB_NAME") ?? "supremeshare_db";
    var username = Environment.GetEnvironmentVariable("DB_USER") ?? "postgres";
    var password = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "";
    
    connectionString = $"Host={host};Port={port};Database={database};Username={username};Password={password}";
}

// Add connection string to configuration
builder.Configuration["ConnectionStrings:DefaultConnection"] = connectionString;

// ===========================================
// Services Configuration
// ===========================================
builder.Services.AddControllersWithViews();

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

app.UseRouting();

app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

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
