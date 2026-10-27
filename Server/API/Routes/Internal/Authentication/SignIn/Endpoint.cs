using FastEndpoints;

namespace Server.API.Routes.Internal.Authentication.SignIn;

public class SignInEndpoint(SignInData data) : Endpoint<UserModel, AuthenticationResponse>
{
    public override void Configure()
    {
        Post("/api/auth/signin");
        AllowAnonymous();
    }

    public override async Task<AuthenticationResponse> ExecuteAsync(UserModel model, CancellationToken ct)
    {
        Token token = await data.SignIn(model, ct);

        HttpContext.Response.Cookies.Append("accessToken", token.AccessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddMinutes(12),
            Path = "/"
        });

        HttpContext.Response.Cookies.Append("refreshToken", token.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddDays(7),
            Path = "/"
        });

        return new()
        {
            Message = "Inloggningen lyckades."
        };
    }
}