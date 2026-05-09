namespace DataWin.Auth.Api.Middleware;

public sealed class RegionalRoutingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RegionalRoutingMiddleware> _logger;

    public RegionalRoutingMiddleware(RequestDelegate next, ILogger<RegionalRoutingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, MutableTenantContext tenantContext)
    {
        if (tenantContext.IsResolved)
        {
            context.Items["RegionCode"] = tenantContext.PrimaryRegion.Value;
            _logger.LogDebug("Request routed to region {Region} for tenant {TenantId}",
                tenantContext.PrimaryRegion, tenantContext.TenantId);
        }

        await _next(context);
    }
}
