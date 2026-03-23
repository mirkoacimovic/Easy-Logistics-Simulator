using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;

namespace EasyLogistics.Telemetry.System.Web.Filters;

public class GlobalExceptionFilter : IExceptionFilter
{
    private readonly ILogger<GlobalExceptionFilter> _logger;

    public GlobalExceptionFilter(ILogger<GlobalExceptionFilter> logger) => _logger = logger;

    public void OnException(ExceptionContext context)
    {
        // Log the full stack trace for Belgrade debugging
        _logger.LogError(context.Exception, ">>>> 🚨 BRIDGE_FAULT: {Message}", context.Exception.Message);

        var errorResponse = new
        {
            error = "INTERNAL_SYSTEM_FAILURE",
            detail = context.Exception.Message,
            timestamp = DateTime.Now.ToString("HH:mm:ss"),
            isBridgeError = context.Exception is IOException || context.Exception is UnauthorizedAccessException
        };

        // If it's a Bridge error (MMF missing), we might return a 503 Service Unavailable
        int statusCode = errorResponse.isBridgeError ? 503 : 500;

        context.Result = new ObjectResult(errorResponse)
        {
            StatusCode = statusCode
        };

        context.ExceptionHandled = true;
    }
}