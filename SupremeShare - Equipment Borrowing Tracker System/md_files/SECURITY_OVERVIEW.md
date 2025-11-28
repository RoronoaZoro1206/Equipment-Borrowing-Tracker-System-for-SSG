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
| Authorization | `Attributes/AuthorizeRoleAttribute.cs` | Controls who can access what |
| Exception Handling | `Middleware/GlobalExceptionHandlerMiddleware.cs` | Hides error details from hackers |
| Audit Logging | `Data/AuditService.cs` | Tracks all user actions |
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
[Authorization] → Is user allowed here?
     ↓
[Input Validation] → Is the data safe?
     ↓
[Password Hashing] → Hash passwords before saving
     ↓
[Database Operation]
     ↓
[Audit Logging] → Record what happened
     ↓
[Exception Handling] → If error, show safe message
     ↓
Response to User
```

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
