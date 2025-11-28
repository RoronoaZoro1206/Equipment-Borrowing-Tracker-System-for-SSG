using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace SupremeShare___Equipment_Borrowing_Tracker_System.Middleware
{
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;

        public SecurityHeadersMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Prevent clickjacking
            context.Response.Headers["X-Frame-Options"] = "DENY";

            // Prevent MIME type sniffing
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";

            // Enable XSS filter
            context.Response.Headers["X-XSS-Protection"] = "1; mode=block";

            // Referrer policy
            context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

            // Content Security Policy - updated to allow CDNs
            context.Response.Headers["Content-Security-Policy"] =
                "default-src 'self'; " +
                "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
                "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://fonts.googleapis.com; " +
                "font-src 'self' https://cdn.jsdelivr.net https://fonts.gstatic.com; " +
                "img-src 'self' data:; " +
                "connect-src 'self' https://fonts.googleapis.com https://fonts.gstatic.com;";

            // Permissions Policy
            context.Response.Headers["Permissions-Policy"] =
                "accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=()";

            // Strict Transport Security (HTTPS only)
            context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";

            await _next(context);
        }
    }
}
