using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DataWin.Auth.Api.Filters;

public sealed class RequireMfaFilter : IAsyncAuthorizationFilter
{
    public Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var mfaClaim = context.HttpContext.User.FindFirst("mfa_verified");
        if (mfaClaim is null || !bool.TryParse(mfaClaim.Value, out var verified) || !verified)
        {
            context.Result = new ForbidResult();
        }
        return Task.CompletedTask;
    }
}
