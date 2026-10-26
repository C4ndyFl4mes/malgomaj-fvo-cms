using Microsoft.EntityFrameworkCore;
using Server.API.Data;
using Server.API.Entities;
using Server.Models;

namespace Server.API.Routes.Internal.Page.Save;

public class PageSaveData(AppDbContext ctx)
{
    public async Task<SavePageResponse> SaveAsync(PageEditorModel model, CancellationToken ct)
    {
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
