# Input Validation

**File Location:** `Data/InputValidator.cs`

---

## What is Input Validation?

Input validation checks if user-submitted data is safe and correct before using it.

**Without validation:** A hacker could type `'; DROP TABLE USER;--` in a username field and delete your entire database!

**With validation:** The system rejects dangerous input before it reaches the database.

---

## How It Works

### Step 1: User submits data
```
Username: john_doe
Email: john@email.com
```

### Step 2: System validates each field
```
Is username valid? → Check pattern, length, dangerous characters
Is email valid? → Check email format
```

### Step 3: Accept or Reject
```
✓ Valid → Continue to database
✗ Invalid → Return error message
```

---

## Validation Methods

### 1. Sanitize String (Remove dangerous characters)

**Process:** Takes any string, trims it, limits length, and checks for SQL injection patterns.

```csharp
// In InputValidator.cs
public static string SanitizeString(string? input, int maxLength = 255)
{
    if (string.IsNullOrWhiteSpace(input))
        return string.Empty;

    var sanitized = input.Trim();
    if (sanitized.Length > maxLength)
        sanitized = sanitized.Substring(0, maxLength);

    // Check for SQL injection patterns
    if (SqlInjectionPattern.IsMatch(sanitized))
        throw new ValidationException("Invalid characters detected in input.");

    return sanitized;
}
```

**Usage in Repository:**
```csharp
// In UserRepository.cs - AddAsync method
new("@username", InputValidator.SanitizeString(user.UserUsername, 50))
```

---

### 2. Validate Email

**Process:** Checks if email follows the correct format (something@domain.com).

```csharp
// In InputValidator.cs
public static void ValidateEmail(string? email)
{
    if (string.IsNullOrWhiteSpace(email)) return;
    
    if (!EmailPattern.IsMatch(email))
        throw new ValidationException("Invalid email format.");
}
```

**Usage:**
```csharp
// In StudentRepository.cs - ValidateStudent method
InputValidator.ValidateEmail(student.StudentEmail);
```

---

### 3. Validate Username

**Process:** Ensures username is 3-50 characters with only letters, numbers, and underscores.

```csharp
// In InputValidator.cs
public static void ValidateUsername(string username)
{
    if (string.IsNullOrWhiteSpace(username))
        throw new ValidationException("Username is required.");
    
    if (!UsernamePattern.IsMatch(username))
        throw new ValidationException("Username must be 3-50 characters...");
}
```

**Usage:**
```csharp
// In UserRepository.cs - ValidateUser method
InputValidator.ValidateUsername(user.UserUsername);
```

---

### 4. Validate ID (Positive numbers only)

**Process:** Ensures IDs are positive numbers (prevents negative ID attacks).

```csharp
// In InputValidator.cs
public static void ValidateId(int id, string fieldName)
{
    if (id <= 0)
        throw new ValidationException($"{fieldName} must be a positive number.");
}
```

**Usage:**
```csharp
// In EquipmentRepository.cs - GetByIdAsync method
InputValidator.ValidateId(equipmentId, "Equipment ID");
```

---

### 5. Validate Role

**Process:** Only allows predefined roles (admin, support_admin).

```csharp
// In InputValidator.cs
public static void ValidateRole(string role)
{
    var validRoles = new[] { "admin", "support_admin" };
    if (!Array.Exists(validRoles, r => r.Equals(role, StringComparison.OrdinalIgnoreCase)))
        throw new ValidationException("Invalid user role.");
}
```

**Usage:**
```csharp
// In UserRepository.cs - ValidateUser method
InputValidator.ValidateRole(user.UserRole);
```

---

## SQL Injection Protection

The system blocks these dangerous patterns:
- `'` (single quotes)
- `--` (SQL comments)
- `;` (statement separator)
- `DROP`, `DELETE`, `INSERT`, `UPDATE` keywords
- `xp_`, `sp_` (stored procedure calls)

**Example Attack Blocked:**
```
Input: "admin'; DROP TABLE USER;--"
Result: ValidationException thrown, attack blocked!
```

---

## Complete Validation Flow Example

```csharp
// When adding a new user in UserRepository.cs

public async Task<bool> AddAsync(User user)
{
    // Step 1: Validate all fields
    ValidateUser(user);  // Calls multiple validators

    // Step 2: Sanitize before database insert
    var parameters = new NpgsqlParameter[]
    {
        new("@username", InputValidator.SanitizeString(user.UserUsername, 50)),
        new("@email", InputValidator.SanitizeString(user.UserEmail, 100)),
        // ... more parameters
    };

    // Step 3: Safe to insert into database
    return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
}

private void ValidateUser(User user)
{
    InputValidator.ValidateUsername(user.UserUsername);
    InputValidator.ValidateRole(user.UserRole);
    InputValidator.ValidateName(user.UserLastName, "Last name");
    InputValidator.ValidateName(user.UserFirstName, "First name");
    InputValidator.ValidateEmail(user.UserEmail);
    InputValidator.ValidatePhone(user.UserContactNo);
}
```
