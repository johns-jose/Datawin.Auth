using DataWin.Auth.Core.Interfaces;
using DataWin.Auth.Core.ValueObjects;

namespace DataWin.Auth.Api.Middleware;

public sealed class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, MutableTenantContext tenantContext, Core.Interfaces.Repositories.ITenantRepository tenantRepo)
    {
        var tenantSlug = context.Request.Headers["X-Tenant-Id"].FirstOrDefault()
            ?? context.Request.RouteValues["tenantSlug"]?.ToString();

        if (!string.IsNullOrWhiteSpace(tenantSlug))
        {
            var tenant = await tenantRepo.GetBySlugAsync(tenantSlug, context.RequestAborted);
            if (tenant is not null)
            {
                var regions = await tenantRepo.GetRegionsAsync(tenant.Id, context.RequestAborted);
                var primaryRegion = regions.FirstOrDefault(r => r.IsPrimary);

                tenantContext.TenantId = new TenantId(tenant.Id);
                tenantContext.PrimaryRegion = primaryRegion?.RegionCode ?? new RegionCode("us-east-1");
                tenantContext.IsResolved = true;
            }
        }

        await _next(context);
    }
}

public sealed class MutableTenantContext : ITenantContext
{
    public TenantId TenantId { get; set; } = TenantId.Empty;
    public RegionCode PrimaryRegion { get; set; } = new("us-east-1");
    public bool IsResolved { get; set; }
}
