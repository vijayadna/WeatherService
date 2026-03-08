using System.Net;
using System.Text.Json;

namespace WeatherService.API
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
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
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error");
                await WriteErrorAsync(context, HttpStatusCode.BadRequest, ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access");
                await WriteErrorAsync(context, HttpStatusCode.Unauthorized, "Unauthorized");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception");
                await WriteErrorAsync(context, HttpStatusCode.InternalServerError, "An unexpected error occurred.");
            }
        }

        private static Task WriteErrorAsync(HttpContext ctx, HttpStatusCode status, string message)
        {
            ctx.Response.ContentType = "application/json";
            ctx.Response.StatusCode = (int)status;
            var body = JsonSerializer.Serialize(new
            {
                error = message,
                status = (int)status,
                timestamp = DateTime.UtcNow
            });
            return ctx.Response.WriteAsync(body);
        }
    }
}
