using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Server.API.Data;
using Server.API.Entities;
using Server.API.Exceptions;

namespace Server.API.Routes.Internal.Page.Redact;

public class PageRedactData(AppDbContext ctx, IAuthorizationService authorization)
{
    public async Task<bool> RedactAsync(ClaimsPrincipal user, Guid pageId, CancellationToken ct)
    {
        AuthorizationResult result = await authorization.AuthorizeAsync(user, "InternalUsers");
        if (!result.Succeeded)
            throw new UnauthorizedException("Du har inte tillgång till denna operation.");

        PublishedPageEntity? existingEntity = await ctx.PublishedPages.FirstOrDefaultAsync(page => page.Id == pageId, ct) ??
            throw new NotFoundException("Sidan hittades inte bland publicerade sidor.");
        ctx.Remove(existingEntity);
        await ctx.SaveChangesAsync(ct);

        return true;
    }
}
