using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace DataWin.Auth.Sdk;

public sealed class TokenValidationHandler : DelegatingHandler
{
    private readonly TokenValidationParameters _validationParameters;

    public TokenValidationHandler(string signingKey, string issuer)
    {
        _validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var authHeader = request.Headers.Authorization;
        if (authHeader?.Scheme == "Bearer" && authHeader.Parameter is not null)
        {
            var handler = new JwtSecurityTokenHandler();
            try
            {
                handler.ValidateToken(authHeader.Parameter, _validationParameters, out _);
            }
            catch (SecurityTokenException)
            {
                return new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized);
            }
        }

        return await base.SendAsync(request, ct);
    }
}
