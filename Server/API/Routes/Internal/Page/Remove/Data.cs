using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Server.API.Data;
using Server.API.Entities;
using Server.API.Exceptions;

namespace Server.API.Routes.Internal.Page.Remove;

public class PageRemoveData(AppDbContext ctx, IAuthorizationService authorization)
{
    public async Task RemoveAsync(ClaimsPrincipal user, Guid pageId, CancellationToken ct)
    {
        AuthorizationResult result = await authorization.AuthorizeAsync(user, "InternalUsers");
        if (!result.Succeeded)
            throw new UnauthorizedException("Du har inte tillgång till denna operation.");

        PageEntity existingEntity = await ctx.Pages.FirstOrDefaultAsync(page => page.Id == pageId, ct) ??
            throw new NotFoundException("Sidan hittades inte. Har sidan blivit sparad tidigare?");
        
        PublishedPageEntity? existingPublishedEntity = null;

        if (existingEntity.IsPublished)
        {
            existingPublishedEntity = await ctx.PublishedPages.FirstOrDefaultAsync(page => pageId == page.Id, ct);
            
            if (existingPublishedEntity is not null)
                ctx.Remove(existingPublishedEntity);
        }

        ctx.Remove(existingEntity);
        await ctx.SaveChangesAsync(ct);
    }
}