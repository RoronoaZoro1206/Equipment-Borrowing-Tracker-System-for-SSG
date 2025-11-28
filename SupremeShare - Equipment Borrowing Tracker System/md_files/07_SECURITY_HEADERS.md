# Security Headers

**File Location:** `Middleware/SecurityHeadersMiddleware.cs`

---

## What are Security Headers?

Security headers are instructions sent to the browser that tell it how to protect your website.

**Without security headers:** Hackers can embed your site in a fake page, inject malicious scripts, or steal data.

**With security headers:** The browser blocks these attacks automatically.

---

## How It Works

### Step 1: Server sends response
```
Your page content...
```

### Step 2: Middleware adds security headers
```
X-Frame-Options: DENY
X-Content-Type-Options: nosniff
X-XSS-Protection: 1; mode=block
... (more headers)
```

### Step 3: Browser reads headers and enforces rules
```
Browser: "I won't allow this page to be embedded in an iframe"
Browser: "I won't run scripts from untrusted sources"
```

---

## The Headers Explained

### 1. X-Frame-Options: DENY

**Attack Prevented:** Clickjacking

**What is Clickjacking?**
Hacker embeds your site in an invisible iframe and tricks users into clicking buttons.

```
Without header:
┌─────────────────────────────┐
│ Fake Page                   │
│  ┌───────────────────────┐  │
│  │ Your Site (invisible) │  │
│  │  [Delete Account]     │  │ ← User thinks they're clicking something else
│  └───────────────────────┘  │
└─────────────────────────────┘

With header:
Browser refuses to load your site in an iframe!
```

---

### 2. X-Content-Type-Options: nosniff

**Attack Prevented:** MIME Type Sniffing

**What is MIME Sniffing?**
Browser guesses file types, which can execute malicious files.

```
Without header:
Hacker uploads "image.jpg" that's actually JavaScript
Browser: "Hmm, this looks like JavaScript, let me run it!"

With header:
Browser: "Server says this is an image, I'll treat it as an image only"
```

---

### 3. X-XSS-Protection: 1; mode=block

**Attack Prevented:** Cross-Site Scripting (XSS)

**What is XSS?**
Hacker injects malicious scripts into your page.

```
Without header:
URL: yoursite.com/search?q=<script>stealCookies()</script>
Browser runs the malicious script!

With header:
Browser detects the attack and blocks the page
```

---

### 4. Content-Security-Policy

**Attack Prevented:** Script Injection, Data Theft

**What it does:**
Tells browser which sources are allowed for scripts, styles, images, etc.

```csharp
"default-src 'self'; script-src 'self' 'unsafe-inline' 'unsafe-eval'; style-src 'self' 'unsafe-inline';"
```

| Directive | Meaning |
|-----------|---------|
| `default-src 'self'` | Only load resources from same domain |
| `script-src 'self'` | Only run scripts from same domain |
| `style-src 'self'` | Only load styles from same domain |

---

### 5. Strict-Transport-Security (HSTS)

**Attack Prevented:** Man-in-the-Middle, SSL Stripping

**What it does:**
Forces browser to always use HTTPS.

```
Without HSTS:
User types: yoursite.com
Browser: Goes to http://yoursite.com first
Hacker: Intercepts and steals data!

With HSTS:
User types: yoursite.com
Browser: "I remember this site requires HTTPS"
Browser: Goes directly to https://yoursite.com
```

---

### 6. Referrer-Policy: strict-origin-when-cross-origin

**Attack Prevented:** Information Leakage

**What it does:**
Controls what information is sent when clicking links to other sites.

```
Without policy:
User clicks link to external site
External site sees: "User came from yoursite.com/admin/users/123"

With policy:
External site only sees: "User came from yoursite.com"
```

---

### 7. Permissions-Policy

**Attack Prevented:** Unauthorized Feature Access

**What it does:**
Disables browser features you don't need.

```csharp
"accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=()"
```

This means: "My site doesn't need camera, microphone, location, etc."

---

## The Code

```csharp
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Add all security headers before sending response
        
        // Prevent clickjacking
        context.Response.Headers.Add("X-Frame-Options", "DENY");
        
        // Prevent MIME type sniffing
        context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
        
        // Enable XSS filter
        context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
        
        // Control referrer information
        context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
        
        // Content Security Policy
        context.Response.Headers.Add("Content-Security-Policy", 
            "default-src 'self'; script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
            "style-src 'self' 'unsafe-inline'; img-src 'self' data:; font-src 'self';");
        
        // Disable unnecessary browser features
        context.Response.Headers.Add("Permissions-Policy", 
            "accelerometer=(), camera=(), geolocation=(), gyroscope=(), " +
            "magnetometer=(), microphone=(), payment=(), usb=()");
        
        // Force HTTPS
        context.Response.Headers.Add("Strict-Transport-Security", 
            "max-age=31536000; includeSubDomains");

        // Continue to next middleware
        await _next(context);
    }
}
```

---

## Registration in Program.cs

```csharp
// Add security headers middleware (should be early in pipeline)
app.UseMiddleware<SecurityHeadersMiddleware>();
```

---

## Testing Your Headers

You can check your headers using browser developer tools:

1. Open your site
2. Press F12 (Developer Tools)
3. Go to Network tab
4. Click on any request
5. Look at Response Headers

You should see all the security headers listed.

---

## Summary Table

| Header | Protects Against |
|--------|------------------|
| X-Frame-Options | Clickjacking |
| X-Content-Type-Options | MIME sniffing |
| X-XSS-Protection | Cross-site scripting |
| Content-Security-Policy | Script injection |
| Strict-Transport-Security | Man-in-the-middle |
| Referrer-Policy | Information leakage |
| Permissions-Policy | Unauthorized feature access |
