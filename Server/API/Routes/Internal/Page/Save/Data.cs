using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Server.API.Data;
using Server.API.Entities;
using Server.API.Exceptions;
using Server.Models;

namespace Server.API.Routes.Internal.Page.Save;

public class PageSaveData(AppDbContext ctx)
{
    public async Task<SavePageResponse> SaveAsync(PageEditorModel model, CancellationToken ct)
    {
        await using IDbContextTransaction transaction = await ctx.Database.BeginTransactionAsync(ct);

        PageEntity incomingPageEntity = SavePageMapper.ToEntity(model);

        

        if (await ctx.Pages.AnyAsync(page => page.Id == incomingPageEntity.Id, cancellationToken: ct))
        {
            PageEntity currentPage = await ctx.Pages.FirstOrDefaultAsync(page => page.Id == incomingPageEntity.Id, ct)
                ?? throw new NotFoundException("Sidan kunde inte hittas.");

            currentPage.Title = incomingPageEntity.Title;
            currentPage.IsPublished = incomingPageEntity.IsPublished;
            currentPage.MetaDescription = incomingPageEntity.MetaDescription;
            currentPage.MetaKeywords = incomingPageEntity.MetaKeywords;
            currentPage.PublishedAt = incomingPageEntity.PublishedAt;
            currentPage.Slug = incomingPageEntity.Slug;
            currentPage.SavedAt = incomingPageEntity.SavedAt;
            currentPage.ContentDeltaJSON = incomingPageEntity.ContentDeltaJSON;

            if (ctx.ChangeTracker.HasChanges())
            {
                currentPage.SavedAt = DateTime.UtcNow;
                await ctx.SaveChangesAsync(ct);
            }
        }
        else
        {
            incomingPageEntity.SavedAt = DateTime.UtcNow;
            await ctx.Pages.AddAsync(incomingPageEntity, ct);
            await ctx.SaveChangesAsync(ct);
        }

        await transaction.CommitAsync(ct);
        return SavePageMapper.ToResponse(incomingPageEntity);
    }
}