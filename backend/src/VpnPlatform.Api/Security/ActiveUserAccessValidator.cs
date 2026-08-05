using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Domain.Enums;

namespace VpnPlatform.Api.Security;

public static class ActiveUserAccessValidator
{
    public static async Task ValidateAsync(TokenValidatedContext context)
    {
        var db = context.HttpContext.RequestServices.GetRequiredService<IApplicationDbContext>();
        if (!await IsActiveAsync(context.Principal, db, context.HttpContext.RequestAborted))
        {
            context.Fail("user_not_active");
        }
    }

    public static async Task<bool> IsActiveAsync(ClaimsPrincipal? principal, IApplicationDbContext db, CancellationToken cancellationToken)
    {
        var subject = principal?.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal?.FindFirstValue("sub");
        if (!Guid.TryParse(subject, out var userId) || userId == Guid.Empty)
        {
            return false;
        }

        return await db.Users
            .AsNoTracking()
            .AnyAsync(x => x.Id == userId && !x.IsBlocked && x.Status == UserStatus.Active, cancellationToken);
    }
}
