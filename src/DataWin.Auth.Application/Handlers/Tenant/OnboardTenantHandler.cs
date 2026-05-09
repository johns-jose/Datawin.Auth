using DataWin.Auth.Application.Abstractions;
using DataWin.Auth.Application.Commands.Tenant;
using DataWin.Auth.Application.DTOs;
using DataWin.Auth.Core.Entities;
using DataWin.Auth.Core.Enums;
using DataWin.Auth.Core.Interfaces.Repositories;
using DataWin.Auth.Core.ValueObjects;
using Microsoft.Extensions.Logging;

namespace DataWin.Auth.Application.Handlers.Tenant;

public sealed class OnboardTenantHandler : ICommandHandler<OnboardTenantCommand, TenantDto>
{
    private readonly ITenantRepository _tenantRepo;
    private readonly ILogger<OnboardTenantHandler> _logger;

    public OnboardTenantHandler(ITenantRepository tenantRepo, ILogger<OnboardTenantHandler> logger)
    {
        _tenantRepo = tenantRepo;
        _logger = logger;
    }

    public async Task<TenantDto> HandleAsync(OnboardTenantCommand command, CancellationToken ct = default)
    {
        var existing = await _tenantRepo.GetBySlugAsync(command.Slug, ct);
        if (existing is not null)
            throw new InvalidOperationException($"Tenant with slug '{command.Slug}' already exists.");

        var tenant = new Core.Entities.Tenant
        {
            Id = UuidV7.New(),
            Name = command.Name,
            Slug = command.Slug,
            Domain = command.Domain,
            Status = TenantStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        await _tenantRepo.CreateAsync(tenant, ct);

        var primaryRegion = new TenantRegion
        {
            Id = UuidV7.New(),
            TenantId = tenant.Id,
            RegionCode = new RegionCode(command.PrimaryRegion),
            IsPrimary = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
        await _tenantRepo.AddRegionAsync(primaryRegion, ct);

        var regions = new List<TenantRegionDto>
        {
            new() { RegionCode = command.PrimaryRegion, IsPrimary = true }
        };

        if (command.AdditionalRegions is not null)
        {
            foreach (var regionCode in command.AdditionalRegions)
            {
                var region = new TenantRegion
                {
                    Id = UuidV7.New(),
                    TenantId = tenant.Id,
                    RegionCode = new RegionCode(regionCode),
                    IsPrimary = false,
                    CreatedAt = DateTimeOffset.UtcNow
                };
                await _tenantRepo.AddRegionAsync(region, ct);
                regions.Add(new() { RegionCode = regionCode, IsPrimary = false });
            }
        }

        _logger.LogInformation("Tenant {TenantId} ({Slug}) onboarded with primary region {Region}", tenant.Id, command.Slug, command.PrimaryRegion);

        return new TenantDto
        {
            Id = tenant.Id,
            Name = tenant.Name,
            Slug = tenant.Slug,
            Domain = tenant.Domain,
            Status = tenant.Status.ToString(),
            Regions = regions
        };
    }
}
