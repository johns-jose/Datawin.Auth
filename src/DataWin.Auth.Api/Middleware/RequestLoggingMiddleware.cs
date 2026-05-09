using System.Diagnostics;

namespace DataWin.Auth.Api.Middleware;

public sealed class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, MutableTenantContext tenantContext)
    {
        var correlationId = context.Items["CorrelationId"]?.ToString() ?? "unknown";
        var tenantId = tenantContext.IsResolved ? tenantContext.TenantId.ToString() : "unresolved";
        var userId = context.User.FindFirst("sub")?.Value ?? "anonymous";

        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["TenantId"] = tenantId,
            ["UserId"] = userId,
            ["ClientIp"] = context.Connection.RemoteIpAddress?.ToString() ?? "unknown"
        }))
        {
            var stopwatch = Stopwatch.StartNew();
            var method = context.Request.Method;
            var path = context.Request.Path.Value;

            _logger.LogInformation("Request started {Method} {Path}", method, path);

            try
            {
                await _next(context);
            }
            finally
            {
                stopwatch.Stop();
                var statusCode = context.Response.StatusCode;
                var elapsed = stopwatch.ElapsedMilliseconds;

                if (statusCode >= 500)
                    _logger.LogError("Request completed {Method} {Path} {StatusCode} in {ElapsedMs}ms", method, path, statusCode, elapsed);
                else if (statusCode >= 400)
                    _logger.LogWarning("Request completed {Method} {Path} {StatusCode} in {ElapsedMs}ms", method, path, statusCode, elapsed);
                else
                    _logger.LogInformation("Request completed {Method} {Path} {StatusCode} in {ElapsedMs}ms", method, path, statusCode, elapsed);
            }
        }
    }
}