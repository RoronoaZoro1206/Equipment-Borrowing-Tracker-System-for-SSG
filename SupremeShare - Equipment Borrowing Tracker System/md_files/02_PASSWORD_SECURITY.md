# Password Security with BCrypt

**File Location:** `Data/PasswordHasher.cs`

**NuGet Package:** `BCrypt.Net-Next`

---

## What is Password Hashing?

Password hashing converts a readable password into an unreadable string that cannot be reversed.

**Without hashing:** If hackers steal your database, they see:
```
Password: mySecretPassword123
```

**With hashing:** Hackers only see:
```
Password: $2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/X4.G5HqGfuCBLhIWi
```

---

## Why BCrypt?

| Feature | BCrypt | MD5/SHA1 |
|---------|--------|----------|
| Built-in salt | ✓ Yes | ✗ No |
| Slow (good for security) | ✓ Yes | ✗ Too fast |
| Adjustable difficulty | ✓ Yes | ✗ No |
| Industry standard | ✓ Yes | ✗ Outdated |

---

## How BCrypt Works

### Step 1: User creates password
```
Password: "MyPassword123"
```

### Step 2: BCrypt adds random salt and hashes
```
Salt: Random characters added to password
Hash: $2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/X4.G5HqGfuCBLhIWi
```

### Step 3: Store hash in database
```
The original password is never stored!
```

### Step 4: When user logs in, BCrypt verifies
```
Input password → Hash it → Compare with stored hash → Match? Login success!
```

---

## The Code

### PasswordHasher.cs

```csharp
using BCrypt.Net;

namespace SupremeShare___Equipment_Borrowing_Tracker_System.Data
{
    public interface IPasswordHasher
    {
        string HashPassword(string password);
        bool VerifyPassword(string password, string hashedPassword);
    }

    public class PasswordHasher : IPasswordHasher
    {
        // Work factor of 12 = 2^12 = 4096 iterations
        // Higher = more secure but slower
        private const int WorkFactor = 12;

        public string HashPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password cannot be empty.");

            if (password.Length < 8)
                throw new ArgumentException("Password must be at least 8 characters long.");

            return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
        }

        public bool VerifyPassword(string password, string hashedPassword)
        {
            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hashedPassword))
                return false;

            try
            {
                return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
            }
            catch
            {
                return false;
            }
        }
    }
}
```

---

## Usage Examples

### 1. When Creating a New User

```csharp
// In your AuthController or UserService

public async Task<bool> RegisterUser(string username, string password)
{
    // Step 1: Hash the password
    var hashedPassword = _passwordHasher.HashPassword(password);
    
    // Step 2: Create user with hashed password
    var user = new User
    {
        UserUsername = username,
        UserHashedPassword = hashedPassword,  // Store the hash, NOT the password!
        // ... other fields
    };
    
    // Step 3: Save to database
    return await _userRepository.AddAsync(user);
}
```

### 2. When User Logs In

```csharp
// In your AuthController

public async Task<bool> Login(string username, string password)
{
    // Step 1: Get user from database
    var user = await _userRepository.GetByUsernameAsync(username);
    
    if (user == null)
        return false;
    
    // Step 2: Verify password against stored hash
    var isValid = _passwordHasher.VerifyPassword(password, user.UserHashedPassword);
    
    // Step 3: Return result
    return isValid;
}
```

---

## Registration in Program.cs

```csharp
// The PasswordHasher is registered as a singleton (one instance for entire app)
builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
```

---

## Work Factor Explained

The work factor determines how slow the hashing is:

| Work Factor | Time to Hash | Security Level |
|-------------|--------------|----------------|
| 10 | ~100ms | Minimum |
| 12 | ~300ms | Recommended |
| 14 | ~1 second | High security |

**Why slow is good:** If a hacker tries to guess passwords, they can only try ~3 per second instead of millions!

---

## Important Rules

1. **Never store plain passwords** - Always hash before saving
2. **Never log passwords** - Not even in error messages
3. **Minimum 8 characters** - Enforced in the code
4. **Use the interface** - Inject `IPasswordHasher`, not the concrete class

---

## Installation

Run this command in your project folder:
```
dotnet add package BCrypt.Net-Next
```
