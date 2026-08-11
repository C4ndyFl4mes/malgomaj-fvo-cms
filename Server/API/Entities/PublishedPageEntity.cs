namespace Server.API.Entities;

public class PublishedPageEntity
{
    public Guid Id { get; set; }
    public DateTime? PublishedAt { get; set; }

    public required string Title { get; set; }
    public required string ContentHTML { get; set; }
    public required string Slug { get; set; }
    public required string MetaDescription { get; set; }
    public required string MetaKeywords { get; set; }
}