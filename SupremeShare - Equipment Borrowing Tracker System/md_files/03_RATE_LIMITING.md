# Rate Limiting

**File Location:** `Middleware/RateLimitingMiddleware.cs`

---

## What is Rate Limiting?

Rate limiting controls how many requests a user can make in a given time period.

**Without rate limiting:** A hacker can try thousands of passwords per second (brute force attack).

**With rate limiting:** After 100 requests per minute, the system blocks them.

---

## How It Works

### Step 1: User makes a request
```
Request from IP: 192.168.1.100
```

### Step 2: System checks request count
```
This IP made 50 requests this minute
Limit is 100 per minute
50 < 100 → Allow request
```

### Step 3: If limit exceeded
```
This IP made 101 requests this minute
101 > 100 → Block request, return "Too Many Requests"
```

---

## The Limits

| Limit Type | Value | Purpose |
|------------|-------|---------|
| Per Minute | 100 requests | Prevents sustained attacks |
| Per Second | 10 requests | Prevents burst attacks |

---

## The Code

### RateLimitingMiddleware.cs

```csharp
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    
    // Store request counts per IP address
    private static readonly ConcurrentDictionary<string, RateLimitInfo> _clients = new();
    
    private const int MaxRequestsPerMinute = 100;
    private const int MaxRequestsPerSecond = 10;

    public async Task InvokeAsync(HttpContext context)
    {
        // Step 1: Get the client's IP address
        var clientIp = GetClientIp(context);
        var now = DateTime.UtcNow;

        // Step 2: Get or create rate limit info for this IP
        if (!_clients.TryGetValue(clientIp, out var rateLimitInfo))
        {
            rateLimitInfo = new RateLimitInfo();
            _clients[clientIp] = rateLimitInfo;
        }

        lock (rateLimitInfo)
        {
            // Step 3: Reset counters if time window passed
            if ((now - rateLimitInfo.MinuteWindowStart).TotalMinutes >= 1)
            {
                rateLimitInfo.MinuteRequestCount = 0;
                rateLimitInfo.MinuteWindowStart = now;
            }

            // Step 4: Check if limit exceeded
            if (rateLimitInfo.MinuteRequestCount >= MaxRequestsPerMinute ||
                rateLimitInfo.SecondRequestCount >= MaxRequestsPerSecond)
            {
                _logger.LogWarning("Rate limit exceeded for IP: {ClientIp}", clientIp);
                context.Response.StatusCode = 429; // Too Many Requests
                context.Response.Headers.Add("Retry-After", "60");
                return;
            }

            // Step 5: Increment counters
            rateLimitInfo.MinuteRequestCount++;
            rateLimitInfo.SecondRequestCount++;
        }

        // Step 6: Allow request to continue
        await _next(context);
    }
}
```

---

## How IP Address is Detected

```csharp
private string GetClientIp(HttpContext context)
{
    // Check for forwarded IP (if behind proxy/load balancer)
    var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
    if (!string.IsNullOrEmpty(forwardedFor))
    {
        return forwardedFor.Split(',')[0].Trim();
    }

    // Otherwise use direct connection IP
    return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}
```

---

## Registration in Program.cs

```csharp
// Add rate limiting middleware (order matters - should be early in pipeline)
app.UseMiddleware<RateLimitingMiddleware>();
```

---

## What Happens When Blocked

### User Experience:
```
HTTP Status: 429 Too Many Requests
Header: Retry-After: 60
```

### Server Log:
```
[Warning] Rate limit exceeded for IP: 192.168.1.100
```

---

## Real-World Scenario

### Normal User:
```
9:00:00 - Request 1 ✓
9:00:01 - Request 2 ✓
9:00:02 - Request 3 ✓
... (continues normally)
```

### Brute Force Attacker:
```
9:00:00 - Request 1-10 ✓ (10 per second limit)
9:00:00 - Request 11 ✗ BLOCKED
9:00:01 - Request 12-21 ✓
9:00:01 - Request 22 ✗ BLOCKED
... (attacker slowed down significantly)
9:01:00 - Request 101 ✗ BLOCKED (minute limit reached)
```

---

## Memory Cleanup

Old entries are automatically cleaned up to prevent memory issues:

```csharp
private void CleanupOldEntries()
{
    var cutoff = DateTime.UtcNow.AddMinutes(-5);
    foreach (var kvp in _clients)
    {
        // Remove IPs that haven't made requests in 5 minutes
        if (kvp.Value.LastRequestTime < cutoff)
        {
            _clients.TryRemove(kvp.Key, out _);
        }
    }
}
```

---

## Adjusting Limits

To change the limits, modify these constants:

```csharp
// For stricter limits (more secure, may affect legitimate users)
private const int MaxRequestsPerMinute = 60;
private const int MaxRequestsPerSecond = 5;

// For looser limits (less secure, better user experience)
private const int MaxRequestsPerMinute = 200;
private const int MaxRequestsPerSecond = 20;
```
