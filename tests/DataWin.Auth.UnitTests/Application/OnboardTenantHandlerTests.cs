using DataWin.Auth.Application.Commands.Tenant;
using DataWin.Auth.Application.Handlers.Tenant;
using DataWin.Auth.Core.Entities;
using DataWin.Auth.Core.Interfaces.Repositories;
using DataWin.Auth.Core.ValueObjects;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace DataWin.Auth.UnitTests.Application;

public class OnboardTenantHandlerTests
{
    private readonly ITenantRepository _tenantRepo = Substitute.For<ITenantRepository>();
    private readonly ILogger<OnboardTenantHandler> _logger = Substitute.For<ILogger<OnboardTenantHandler>>();

    [Fact]
    public async Task HandleAsync_NewTenant_CreatesTenantWithRegions()
    {
        _tenantRepo.GetBySlugAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((Tenant?)null);

        var handler = new OnboardTenantHandler(_tenantRepo, _logger);
        var command = new OnboardTenantCommand
        {
            TenantId = Guid.Empty,
            Name = "Acme Corp",
            Slug = "acme-corp",
            Domain = "acme.com",
            PrimaryRegion = "eu-west-1",
            AdditionalRegions = ["us-east-1", "ap-southeast-1"]
        };

        var result = await handler.HandleAsync(command);

        Assert.Equal("Acme Corp", result.Name);
        Assert.Equal("acme-corp", result.Slug);
        Assert.Equal("Active", result.Status);
        Assert.Equal(3, result.Regions.Count);
        Assert.Single(result.Regions, r => r.IsPrimary && r.RegionCode == "eu-west-1");

        await _tenantRepo.Received(1).CreateAsync(Arg.Any<Tenant>(), Arg.Any<CancellationToken>());
        await _tenantRepo.Received(3).AddRegionAsync(Arg.Any<TenantRegion>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_DuplicateSlug_ThrowsException()
    {
        _tenantRepo.GetBySlugAsync("existing", Arg.Any<CancellationToken>())
            .Returns(new Tenant
            {
                Id = UuidV7.New(),
                Name = "Existing",
                Slug = "existing",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });

        var handler = new OnboardTenantHandler(_tenantRepo, _logger);
        var command = new OnboardTenantCommand
        {
            TenantId = Guid.Empty,
            Name = "New Tenant",
            Slug = "existing",
            PrimaryRegion = "us-east-1"
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.HandleAsync(command));
    }
}
