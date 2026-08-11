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