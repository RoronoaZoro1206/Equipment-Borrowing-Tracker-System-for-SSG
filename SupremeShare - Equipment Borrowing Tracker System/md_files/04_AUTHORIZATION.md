# Authorization (Role-Based Access Control)

**File Location:** `Attributes/AuthorizeRoleAttribute.cs`

---

## What is Authorization?

Authorization controls WHO can access WHAT in your application.

**Without authorization:** Any logged-in user can access admin pages and delete data.

**With authorization:** Only users with the correct role can access protected pages.

---

## Roles in This System

| Role | Access Level |
|------|--------------|
| `admin` | Full access - can manage users, equipment, view audit logs |
| `support_admin` | Limited access - can manage students, transactions, borrow records |

---

## How It Works

### Step 1: User logs in
```
Username: john_doe
Role: support_admin (stored in session)
```

### Step 2: User tries to access a page
```
Page: /Admin/ManageUsers
Required Role: admin
```

### Step 3: System checks authorization
```
User Role: support_admin
Required: admin
support_admin ≠ admin → ACCESS DENIED
```

---

## The Attributes

### 1. AuthorizeRoleAttribute (Base)

Checks if user has any of the specified roles.

```csharp
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class AuthorizeRoleAttribute : Attribute, IAuthorizationFilter
{
    private readonly string[] _roles;

    public AuthorizeRoleAttribute(params string[] roles)
    {
        _roles = roles;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        // Step 1: Get user info from session
        var userRole = context.HttpContext.Session.GetString("UserRole");
        var userId = context.HttpContext.Session.GetInt32("UserId");

        // Step 2: Check if logged in
        if (userId == null || string.IsNullOrEmpty(userRole))
        {
            // Not logged in → Redirect to login page
            context.Result = new RedirectToActionResult("Login", "Auth", null);
            return;
        }

        // Step 3: Check if user has required role
        if (_roles.Length > 0 && !Array.Exists(_roles, r => 
            r.Equals(userRole, StringComparison.OrdinalIgnoreCase)))
        {
            // Wrong role → Show forbidden page
            context.Result = new ForbidResult();
            return;
        }
        
        // Step 4: User authorized → Continue to page
    }
}
```

---

### 2. AdminOnlyAttribute

Shortcut for admin-only pages.

```csharp
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class AdminOnlyAttribute : AuthorizeRoleAttribute
{
    public AdminOnlyAttribute() : base("admin") { }
}
```

---

### 3. SupportAdminAttribute

Allows both admin and support_admin roles.

```csharp
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class SupportAdminAttribute : AuthorizeRoleAttribute
{
    public SupportAdminAttribute() : base("admin", "support_admin") { }
}
```

---

## Usage Examples

### Protect an Entire Controller

```csharp
// Only admins can access any action in this controller
[AdminOnly]
public class AdminController : Controller
{
    public IActionResult ManageUsers() { }
    public IActionResult ViewAuditLogs() { }
    public IActionResult SystemSettings() { }
}
```

### Protect a Single Action

```csharp
public class EquipmentController : Controller
{
    // Anyone logged in can view
    [SupportAdmin]
    public IActionResult Index() { }
    
    // Only admins can delete
    [AdminOnly]
    public IActionResult Delete(int id) { }
}
```

### Allow Multiple Roles

```csharp
// Both admin and support_admin can access
[AuthorizeRole("admin", "support_admin")]
public IActionResult BorrowEquipment() { }
```

---

## Setting Up Session (Login)

When user logs in, store their info in session:

```csharp
// In AuthController.cs
public async Task<IActionResult> Login(string username, string password)
{
    var user = await _userRepository.GetByUsernameAsync(username);
    
    if (user != null && _passwordHasher.VerifyPassword(password, user.UserHashedPassword))
    {
        // Store user info in session
        HttpContext.Session.SetInt32("UserId", user.UserId);
        HttpContext.Session.SetString("UserRole", user.UserRole);
        HttpContext.Session.SetString("UserName", user.UserUsername);
        
        return RedirectToAction("Index", "Home");
    }
    
    return View("Login", "Invalid credentials");
}
```

---

## Session Configuration in Program.cs

```csharp
// Configure session with security settings
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);  // Auto logout after 30 min
    options.Cookie.HttpOnly = true;                   // Prevent JavaScript access
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;  // HTTPS only
    options.Cookie.SameSite = SameSiteMode.Strict;   // Prevent CSRF
});

// In middleware pipeline
app.UseSession();
```

---

## Access Control Matrix

| Page/Action | Admin | Support Admin | Not Logged In |
|-------------|-------|---------------|---------------|
| View Equipment | ✓ | ✓ | ✗ → Login |
| Add Equipment | ✓ | ✓ | ✗ → Login |
| Delete Equipment | ✓ | ✗ → Forbidden | ✗ → Login |
| Manage Users | ✓ | ✗ → Forbidden | ✗ → Login |
| View Audit Logs | ✓ | ✗ → Forbidden | ✗ → Login |
| Borrow Transaction | ✓ | ✓ | ✗ → Login |

---

## Logout

```csharp
public IActionResult Logout()
{
    // Clear all session data
    HttpContext.Session.Clear();
    
    return RedirectToAction("Login", "Auth");
}
```
