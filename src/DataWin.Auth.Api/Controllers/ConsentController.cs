using DataWin.Auth.Application.Abstractions;
using DataWin.Auth.Application.Commands.Consent;
using DataWin.Auth.Application.Commands.Erasure;
using DataWin.Auth.Application.DTOs;
using DataWin.Auth.Application.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DataWin.Auth.Api.Controllers;

[ApiController]
[Route("api/consent")]
[Authorize]
public sealed class ConsentController : ControllerBase
{
    private readonly ICommandHandler<GrantConsentCommand, bool> _grantHandler;
    private readonly ICommandHandler<WithdrawConsentCommand, bool> _withdrawHandler;
    private readonly IQueryHandler<GetConsentStatusQuery, ConsentStatusDto> _statusHandler;
    private readonly ICommandHandler<RequestErasureCommand, Guid> _erasureHandler;

    public ConsentController(
        ICommandHandler<GrantConsentCommand, bool> grantHandler,
        ICommandHandler<WithdrawConsentCommand, bool> withdrawHandler,
        IQueryHandler<GetConsentStatusQuery, ConsentStatusDto> statusHandler,
        ICommandHandler<RequestErasureCommand, Guid> erasureHandler)
    {
        _grantHandler = grantHandler;
        _withdrawHandler = withdrawHandler;
        _statusHandler = statusHandler;
        _erasureHandler = erasureHandler;
    }

    [HttpPost("grant")]
    public async Task<IActionResult> Grant([FromBody] ConsentRequest request, CancellationToken ct)
    {
        var command = new GrantConsentCommand
        {
            TenantId = request.TenantId,
            UserId = request.UserId,
            Purpose = request.Purpose,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"
        };

        await _grantHandler.HandleAsync(command, ct);
        return NoContent();
    }

    [HttpPost("withdraw")]
    public async Task<IActionResult> Withdraw([FromBody] ConsentRequest request, CancellationToken ct)
    {
        var command = new WithdrawConsentCommand
        {
            TenantId = request.TenantId,
            UserId = request.UserId,
            Purpose = request.Purpose
        };

        await _withdrawHandler.HandleAsync(command, ct);
        return NoContent();
    }

    [HttpGet("status")]
    public async Task<IActionResult> Status([FromQuery] Guid tenantId, [FromQuery] Guid userId, CancellationToken ct)
    {
        var query = new GetConsentStatusQuery { TenantId = tenantId, UserId = userId };
        var result = await _statusHandler.HandleAsync(query, ct);
        return Ok(result);
    }

    [HttpPost("erasure")]
    public async Task<IActionResult> RequestErasure([FromBody] ErasureRequest request, CancellationToken ct)
    {
        var command = new RequestErasureCommand
        {
            TenantId = request.TenantId,
            UserId = request.UserId,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"
        };

        var requestId = await _erasureHandler.HandleAsync(command, ct);
        return Accepted(new { RequestId = requestId });
    }
}

public sealed record ConsentRequest(Guid TenantId, Guid UserId, string Purpose);
public sealed record ErasureRequest(Guid TenantId, Guid UserId);
