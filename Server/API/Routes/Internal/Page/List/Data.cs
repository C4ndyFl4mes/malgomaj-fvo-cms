using Microsoft.EntityFrameworkCore;
using Server.API.Data;
using Server.API.Entities;

namespace Server.API.Routes.Internal.Page.List;

public class PageListGetData(AppDbContext ctx)
{
    public async Task<PageListGetResponse> GetListAsync(CancellationToken ct)
    {
        List<PageEntity> pages = await ctx.Pages.ToListAsync(ct);

        return PageListGetMapper.ToResponse(pages);
    }
}