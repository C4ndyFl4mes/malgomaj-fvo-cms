namespace Server.API.Entities;

public class RoleEntity
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; } = string.Empty;
}