# Exception Handling

**File Location:** `Middleware/GlobalExceptionHandlerMiddleware.cs`

---

## What is Exception Handling?

Exception handling catches errors and returns safe, user-friendly messages instead of exposing system details.

**Without exception handling:** Hackers see:
```
NpgsqlException: relation "USER" does not exist
Stack trace: at DatabaseHelper.ExecuteQueryAsync() line 45
Connection string: Host=localhost;Database=equipment_db;Username=admin...
```

**With exception handling:** Hackers see:
```
{ "message": "An unexpected error occurred. Please try again later." }
```

---

## How It Works

### Step 1: Error occurs somewhere in the application
```
Database connection failed!
```

### Step 2: Middleware catches the error
```
Exception caught: NpgsqlException
```

### Step 3: Log full details (for developers)
```
[Error] Database error occurred
  Exception: NpgsqlException
  Message: Connection refused
  Stack trace: ...
```

### Step 4: Return safe message (for users)
```
HTTP 500
{ "message": "A database error occurred. Please try again later." }
```

---

## Custom Exception Types

**File Location:** `Exceptions/DatabaseException.cs`

```csharp
// Thrown when input validation fails
public class ValidationException : Exception
{
    public ValidationException(string message) : base(message) { }
}

// Thrown when a record is not found
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}

// Thrown when user doesn't have permission
public class UnauthorizedAccessException : Exception
{
    public UnauthorizedAccessException(string message) : base(message) { }
}
```

---

## The Middleware Code

```csharp
public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            // Try to process the request normally
            await _next(context);
        }
        catch (Exception ex)
        {
            // If any error occurs, handle it safely
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = context.Response;
        response.ContentType = "application/json";
        var errorResponse = new ErrorResponse();

        // Different handling based on exception type
        switch (exception)
        {
            case ValidationException validationEx:
                // User input error - show the message
                response.StatusCode = 400; // Bad Request
                errorResponse.Message = validationEx.Message;
                _logger.LogWarning("Validation error: {Message}", validationEx.Message);
                break;

            case NotFoundException notFoundEx:
                // Record not found - show the message
                response.StatusCode = 404; // Not Found
                errorResponse.Message = notFoundEx.Message;
                _logger.LogWarning("Not found: {Message}", notFoundEx.Message);
                break;

            case UnauthorizedAccessException unauthorizedEx:
                // Permission denied - show the message
                response.StatusCode = 401; // Unauthorized
                errorResponse.Message = unauthorizedEx.Message;
                _logger.LogWarning("Unauthorized: {Message}", unauthorizedEx.Message);
                break;

            case DatabaseException dbEx:
                // Database error - hide details!
                response.StatusCode = 500; // Internal Server Error
                errorResponse.Message = "A database error occurred. Please try again later.";
                _logger.LogError(dbEx, "Database error occurred"); // Full details in log
                break;

            default:
                // Unknown error - hide everything!
                response.StatusCode = 500;
                errorResponse.Message = "An unexpected error occurred. Please try again later.";
                _logger.LogError(exception, "Unhandled exception"); // Full details in log
                break;
        }

        var result = JsonSerializer.Serialize(errorResponse);
        await response.WriteAsync(result);
    }
}
```

---

## Error Response Format

```csharp
public class ErrorResponse
{
    public int StatusCode { get; set; }
    public string Message { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
```

**Example Response:**
```json
{
    "statusCode": 400,
    "message": "Invalid email format.",
    "timestamp": "2025-11-29T10:30:00Z"
}
```

---

## HTTP Status Codes Used

| Code | Meaning | When Used |
|------|---------|-----------|
| 400 | Bad Request | Validation errors (invalid input) |
| 401 | Unauthorized | User not logged in or wrong role |
| 404 | Not Found | Record doesn't exist |
| 429 | Too Many Requests | Rate limit exceeded |
| 500 | Internal Server Error | Database errors, unexpected errors |

---

## Usage in Repositories

### Throwing Validation Errors

```csharp
// In StudentRepository.cs
public async Task<bool> AddAsync(Student student)
{
    // This throws ValidationException if invalid
    ValidateStudent(student);
    
    // ... rest of code
}

private void ValidateStudent(Student student)
{
    if (string.IsNullOrWhiteSpace(student.StudentIdNumber))
        throw new ValidationException("Student ID number is required.");
}
```

### Throwing Not Found Errors

```csharp
// In EquipmentRepository.cs
public async Task<bool> UpdateAsync(Equipment equipment)
{
    var existing = await GetByIdAsync(equipment.EquipmentId);
    
    if (existing == null)
        throw new NotFoundException("Equipment not found.");
    
    // ... rest of code
}
```

---

## Registration in Program.cs

```csharp
// Add exception handling middleware
// Should be early in pipeline to catch all errors
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
```

---

## What Gets Logged vs What User Sees

| Scenario | Log (Developer) | Response (User) |
|----------|-----------------|-----------------|
| Invalid email | "Validation error: Invalid email format" | "Invalid email format." |
| User not found | "Not found: User not found" | "User not found." |
| Database down | Full exception with stack trace | "A database error occurred..." |
| Code bug | Full exception with stack trace | "An unexpected error occurred..." |
