using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Server.API.Data;
using Server.API.Entities;
using Server.API.Exceptions;
using Server.Models;
using Server.Services;

namespace Server.API.Routes.Internal.Page.Publish;

public class PagePublishData(AppDbContext ctx, IAuthorizationService authorization)
{
    public async Task<DateTime?> PublishAsync(ClaimsPrincipal user, PageEditorModel model, CancellationToken ct)
    {
        AuthorizationResult result = await authorization.AuthorizeAsync(user, "InternalUsers");
        if (!result.Succeeded)
            throw new UnauthorizedException("Du har inte tillgång till denna operation.");

        if (!DeltaToNestedNormalizationService.IsCorrectlyNormalized(model.ContentJSON))
            throw new BadRequestException("Innehållet är inte korrekt formaterad.");

        if (!DeltaToNestedNormalizationService.IsNotNullOrEmpty(model.ContentJSON))
            throw new BadRequestException("Sidan är tom.");

        DateTime publishedAt = DateTime.UtcNow;
        PublishedPageEntity incomingEntity = PublishPageMapper.ToEntity(model);
        PublishedPageEntity? existingEntity = await ctx.PublishedPages.FirstOrDefaultAsync(page => page.Id == incomingEntity.Id, ct);
        incomingEntity.PublishedAt = publishedAt;

        if (existingEntity is not null)
        {
            ctx.Entry(existingEntity).CurrentValues.SetValues(incomingEntity);
        }
        else
        {
            await ctx.PublishedPages.AddAsync(incomingEntity, ct);
        }

        await ctx.SaveChangesAsync(ct);

        return publishedAt;
    }
}

