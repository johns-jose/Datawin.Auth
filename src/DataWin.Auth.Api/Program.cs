using System.Text;
using DataWin.Auth.Api.Middleware;
using DataWin.Auth.Application.Abstractions;
using DataWin.Auth.Application.Commands.Auth;
using DataWin.Auth.Application.Commands.Consent;
using DataWin.Auth.Application.Commands.Erasure;
using DataWin.Auth.Application.Commands.Mfa;
using DataWin.Auth.Application.Commands.Tenant;
using DataWin.Auth.Application.DTOs;
using DataWin.Auth.Application.Handlers.Auth;
using DataWin.Auth.Application.Handlers.Consent;
using DataWin.Auth.Application.Handlers.Erasure;
using DataWin.Auth.Application.Handlers.Mfa;
using DataWin.Auth.Application.Handlers.Tenant;
using DataWin.Auth.Application.Queries;
using DataWin.Auth.Core.Interfaces;
using DataWin.Auth.Infrastructure;
using DataWin.Auth.Infrastructure.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Structured logging with scopes enabled
builder.Logging.ClearProviders();
builder.Logging.AddConsole(options => options.FormatterName = "simple")
    .AddSimpleConsole(options =>
    {
        options.IncludeScopes = true;
        options.TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff ";
    });

// JWT settings
var jwtSettings = new JwtTokenSettings
{
    SigningKey = builder.Configuration["Jwt:SigningKey"] ?? "DataWin-Auth-Default-Signing-Key-Change-In-Production-256bit!",
    Issuer = builder.Configuration["Jwt:Issuer"] ?? "https://auth.datawin.io",
    AccessTokenLifetimeSeconds = int.TryParse(builder.Configuration["Jwt:AccessTokenLifetimeSeconds"], out var ttl) ? ttl : 900
};

// Infrastructure
var globalConnectionString = builder.Configuration.GetConnectionString("GlobalDb")
    ?? "Host=localhost;Database=datawin_auth_global;Username=datawin;Password=datawin";

builder.Services.AddDataWinAuthInfrastructure(globalConnectionString, jwtSettings);

// Application handlers
builder.Services.AddScoped<ICommandHandler<RegisterUserCommand, RegisterUserResultDto>, RegisterUserHandler>();
builder.Services.AddScoped<ICommandHandler<LoginCommand, AuthResultDto>, LoginHandler>();
builder.Services.AddScoped<ICommandHandler<LogoutCommand, bool>, LogoutHandler>();
builder.Services.AddScoped<ICommandHandler<RefreshTokenCommand, TokenResponseDto>, RefreshTokenHandler>();
builder.Services.AddScoped<ICommandHandler<ExternalLoginCommand, AuthResultDto>, ExternalLoginHandler>();
builder.Services.AddScoped<ICommandHandler<EnrollMfaCommand, string>, EnrollMfaHandler>();
builder.Services.AddScoped<ICommandHandler<VerifyMfaCommand, AuthResultDto>, VerifyMfaHandler>();
builder.Services.AddScoped<ICommandHandler<GrantConsentCommand, bool>, GrantConsentHandler>();
builder.Services.AddScoped<ICommandHandler<WithdrawConsentCommand, bool>, WithdrawConsentHandler>();
builder.Services.AddScoped<ICommandHandler<RequestErasureCommand, Guid>, RequestErasureHandler>();
builder.Services.AddScoped<ICommandHandler<OnboardTenantCommand, TenantDto>, OnboardTenantHandler>();
builder.Services.AddScoped<IQueryHandler<GetUserProfileQuery, UserProfileDto?>, GetUserProfileHandler>();
builder.Services.AddScoped<IQueryHandler<GetConsentStatusQuery, ConsentStatusDto>, GetConsentStatusHandler>();

// Tenant context
builder.Services.AddScoped<MutableTenantContext>();
builder.Services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<MutableTenantContext>());

// Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SigningKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

// Middleware pipeline â€” order matters
// 1. Correlation ID (outermost â€” generates/reads X-Correlation-Id, echoes on response)
app.UseMiddleware<CorrelationIdMiddleware>();

// 2. Global exception handler (catches all unhandled exceptions, logs with correlation)
app.UseMiddleware<GlobalExceptionMiddleware>();

// 3. Tenant resolution (reads X-Tenant-Id, populates MutableTenantContext)
app.UseMiddleware<TenantResolutionMiddleware>();

// 4. Regional routing (sets RegionCode in HttpContext.Items)
app.UseMiddleware<RegionalRoutingMiddleware>();

// 5. Request logging (structured scope: CorrelationId, TenantId, UserId, ClientIp, timing)
app.UseMiddleware<RequestLoggingMiddleware>();

// 6. PII audit logging (logs access to PII-sensitive endpoints)
app.UseMiddleware<PiiAuditMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }