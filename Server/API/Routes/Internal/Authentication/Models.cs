using System.Text.Json.Serialization;
using FastEndpoints;
using Server.API.Exceptions;

namespace Server.API.Routes.Internal.Authentication;

public record UserModel
{
    public required string Email { get; set; }
    public required string Password { get; set; }
}

public record Token
{
    public required string AccessToken { get; set; }
    public required string RefreshToken { get; set; }
}

public record AuthenticationResponse
{
    public required string Message { get; set; }
}

public record ClientCallResponse<T>
{
    public bool IsSuccess { get; set; }
    public T? Data { get; set; }
    public ExceptionResponse? Error { get; set; }
}