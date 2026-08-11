namespace Server.Models;

public record PageMetaModel
{
    public required Guid Id { get; set; }
    public required string Title { get; set; } = "Namnlös sida";
    public required string Slug { get; set; } = "namnlös-sida";
    public string? Keywords { get; set; } // Separated by commas.
    public string? Description { get; set; }
    public required bool IsPublished { get; set; } = false;
    public DateTime? PublishedAt { get; set; }
    public required DateTime SavedAt { get; set; }
}