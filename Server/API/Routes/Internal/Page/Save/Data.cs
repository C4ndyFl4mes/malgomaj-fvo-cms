using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Server.API.Data;
using Server.API.Entities;
using Server.API.Exceptions;
using Server.Models;

namespace Server.API.Routes.Internal.Page.Save;

public class PageSaveData(AppDbContext ctx, IAuthorizationService authorization)
{
    public async Task<SavePageResponse> SaveAsync(ClaimsPrincipal user, PageEditorModel model, CancellationToken ct)
    {
        AuthorizationResult result = await authorization.AuthorizeAsync(user, "InternalUsers");
        if (!result.Succeeded)
            throw new UnauthorizedException("Du har inte tillgång till denna operation.");

        PageEntity incomingEntity = SavePageMapper.ToEntity(model);
        PageEntity? existingEntity = await ctx.Pages.FirstOrDefaultAsync(page => page.Id == incomingEntity.Id, ct);
        incomingEntity.SavedAt = DateTime.UtcNow;

        if (existingEntity is not null)
        {
            ctx.Entry(existingEntity).CurrentValues.SetValues(incomingEntity);
        }
        else
        {
            await ctx.Pages.AddAsync(incomingEntity, ct);
        }

        await ctx.SaveChangesAsync(ct);

        return SavePageMapper.ToResponse(incomingEntity);
    }
}
