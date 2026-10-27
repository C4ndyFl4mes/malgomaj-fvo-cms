using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Server.API.Data;
using Server.API.Entities;
using Server.API.Exceptions;

namespace Server.API.Routes.Internal.Page.List;

public class PageListGetData(AppDbContext ctx, IAuthorizationService authorization)
{
    public async Task<PageListGetResponse> GetListAsync(ClaimsPrincipal user, CancellationToken ct)
    {
        AuthorizationResult result = await authorization.AuthorizeAsync(user, "InternalUsers");
        if (!result.Succeeded)
            throw new UnauthorizedException("Du har inte tillgång till denna operation.");
        
        List<PageEntity> pages = await ctx.Pages.ToListAsync(ct);

        return PageListGetMapper.ToResponse(pages);
    }
}