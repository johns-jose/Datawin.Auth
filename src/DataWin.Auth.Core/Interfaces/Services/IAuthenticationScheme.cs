using DataWin.Auth.Core.Enums;

namespace DataWin.Auth.Core.Interfaces.Services;

public interface IAuthenticationScheme
{
    AuthSchemeType SchemeType { get; }
    Task<AuthenticationResult> AuthenticateAsync(AuthenticationRequest request, CancellationToken ct = default);
}

public sealed record AuthenticationRequest
{
    public required string TenantSlug { get; init; }
    public string? Code { get; init; }
    public string? RedirectUri { get; init; }
    public string? State { get; init; }
    public string? SamlResponse { get; init; }
    public string? Username { get; init; }
    public string? Password { get; init; }
}

public sealed record AuthenticationResult
{
    public bool IsSuccess { get; init; }
    public string? ExternalUserId { get; init; }
    public string? Email { get; init; }
    public string? DisplayName { get; init; }
    public string? ErrorMessage { get; init; }
    public IDictionary<string, string>? Claims { get; init; }

    public static AuthenticationResult Success(string externalUserId, string email, string? displayName = null, IDictionary<string, string>? claims = null)
        => new() { IsSuccess = true, ExternalUserId = externalUserId, Email = email, DisplayName = displayName, Claims = claims };

    public static AuthenticationResult Failure(string error)
        => new() { IsSuccess = false, ErrorMessage = error };
}
