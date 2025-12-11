# Security Overview - Equipment Borrowing Tracker System

This document provides an overview of all security features implemented in the system.

---

## What is Security in Web Applications?

Security protects your application from hackers who try to:
- Steal user data (passwords, personal info)
- Delete or modify database records
- Crash your system
- Access pages they shouldn't see

---

## Security Layers in This System

| Layer | File Location | Purpose |
|-------|---------------|---------|
| Input Validation | `Data/InputValidator.cs` | Blocks bad data before it reaches the database |
| Password Hashing | `Data/PasswordHasher.cs` | Protects passwords using BCrypt |
| Rate Limiting | `Middleware/RateLimitingMiddleware.cs` | Stops brute force attacks |
| Login Rate Limiting | `Controllers/Auth/AuthController.cs` | 5 attempts max, 20s lockout |
| Authorization | `Attributes/AuthorizeRoleAttribute.cs` | Controls who can access what |
| Exception Handling | `Middleware/GlobalExceptionHandlerMiddleware.cs` | Hides error details from hackers |
| Audit Logging | `Data/AuditService.cs` | Tracks all user actions |
| Failed Login Logging | `Controllers/Auth/AuthController.cs` | Records failed login attempts |
| Security Headers | `Middleware/SecurityHeadersMiddleware.cs` | Protects against browser attacks |

---

## How They Work Together

```
User Request
     ↓
[Rate Limiting] → Too many requests? Block them!
     ↓
[Security Headers] → Add protection headers
     ↓
[Login Rate Limiting] → 5 failed attempts? Lock for 20 seconds!
     ↓
[Authorization] → Is user allowed here?
     ↓
[Input Validation] → Is the data safe? SQL injection detected?
     ↓
[Password Hashing] → Hash passwords before saving
     ↓
[Database Operation]
     ↓
[Audit Logging] → Record what happened (including failed logins)
     ↓
[Exception Handling] → If error, show safe message
     ↓
Response to User
```

---

## Login Security Settings

| Setting | Value | Description |
|---------|-------|-------------|
| Max Failed Attempts | 5 | Lock account after 5 wrong passwords |
| Lockout Duration | 20 seconds | Time before retry allowed |
| SQL Injection Detection | Yes | Logs suspicious login patterns |

---

## Audit Log Action Types

| Action | Description | Logged Data |
|--------|-------------|-------------|
| `LOGIN` | Successful login | Username, IP |
| `LOGOUT` | User logout | Username, IP |
| `LOGIN_FAILED` | Failed login attempt | Username, IP, Reason, Attempt # |
| `LOGIN_FAILED_SQL_INJECTION` | SQL injection detected | Username pattern, IP, Details |
| `LOGIN_BLOCKED_RATE_LIMIT` | IP blocked after max attempts | IP, Details |

---

## Quick Reference

### To hash a password:
```csharp
var hashedPassword = _passwordHasher.HashPassword("userPassword123");
```

### To validate input:
```csharp
InputValidator.ValidateEmail(email);
InputValidator.ValidateUsername(username);
```

### To protect a controller:
```csharp
[AdminOnly]
public IActionResult AdminDashboard() { }
```

### To log an action:
```csharp
await _auditService.LogActionAsync(userId, "USER_CREATED", null, newUserData);
```

---

## Related Documentation

1. [Input Validation](01_INPUT_VALIDATION.md)
2. [Password Security](02_PASSWORD_SECURITY.md)
3. [Rate Limiting](03_RATE_LIMITING.md)
4. [Authorization](04_AUTHORIZATION.md)
5. [Exception Handling](05_EXCEPTION_HANDLING.md)
6. [Audit Logging](06_AUDIT_LOGGING.md)
