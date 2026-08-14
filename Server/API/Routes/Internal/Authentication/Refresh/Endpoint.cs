using FastEndpoints;
using Server.API.Data;
using Server.API.Exceptions;
using Server.Services;
using Server.Utils;

namespace Server.API.Routes.Internal.Authentication.Refresh;

public class RefreshEndpoint(IConfiguration config, AppDbContext ctx) : EndpointWithoutRequest<AuthenticationResponse>
{
    public override void Configure()
    {
        Post("/api/auth/refresh");
        AllowAnonymous();
    }

    public override async Task<AuthenticationResponse> ExecuteAsync(CancellationToken ct)
    {
        Token reqToken = new()
        {
            AccessToken = HttpContext.Request.Cookies["accessToken"] ?? string.Empty,
            RefreshToken = HttpContext.Request.Cookies["refreshToken"] ?? string.Empty
        };

        Token? resToken = null;

        if (!string.IsNullOrEmpty(reqToken.RefreshToken))
        {
            TokenService tokenService = new(config, ctx);
            resToken = await tokenService.RenewTokens(UserUtils.GetAuthenticatedUserID(), reqToken.RefreshToken, ct);
        }

        if (resToken is null || string.IsNullOrEmpty(resToken.AccessToken) || string.IsNullOrEmpty(resToken.RefreshToken))
        {
            HttpContext.Response.Cookies.Delete("accessToken");
            HttpContext.Response.Cookies.Delete("refreshToken");
            throw new UnauthorizedException("Token kunde inte uppdateras.");
        }

        HttpContext.Response.Cookies.Append("accessToken", resToken.AccessToken,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Path = "/",
                Expires = DateTimeOffset.UtcNow.AddMinutes(12)
            }
        );

        HttpContext.Response.Cookies.Append("refreshToken", resToken.RefreshToken,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Path = "/",
                Expires = DateTimeOffset.UtcNow.AddDays(7)
            }
        );

        return new()
        {
            Message = "Token uppdaterad."
        };
    }
}