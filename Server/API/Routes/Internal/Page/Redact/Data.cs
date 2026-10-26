using Microsoft.EntityFrameworkCore;
using Server.API.Data;
using Server.API.Entities;
using Server.API.Exceptions;

namespace Server.API.Routes.Internal.Page.Redact;

public class PageRedactData(AppDbContext ctx)
{
    public async Task<bool> RedactAsync(Guid pageId, CancellationToken ct)
    {
        PublishedPageEntity? existingEntity = await ctx.PublishedPages.FirstOrDefaultAsync(page => page.Id == pageId, ct) ??
            throw new NotFoundException("Sidan hittades inte bland publicerade sidor.");
        ctx.Remove(existingEntity);
        await ctx.SaveChangesAsync(ct);

        return true;
    }
}
