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
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var response = context.Response;
            response.ContentType = "application/json";

            var errorResponse = new ErrorResponse();

            switch (exception)
            {
                case ValidationException validationEx:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    errorResponse.Message = validationEx.Message;
                    errorResponse.StatusCode = response.StatusCode;
                    _logger.LogWarning("Validation error: {Message}", validationEx.Message);
                    break;

                case NotFoundException notFoundEx:
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    errorResponse.Message = notFoundEx.Message;
                    errorResponse.StatusCode = response.StatusCode;
                    _logger.LogWarning("Not found: {Message}", notFoundEx.Message);
                    break;

                case Exceptions.UnauthorizedAccessException unauthorizedEx:
                    response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    errorResponse.Message = unauthorizedEx.Message;
                    errorResponse.StatusCode = response.StatusCode;
                    _logger.LogWarning("Unauthorized access: {Message}", unauthorizedEx.Message);
                    break;

                case Data.DatabaseException dbEx:
                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    errorResponse.Message = "A database error occurred. Please try again later.";
                    errorResponse.StatusCode = response.StatusCode;
                    _logger.LogError(dbEx, "Database error occurred");
                    break;

                default:
                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    errorResponse.Message = "An unexpected error occurred. Please try again later.";
                    errorResponse.StatusCode = response.StatusCode;
                    // Log full exception details but don't expose to client
                    _logger.LogError(exception, "Unhandled exception occurred");
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
