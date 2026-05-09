# DataWin.Auth - SDK Integration Guide

## Overview

The DataWin.Auth.Sdk is a .NET NuGet package that provides a typed HTTP client for
integrating DataWin products with the authentication API. Products should NEVER call
the REST API directly - always use the SDK.

---

## Installation

Add the SDK to your DataWin product project:

    dotnet add package DataWin.Auth.Sdk
    dotnet add package DataWin.Auth.Contracts

---

## Quick Start

### 1. Register the SDK in Program.cs

    using DataWin.Auth.Sdk;

    var builder = WebApplication.CreateBuilder(args);

    // Register DataWin.Auth SDK
    builder.Services.AddDataWinAuthSdk("https://auth.datawin.io");

    var app = builder.Build();

### 2. Inject and Use the Client

    using DataWin.Auth.Sdk;
    using DataWin.Auth.Contracts.Models;

    public class MyService
    {
        private readonly DataWinAuthClient _authClient;

        public MyService(DataWinAuthClient authClient)
        {
            _authClient = authClient;
        }

        public async Task<TokenResponse?> AuthenticateUser(
            Guid tenantId, string email, string password)
        {
            return await _authClient.LoginAsync(
                tenantId,
                email,
                password,
                deviceFingerprint: GenerateFingerprint());
        }
    }

---

## SDK Methods Reference

### RegisterAsync

Register a new user account within a tenant.

    Task<RegisterResult?> RegisterAsync(
        Guid tenantId,
        string email,
        string password,
        string? displayName = null,
        CancellationToken ct = default)

Returns a RegisterResult containing the new user's ID on success.

Example:

    var result = await authClient.RegisterAsync(
        tenantId: Guid.Parse("019568b0-0002-7000-8000-000000000002"),
        email: "jane.doe@acme-corp.com",
        password: "P@ssw0rd!2025",
        displayName: "Jane Doe");

    Console.WriteLine($"User created: {result.UserId}");

### LoginAsync

Authenticate a user with email and password.

    Task<TokenResponse?> LoginAsync(
        Guid tenantId,
        string email,
        string password,
        string deviceFingerprint,
        CancellationToken ct = default)

Returns a TokenResponse on success. If MFA is required, the response will indicate
that (check the HTTP response before deserialization).

Example:

    var tokens = await authClient.LoginAsync(
        tenantId: Guid.Parse("019568b0-0002-7000-8000-000000000002"),
        email: "john.doe@acme-corp.com",
        password: "P@ssw0rd!2025",
        deviceFingerprint: "fp-my-app-instance-001");

    Console.WriteLine($"Access Token: {tokens.AccessToken}");
    Console.WriteLine($"Expires in: {tokens.ExpiresIn} seconds");

### RefreshAsync

Exchange a refresh token for new tokens.

    Task<TokenResponse?> RefreshAsync(
        Guid tenantId,
        string refreshToken,
        CancellationToken ct = default)

IMPORTANT: Store the new refresh token from the response. The old one is now invalid.

Example:

    var newTokens = await authClient.RefreshAsync(
        tenantId: tenantId,
        refreshToken: currentRefreshToken);

    // Update stored tokens
    currentAccessToken = newTokens.AccessToken;
    currentRefreshToken = newTokens.RefreshToken;

### LogoutAsync

Revoke a session and all its refresh tokens.

    Task LogoutAsync(
        Guid tenantId,
        Guid userId,
        Guid sessionId,
        string accessToken,
        CancellationToken ct = default)

Example:

    await authClient.LogoutAsync(
        tenantId: tenantId,
        userId: userId,
        sessionId: sessionId,
        accessToken: currentAccessToken);

### GetProfileAsync

Retrieve a user's profile information.

    Task<AuthenticatedUser?> GetProfileAsync(
        Guid tenantId,
        Guid userId,
        string accessToken,
        CancellationToken ct = default)

Example:

    var profile = await authClient.GetProfileAsync(
        tenantId: tenantId,
        userId: userId,
        accessToken: currentAccessToken);

    Console.WriteLine($"Name: {profile.DisplayName}");
    Console.WriteLine($"Region: {profile.Region}");

---

## Contract Models

### TokenResponse

    public sealed record TokenResponse
    {
        public required string AccessToken { get; init; }
        public required string RefreshToken { get; init; }
        public required string TokenType { get; init; }    // Always "Bearer"
        public required int ExpiresIn { get; init; }       // Seconds
        public string? IdToken { get; init; }              // OIDC flows only
    }

### AuthenticatedUser

    public sealed record AuthenticatedUser
    {
        public required Guid UserId { get; init; }
        public required Guid TenantId { get; init; }
        public required string Email { get; init; }
        public string? DisplayName { get; init; }
        public required string Region { get; init; }
        public required IReadOnlyList<string> Roles { get; init; }
        public required bool MfaVerified { get; init; }
    }

### TenantContext

    public sealed record TenantContext
    {
        public required Guid TenantId { get; init; }
        public required string Slug { get; init; }
        public required string PrimaryRegion { get; init; }
        public required IReadOnlyList<string> Regions { get; init; }
    }

---

## Common Integration Patterns

### Pattern 1: Login with MFA Flow

    // Step 1: Attempt login
    var loginResponse = await httpClient.PostAsJsonAsync("api/auth/login", new
    {
        TenantId = tenantId,
        Email = email,
        Password = password,
        DeviceFingerprint = fingerprint
    });

    var result = await loginResponse.Content.ReadFromJsonAsync<AuthResult>();

    if (result.RequiresMfa)
    {
        // Step 2: Prompt user for MFA code
        var mfaCode = PromptUserForCode();

        // Step 3: Verify MFA
        var mfaResponse = await httpClient.PostAsJsonAsync("api/mfa/verify", new
        {
            TenantId = tenantId,
            UserId = result.UserId,
            Code = mfaCode,
            Method = "totp",
            ChallengeToken = result.MfaChallengeToken
        });

        result = await mfaResponse.Content.ReadFromJsonAsync<AuthResult>();
    }

    // Step 4: Store tokens
    StoreTokens(result.AccessToken, result.RefreshToken);

### Pattern 2: Automatic Token Refresh

Implement a background timer or middleware that refreshes the access token before it expires:

    public class TokenRefreshService : BackgroundService
    {
        private readonly DataWinAuthClient _authClient;
        private readonly ITokenStore _tokenStore;

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                var currentToken = _tokenStore.GetAccessToken();
                if (IsExpiringSoon(currentToken, thresholdSeconds: 120))
                {
                    var newTokens = await _authClient.RefreshAsync(
                        _tokenStore.TenantId,
                        _tokenStore.GetRefreshToken(),
                        ct);

                    _tokenStore.Update(newTokens.AccessToken, newTokens.RefreshToken);
                }

                await Task.Delay(TimeSpan.FromSeconds(30), ct);
            }
        }

        private bool IsExpiringSoon(string jwt, int thresholdSeconds)
        {
            // Decode JWT exp claim and compare to DateTime.UtcNow
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(jwt);
            var exp = token.ValidTo;
            return (exp - DateTime.UtcNow).TotalSeconds < thresholdSeconds;
        }
    }

### Pattern 3: Protecting API Endpoints in Your Product

Use the standard ASP.NET JWT middleware to validate DataWin.Auth tokens in your own API:

    // In your product's Program.cs
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(signingKey)), // Same key as DataWin.Auth
                ValidateIssuer = true,
                ValidIssuer = "https://auth.datawin.io",
                ValidateAudience = false,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30)
            };
        });

Then in your controllers:

    [ApiController]
    [Route("api/orders")]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetOrders()
        {
            // Extract user info from JWT claims
            var userId = User.FindFirst("sub")?.Value;
            var tenantId = User.FindFirst("tid")?.Value;
            var region = User.FindFirst("region")?.Value;
            var mfaVerified = User.FindFirst("mfa_verified")?.Value == "true";

            // Your product logic here
            return Ok(new { userId, tenantId, region });
        }
    }

### Pattern 4: Consent-Aware Features

Before using a feature that requires consent (e.g., analytics), check consent status:

    public class AnalyticsService
    {
        private readonly HttpClient _httpClient;

        public async Task<bool> CanCollectAnalyticsAsync(
            Guid tenantId, Guid userId, string accessToken)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get,
                $"api/consent/status?tenantId={tenantId}&userId={userId}");
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request);
            var status = await response.Content
                .ReadFromJsonAsync<ConsentStatusResponse>();

            return status.Consents
                .Any(c => c.Purpose == "analytics" && c.IsGranted);
        }
    }

### Pattern 5: Registration + Login Flow

    // Step 1: Register the user
    var registerResult = await httpClient.PostAsJsonAsync("api/auth/register", new
    {
        TenantId = tenantId,
        Email = email,
        Password = password,
        DisplayName = displayName
    });

    if (registerResult.StatusCode == HttpStatusCode.Conflict)
    {
        // Email already registered — prompt to login instead
    }

    var registration = await registerResult.Content
        .ReadFromJsonAsync<RegisterResult>();

    // Step 2: Immediately log in with the same credentials
    var loginResult = await httpClient.PostAsJsonAsync("api/auth/login", new
    {
        TenantId = tenantId,
        Email = email,
        Password = password,
        DeviceFingerprint = fingerprint
    });

    var tokens = await loginResult.Content.ReadFromJsonAsync<AuthResult>();
    StoreTokens(tokens.AccessToken, tokens.RefreshToken);

### Pattern 6: Handling External IdP Login

    // Step 1: Redirect user to provider's authorization URL (client-side)
    // Google example:
    // https://accounts.google.com/o/oauth2/v2/auth?
    //   client_id=YOUR_CLIENT_ID&
    //   redirect_uri=https://app.acme-corp.com/auth/callback&
    //   response_type=code&
    //   scope=openid%20email%20profile&
    //   state=random-state

    // Step 2: After redirect, exchange code via DataWin.Auth
    var response = await httpClient.PostAsJsonAsync("api/auth/external", new
    {
        TenantId = tenantId,
        Provider = "google",
        Code = authorizationCode,       // From query string
        RedirectUri = "https://app.acme-corp.com/auth/callback",
        State = state,
        DeviceFingerprint = fingerprint
    });

    var result = await response.Content.ReadFromJsonAsync<AuthResult>();
    // result contains accessToken + refreshToken

---

## Error Handling

The SDK throws HttpRequestException on non-success status codes. Wrap calls in try-catch:

    try
    {
        var tokens = await authClient.LoginAsync(tenantId, email, password, fingerprint);
        // Success
    }
    catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
    {
        // Invalid credentials or account locked
    }
    catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.BadRequest)
    {
        // Validation error
    }

---

## Configuration Reference

### appsettings.json for SDK consumers

    {
      "DataWinAuth": {
        "BaseUrl": "https://auth.datawin.io",
        "TenantId": "019568b0-0002-7000-8000-000000000002",
        "SigningKey": "your-shared-jwt-signing-key"
      }
    }

### Environment-Specific Base URLs

| Environment | Base URL |
|-------------|----------|
| Production | https://auth.datawin.io |
| Staging | https://auth-staging.datawin.io |
| Development | https://localhost:5001 |

---

## Troubleshooting

### "401 Unauthorized" on all requests
- Verify the access token has not expired (check exp claim)
- Ensure the JWT signing key matches between your product and DataWin.Auth
- Check that the Authorization header format is "Bearer {token}" (with space)

### "400 Bad Request" with tenant_not_found
- Verify the tenantId is correct and the tenant is Active (not Suspended/Deactivated)
- Check the X-Tenant-Id header is set if not passing tenantId in the body

### Refresh token returns "invalid_token"
- The refresh token may have already been rotated (each token is single-use)
- The session may have been revoked (e.g., user logged out from another device)
- Store and use only the LATEST refresh token from the most recent response

### MFA flow not completing
- The MFA challenge token expires after 5 minutes
- Ensure the method parameter matches the enrolled method exactly
- TOTP codes are valid for 30 seconds; verify device clock is synchronized