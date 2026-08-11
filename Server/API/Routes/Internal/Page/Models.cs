namespace Server.API.Routes.Internal.Page;

public record EditorGetRequest
{
    public required Guid PageId { get; set; }
}

public record SavePageResponse
{
    public required string Message { get; set; }
    public required bool IsPublished { get; set; }
    public required DateTime SavedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
}

public record PageListGetResponse
{
    public required List<PageItem> PageItems { get; set; }
}

public record PageItem
{
    public required Guid PageId { get; set; }
    public required string Title { get; set; }
    public required string Slug { get; set; }
    public required bool IsPublished { get; set; }
    public DateTime? PublishedAt { get; set; }
    public required DateTime SavedAt { get; set; }
}