namespace DataWin.Auth.Api.Middleware;

public sealed class PiiAuditMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PiiAuditMiddleware> _logger;

    private static readonly HashSet<string> PiiEndpoints = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/user/profile",
        "/api/user/export",
        "/api/consent"
    };

    public PiiAuditMiddleware(RequestDelegate next, ILogger<PiiAuditMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        if (PiiEndpoints.Any(e => path.StartsWith(e, StringComparison.OrdinalIgnoreCase)))
        {
            _logger.LogInformation("PII endpoint accessed: {Path} by {User} from {IP}",
                path,
                context.User.Identity?.Name ?? "anonymous",
                context.Connection.RemoteIpAddress);
        }

        await _next(context);
    }
}
