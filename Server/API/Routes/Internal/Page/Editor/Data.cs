using Microsoft.EntityFrameworkCore;
using Server.API.Data;
using Server.API.Entities;
using Server.API.Exceptions;
using Server.Models;

namespace Server.API.Routes.Internal.Page.Editor;

public class EditorGetData(AppDbContext ctx)
{
    public async Task<PageEditorModel> GetAsync(Guid pageId, CancellationToken ct)
    {
        PageEntity entity = await ctx.Pages.FirstOrDefaultAsync(page => page.Id == pageId, ct) ??
            throw new NotFoundException("Hittade inte sidan.");
        
        return EditorGetMapper.ToResponse(entity);
    }
}