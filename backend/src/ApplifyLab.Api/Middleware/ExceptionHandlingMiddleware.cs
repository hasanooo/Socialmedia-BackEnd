using System.Net;
using System.Text.Json;

namespace ApplifyLab.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
            var status = ex switch
            {
                UnauthorizedAccessException => HttpStatusCode.Forbidden,
                KeyNotFoundException => HttpStatusCode.NotFound,
                InvalidOperationException => HttpStatusCode.Conflict,
                ArgumentException => HttpStatusCode.BadRequest,
                _ => HttpStatusCode.InternalServerError,
            };

            if (status == HttpStatusCode.InternalServerError)
                _logger.LogError(ex, "Unhandled exception");

            context.Response.StatusCode = (int)status;
            context.Response.ContentType = "application/json";
            var payload = JsonSerializer.Serialize(new { error = status == HttpStatusCode.InternalServerError ? "An unexpected error occurred." : ex.Message });
            await context.Response.WriteAsync(payload);
        }
    }
}
