using DataWin.Auth.Application.Abstractions;
using DataWin.Auth.Application.Commands.Mfa;
using DataWin.Auth.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DataWin.Auth.Api.Controllers;

[ApiController]
[Route("api/mfa")]
[Authorize]
public sealed class MfaController : ControllerBase
{
    private readonly ICommandHandler<EnrollMfaCommand, string> _enrollHandler;
    private readonly ICommandHandler<VerifyMfaCommand, AuthResultDto> _verifyHandler;

    public MfaController(
        ICommandHandler<EnrollMfaCommand, string> enrollHandler,
        ICommandHandler<VerifyMfaCommand, AuthResultDto> verifyHandler)
    {
        _enrollHandler = enrollHandler;
        _verifyHandler = verifyHandler;
    }

    [HttpPost("enroll")]
    public async Task<IActionResult> Enroll([FromBody] EnrollMfaRequest request, CancellationToken ct)
    {
        var command = new EnrollMfaCommand
        {
            TenantId = request.TenantId,
            UserId = request.UserId,
            Method = request.Method
        };

        var result = await _enrollHandler.HandleAsync(command, ct);
        return Ok(new { ProvisioningUri = result });
    }

    [HttpPost("verify")]
    [AllowAnonymous]
    public async Task<IActionResult> Verify([FromBody] VerifyMfaRequest request, CancellationToken ct)
    {
        var command = new VerifyMfaCommand
        {
            TenantId = request.TenantId,
            UserId = request.UserId,
            Code = request.Code,
            ChallengeToken = request.ChallengeToken,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            DeviceFingerprint = request.DeviceFingerprint
        };

        var result = await _verifyHandler.HandleAsync(command, ct);
        return result.IsSuccess ? Ok(result) : Unauthorized(result);
    }
}

public sealed record EnrollMfaRequest(Guid TenantId, Guid UserId, string Method);
public sealed record VerifyMfaRequest(Guid TenantId, Guid UserId, string Code, string ChallengeToken, string DeviceFingerprint);
