using DataWin.Auth.Application.Abstractions;
using DataWin.Auth.Application.DTOs;
using DataWin.Auth.Application.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DataWin.Auth.Api.Controllers;

[ApiController]
[Route("api/user")]
[Authorize]
public sealed class UserController : ControllerBase
{
    private readonly IQueryHandler<GetUserProfileQuery, UserProfileDto?> _profileHandler;
    private readonly IQueryHandler<ExportUserPiiQuery, UserPiiExportDto> _exportHandler;

    public UserController(
        IQueryHandler<GetUserProfileQuery, UserProfileDto?> profileHandler,
        IQueryHandler<ExportUserPiiQuery, UserPiiExportDto> exportHandler)
    {
        _profileHandler = profileHandler;
        _exportHandler = exportHandler;
    }

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile([FromQuery] Guid tenantId, [FromQuery] Guid userId, CancellationToken ct)
    {
        var query = new GetUserProfileQuery { TenantId = tenantId, UserId = userId };
        var result = await _profileHandler.HandleAsync(query, ct);
        return result is not null ? Ok(result) : NotFound();
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export([FromQuery] Guid tenantId, [FromQuery] Guid userId, CancellationToken ct)
    {
        var query = new ExportUserPiiQuery { TenantId = tenantId, UserId = userId };
        var result = await _exportHandler.HandleAsync(query, ct);
        return Ok(result);
    }
}
