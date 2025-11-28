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
            context.Response.Headers.Add("X-Frame-Options", "DENY");
            
            // Prevent MIME type sniffing
            context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
            
            // Enable XSS filter
            context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
            
            // Referrer policy
            context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
            
            // Content Security Policy
            context.Response.Headers.Add("Content-Security-Policy", 
                "default-src 'self'; script-src 'self' 'unsafe-inline' 'unsafe-eval'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; font-src 'self';");
            
            // Permissions Policy
            context.Response.Headers.Add("Permissions-Policy", 
                "accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=()");
            
            // Strict Transport Security (HTTPS only)
            context.Response.Headers.Add("Strict-Transport-Security", "max-age=31536000; includeSubDomains");

            await _next(context);
        }
    }
}
