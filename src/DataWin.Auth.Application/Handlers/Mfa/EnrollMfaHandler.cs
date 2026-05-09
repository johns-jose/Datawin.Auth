using DataWin.Auth.Application.Abstractions;
using DataWin.Auth.Application.Commands.Mfa;
using DataWin.Auth.Core.Enums;
using DataWin.Auth.Core.Interfaces.Services;
using DataWin.Auth.Core.ValueObjects;
using Microsoft.Extensions.Logging;

namespace DataWin.Auth.Application.Handlers.Mfa;

public sealed class EnrollMfaHandler : ICommandHandler<EnrollMfaCommand, string>
{
    private readonly IEnumerable<IMfaProvider> _providers;
    private readonly ILogger<EnrollMfaHandler> _logger;

    public EnrollMfaHandler(IEnumerable<IMfaProvider> providers, ILogger<EnrollMfaHandler> logger)
    {
        _providers = providers;
        _logger = logger;
    }

    public async Task<string> HandleAsync(EnrollMfaCommand command, CancellationToken ct = default)
    {
        if (!Enum.TryParse<MfaMethod>(command.Method, true, out var method))
            throw new ArgumentException($"Unknown MFA method: {command.Method}");

        var provider = _providers.FirstOrDefault(p => p.Method == method)
            ?? throw new InvalidOperationException($"MFA provider for {command.Method} is not configured.");

        _logger.LogInformation("MFA enrollment started for user {UserId} in tenant {TenantId}, method {Method}",
            command.UserId, command.TenantId, command.Method);

        var result = await provider.EnrollAsync(UuidV7.From(command.TenantId), UuidV7.From(command.UserId), ct);

        _logger.LogInformation("MFA enrollment completed for user {UserId} in tenant {TenantId}, method {Method}",
            command.UserId, command.TenantId, command.Method);

        return result;
    }
}