using System.Net.Http.Headers;
using System.Net.Http.Json;
using DataWin.Auth.Contracts.Models;

namespace DataWin.Auth.Sdk;

public sealed class DataWinAuthClient
{
    private readonly HttpClient _httpClient;

    public DataWinAuthClient(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<TokenResponse?> LoginAsync(Guid tenantId, string email, string password, string deviceFingerprint, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/login", new
        {
            TenantId = tenantId,
            Email = email,
            Password = password,
            DeviceFingerprint = deviceFingerprint
        }, ct);

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TokenResponse>(ct);
    }

    public async Task<TokenResponse?> RefreshAsync(Guid tenantId, string refreshToken, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/refresh", new
        {
            TenantId = tenantId,
            RefreshToken = refreshToken
        }, ct);

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TokenResponse>(ct);
    }

    public async Task LogoutAsync(Guid tenantId, Guid userId, Guid sessionId, string accessToken, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/auth/logout");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Content = JsonContent.Create(new { TenantId = tenantId, UserId = userId, SessionId = sessionId });

        var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<AuthenticatedUser?> GetProfileAsync(Guid tenantId, Guid userId, string accessToken, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"api/user/profile?tenantId={tenantId}&userId={userId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AuthenticatedUser>(ct);
    }
}
