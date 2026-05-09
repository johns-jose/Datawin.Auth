using DataWin.Auth.Application.Abstractions;
using DataWin.Auth.Application.Commands.Tenant;
using DataWin.Auth.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DataWin.Auth.Api.Controllers;

[ApiController]
[Route("api/tenant")]
[Authorize]
public sealed class TenantController : ControllerBase
{
    private readonly ICommandHandler<OnboardTenantCommand, TenantDto> _onboardHandler;

    public TenantController(ICommandHandler<OnboardTenantCommand, TenantDto> onboardHandler)
        => _onboardHandler = onboardHandler;

    [HttpPost]
    public async Task<IActionResult> Onboard([FromBody] OnboardTenantRequest request, CancellationToken ct)
    {
        var command = new OnboardTenantCommand
        {
            TenantId = Guid.Empty,
            Name = request.Name,
            Slug = request.Slug,
            Domain = request.Domain,
            PrimaryRegion = request.PrimaryRegion,
            AdditionalRegions = request.AdditionalRegions
        };

        var result = await _onboardHandler.HandleAsync(command, ct);
        return CreatedAtAction(null, new { id = result.Id }, result);
    }
}

public sealed record OnboardTenantRequest(string Name, string Slug, string? Domain, string PrimaryRegion, IReadOnlyList<string>? AdditionalRegions);
