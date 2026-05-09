using DataWin.Auth.Application.Abstractions;
using DataWin.Auth.Application.Commands.Auth;
using DataWin.Auth.Core.Interfaces.Repositories;
using DataWin.Auth.Core.ValueObjects;
using Microsoft.Extensions.Logging;

namespace DataWin.Auth.Application.Handlers.Auth;

public sealed class LogoutHandler : ICommandHandler<LogoutCommand, bool>
{
    private readonly ISessionRepository _sessionRepo;
    private readonly ILogger<LogoutHandler> _logger;

    public LogoutHandler(ISessionRepository sessionRepo, ILogger<LogoutHandler> logger)
    {
        _sessionRepo = sessionRepo;
        _logger = logger;
    }

    public async Task<bool> HandleAsync(LogoutCommand command, CancellationToken ct = default)
    {
        var tenantId = UuidV7.From(command.TenantId);
        var sessionId = UuidV7.From(command.SessionId);

        await _sessionRepo.RevokeAsync(tenantId, sessionId, ct);
        _logger.LogInformation("Session {SessionId} revoked for user {UserId}", command.SessionId, command.UserId);
        return true;
    }
}
