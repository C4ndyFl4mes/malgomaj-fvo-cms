namespace Server.Models;

public record PageEditorModel
{
    public required Guid Id { get; set; }
    public required PageMetaModel Meta { get; set; }
    public required string ContentDeltaJSON { get; set; }
}