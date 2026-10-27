using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Server.API.Data;
using Server.API.Entities;
using Server.API.Exceptions;
using Server.Models;

namespace Server.API.Routes.Internal.Page.Editor;

public class EditorGetData(AppDbContext ctx, IAuthorizationService authorization)
{
    public async Task<PageEditorModel> GetAsync(ClaimsPrincipal user, Guid pageId, CancellationToken ct)
    {
        AuthorizationResult result = await authorization.AuthorizeAsync(user, "InternalUsers");
        if (!result.Succeeded)
            throw new UnauthorizedException("Du har inte tillgång till denna operation.");

        PageEntity entity = await ctx.Pages.FirstOrDefaultAsync(page => page.Id == pageId, ct) ?? new()
            {
                Id = Guid.NewGuid(),
                Title = "Namnlös sida",
                Slug = "namnlos-sida",
                MetaDescription = string.Empty,
                MetaKeywords = string.Empty,
                IsPublished = false,
                PublishedAt = null,
                SavedAt = DateTime.UtcNow,
                ContentDeltaJSON = string.Empty
            };
        
        return EditorGetMapper.ToResponse(entity);
    }
}