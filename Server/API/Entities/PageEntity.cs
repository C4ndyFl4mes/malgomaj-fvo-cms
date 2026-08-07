namespace Server.API.Entities;

public class PageEntity
{
    public Guid Id { get; set; }
    public required bool IsPublished { get; set; }
    public required DateTime SavedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public required string Title { get; set; }
    public required string Content { get; set; }
    public required string Slug { get; set; }
    public required string MetaDescription { get; set; }
    public required string MetaKeywords { get; set; }
}