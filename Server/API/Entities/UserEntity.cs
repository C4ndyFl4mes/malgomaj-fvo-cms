namespace Server.API.Entities;

public class UserEntity
{
    public Guid Id { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }

    public Guid RoleId { get; set; }
    public required RoleEntity Role { get; set; }
}