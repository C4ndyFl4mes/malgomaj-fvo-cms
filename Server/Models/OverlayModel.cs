namespace Server.Models;

public record OverlayModel
{
    public required Guid Id { get; init; }
    public required Type ComponentType { get; init; }
    public IDictionary<string, object> Parameters { get; init; } = new Dictionary<string, object>();
}