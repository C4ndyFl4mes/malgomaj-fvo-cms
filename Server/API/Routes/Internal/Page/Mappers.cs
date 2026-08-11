using Server.API.Entities;
using Server.Models;

namespace Server.API.Routes.Internal.Page;

public static class EditorGetMapper
{
    public static PageEditorModel ToResponse(PageEntity entity)
    {
        return new PageEditorModel
        {
            Id = entity.Id,
            Meta = new PageMetaModel
            {
                Id = entity.Id,
                IsPublished = entity.IsPublished,
                PublishedAt = entity.PublishedAt,
                SavedAt = entity.SavedAt,
                Title = entity.Title,
                Slug = entity.Slug,
                Keywords = entity.MetaKeywords,
                Description = entity.MetaDescription
            },
            ContentDeltaJSON = entity.ContentDeltaJSON
        };
    }
}

public static class SavePageMapper
{
    public static PageEntity ToEntity(PageEditorModel model)
    {
        return new PageEntity
        {
            Id = model.Id,
            IsPublished = model.Meta.IsPublished,
            SavedAt = model.Meta.SavedAt,
            Title = model.Meta.Title,
            Slug = model.Meta.Slug,
            MetaKeywords = model.Meta.Keywords ?? string.Empty,
            MetaDescription = model.Meta.Description ?? string.Empty,
            ContentDeltaJSON = model.ContentDeltaJSON
        };
    }

    public static SavePageResponse ToResponse(PageEntity entity)
    {
        return new SavePageResponse
        {
            Message = "Sidan har sparats.",
            IsPublished = entity.IsPublished,
            PublishedAt = entity.PublishedAt,
            SavedAt = entity.SavedAt
        };
    }
}

public static class PageListGetMapper
{
    public static PageListGetResponse ToResponse(List<PageEntity> entities)
    {
        return new PageListGetResponse
        {
            PageItems = entities.Select(entity => new PageItem
            {
                PageId = entity.Id,
                Title = entity.Title,
                Slug = entity.Slug,
                IsPublished = entity.IsPublished,
                PublishedAt = entity.PublishedAt,
                SavedAt = entity.SavedAt
            }).ToList()
        };
    }
}