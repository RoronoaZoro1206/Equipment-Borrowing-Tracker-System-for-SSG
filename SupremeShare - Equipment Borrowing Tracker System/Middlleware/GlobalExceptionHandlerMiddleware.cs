using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SupremeShare___Equipment_Borrowing_Tracker_System.Exceptions;

namespace SupremeShare___Equipment_Borrowing_Tracker_System.Middleware
{
    public class GlobalExceptionHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

        public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception occurred at {Path}", context.Request.Path);
                
                // Check if the request expects HTML (browser request)
                var acceptHeader = context.Request.Headers["Accept"].ToString();
                var isHtmlRequest = acceptHeader.Contains("text/html") || 
                                   !acceptHeader.Contains("application/json");

                if (isHtmlRequest && !context.Response.HasStarted)
                {
                    // For HTML requests, redirect to error page
                    context.Response.Redirect("/Home/Error");
                    return;
                }

                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            if (context.Response.HasStarted)
            {
                _logger.LogWarning("Response has already started, cannot modify response");
                return;
            }

            var response = context.Response;
            response.ContentType = "application/json";

            var errorResponse = new ErrorResponse();

            switch (exception)
            {
                case ValidationException validationEx:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    errorResponse.Message = validationEx.Message;
                    errorResponse.StatusCode = response.StatusCode;
                    break;

                case NotFoundException notFoundEx:
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    errorResponse.Message = notFoundEx.Message;
                    errorResponse.StatusCode = response.StatusCode;
                    break;

                case Exceptions.UnauthorizedAccessException unauthorizedEx:
                    response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    errorResponse.Message = unauthorizedEx.Message;
                    errorResponse.StatusCode = response.StatusCode;
                    break;

                case Data.DatabaseException:
                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    errorResponse.Message = "A database error occurred. Please try again later.";
                    errorResponse.StatusCode = response.StatusCode;
                    break;

                default:
                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    errorResponse.Message = "An unexpected error occurred. Please try again later.";
                    errorResponse.StatusCode = response.StatusCode;
                    break;
            }

            var result = JsonSerializer.Serialize(errorResponse);
            await response.WriteAsync(result);
        }
    }

    public class ErrorResponse
    {
        public int StatusCode { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
