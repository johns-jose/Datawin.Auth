using DataWin.Auth.Application.Abstractions;
using DataWin.Auth.Application.Commands.Erasure;
using DataWin.Auth.Core.Interfaces.Services;
using DataWin.Auth.Core.ValueObjects;
using Microsoft.Extensions.Logging;

namespace DataWin.Auth.Application.Handlers.Erasure;

public sealed class RequestErasureHandler : ICommandHandler<RequestErasureCommand, Guid>
{
    private readonly IDataErasureService _erasureService;
    private readonly ILogger<RequestErasureHandler> _logger;

    public RequestErasureHandler(IDataErasureService erasureService, ILogger<RequestErasureHandler> logger)
    {
        _erasureService = erasureService;
        _logger = logger;
    }

    public async Task<Guid> HandleAsync(RequestErasureCommand command, CancellationToken ct = default)
    {
        _logger.LogWarning("Data erasure requested for user {UserId} in tenant {TenantId} from IP {IpAddress}",
            command.UserId, command.TenantId, command.IpAddress);

        await _erasureService.RequestErasureAsync(
            UuidV7.From(command.TenantId),
            UuidV7.From(command.UserId),
            command.IpAddress,
            ct);

        _logger.LogInformation("Data erasure request queued for user {UserId} in tenant {TenantId}", command.UserId, command.TenantId);

        return command.UserId;
    }
}