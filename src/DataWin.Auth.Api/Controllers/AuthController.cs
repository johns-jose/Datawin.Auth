using DataWin.Auth.Application.Abstractions;
using DataWin.Auth.Application.Commands.Auth;
using DataWin.Auth.Application.DTOs;
using DataWin.Auth.Application.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DataWin.Auth.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly ICommandHandler<RegisterUserCommand, RegisterUserResultDto> _registerHandler;
    private readonly ICommandHandler<LoginCommand, AuthResultDto> _loginHandler;
    private readonly ICommandHandler<LogoutCommand, bool> _logoutHandler;
    private readonly ICommandHandler<RefreshTokenCommand, TokenResponseDto> _refreshHandler;
    private readonly ICommandHandler<ExternalLoginCommand, AuthResultDto> _externalLoginHandler;

    public AuthController(
        ICommandHandler<RegisterUserCommand, RegisterUserResultDto> registerHandler,
        ICommandHandler<LoginCommand, AuthResultDto> loginHandler,
        ICommandHandler<LogoutCommand, bool> logoutHandler,
        ICommandHandler<RefreshTokenCommand, TokenResponseDto> refreshHandler,
        ICommandHandler<ExternalLoginCommand, AuthResultDto> externalLoginHandler)
    {
        _registerHandler = registerHandler;
        _loginHandler = loginHandler;
        _logoutHandler = logoutHandler;
        _refreshHandler = refreshHandler;
        _externalLoginHandler = externalLoginHandler;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var command = new RegisterUserCommand
        {
            TenantId = request.TenantId,
            Email = request.Email,
            Password = request.Password,
            DisplayName = request.DisplayName
        };

        var result = await _registerHandler.HandleAsync(command, ct);
        return result.IsSuccess ? Created($"/api/user/profile?tenantId={request.TenantId}&userId={result.UserId}", result) : Conflict(result);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var command = new LoginCommand
        {
            TenantId = request.TenantId,
            Email = request.Email,
            Password = request.Password,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            DeviceFingerprint = request.DeviceFingerprint,
            UserAgent = Request.Headers.UserAgent.ToString(),
            Audience = request.Audience
        };

        var result = await _loginHandler.HandleAsync(command, ct);
        return result.IsSuccess ? Ok(result) : Unauthorized(result);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request, CancellationToken ct)
    {
        var command = new LogoutCommand
        {
            TenantId = request.TenantId,
            UserId = request.UserId,
            SessionId = request.SessionId
        };

        await _logoutHandler.HandleAsync(command, ct);
        return NoContent();
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request, CancellationToken ct)
    {
        var command = new RefreshTokenCommand
        {
            TenantId = request.TenantId,
            RefreshToken = request.RefreshToken,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"
        };

        var result = await _refreshHandler.HandleAsync(command, ct);
        return Ok(result);
    }

    [HttpPost("external")]
    [AllowAnonymous]
    public async Task<IActionResult> ExternalLogin([FromBody] ExternalLoginRequest request, CancellationToken ct)
    {
        var command = new ExternalLoginCommand
        {
            TenantId = request.TenantId,
            Provider = request.Provider,
            Code = request.Code,
            RedirectUri = request.RedirectUri,
            State = request.State,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            DeviceFingerprint = request.DeviceFingerprint
        };

        var result = await _externalLoginHandler.HandleAsync(command, ct);
        return result.IsSuccess ? Ok(result) : Unauthorized(result);
    }
}

public sealed record RegisterRequest(Guid TenantId, string Email, string Password, string? DisplayName);
public sealed record LoginRequest(Guid TenantId, string Email, string Password, string DeviceFingerprint, string? Audience);
public sealed record LogoutRequest(Guid TenantId, Guid UserId, Guid SessionId);
public sealed record RefreshRequest(Guid TenantId, string RefreshToken);
public sealed record ExternalLoginRequest(Guid TenantId, string Provider, string Code, string RedirectUri, string? State, string DeviceFingerprint);
