using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace SupremeShare___Equipment_Borrowing_Tracker_System.Middleware
{
    public class RateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RateLimitingMiddleware> _logger;
        private static readonly ConcurrentDictionary<string, RateLimitInfo> _clients = new();

        private const int MaxRequestsPerMinute = 500;
        private const int MaxRequestsPerSecond = 50;

        public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var clientIp = GetClientIp(context);
            var now = DateTime.UtcNow;

            if (!_clients.TryGetValue(clientIp, out var rateLimitInfo))
            {
                rateLimitInfo = new RateLimitInfo();
                _clients[clientIp] = rateLimitInfo;
            }

            // Clean up old entries periodically
            CleanupOldEntries();

            // Check rate limits
            lock (rateLimitInfo)
            {
                // Reset counters if time window has passed
                if ((now - rateLimitInfo.MinuteWindowStart).TotalMinutes >= 1)
                {
                    rateLimitInfo.MinuteRequestCount = 0;
                    rateLimitInfo.MinuteWindowStart = now;
                }

                if ((now - rateLimitInfo.SecondWindowStart).TotalSeconds >= 1)
                {
                    rateLimitInfo.SecondRequestCount = 0;
                    rateLimitInfo.SecondWindowStart = now;
                }

                // Check if rate limit exceeded
                if (rateLimitInfo.MinuteRequestCount >= MaxRequestsPerMinute ||
                    rateLimitInfo.SecondRequestCount >= MaxRequestsPerSecond)
                {
                    _logger.LogWarning("Rate limit exceeded for IP: {ClientIp}", clientIp);
                    context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                    context.Response.Headers["Retry-After"] = "60";
                    return;
                }

                rateLimitInfo.MinuteRequestCount++;
                rateLimitInfo.SecondRequestCount++;
                rateLimitInfo.LastRequestTime = now;
            }

            await _next(context);
        }

        private string GetClientIp(HttpContext context)
        {
            // Check for forwarded IP (behind proxy/load balancer)
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                return forwardedFor.Split(',')[0].Trim();
            }

            return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        private void CleanupOldEntries()
        {
            var cutoff = DateTime.UtcNow.AddMinutes(-5);
            foreach (var kvp in _clients)
            {
                if (kvp.Value.LastRequestTime < cutoff)
                {
                    _clients.TryRemove(kvp.Key, out _);
                }
            }
        }

        private class RateLimitInfo
        {
            public int MinuteRequestCount { get; set; }
            public int SecondRequestCount { get; set; }
            public DateTime MinuteWindowStart { get; set; } = DateTime.UtcNow;
            public DateTime SecondWindowStart { get; set; } = DateTime.UtcNow;
            public DateTime LastRequestTime { get; set; } = DateTime.UtcNow;
        }
    }
}
