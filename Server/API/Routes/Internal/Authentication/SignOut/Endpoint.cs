using FastEndpoints;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Server.API.Routes.Internal.Authentication.SignOut;

public class SignOutEndpoint : EndpointWithoutRequest<AuthenticationResponse>
{
    public override void Configure()
    {
        Post("/api/auth/signout");
        AllowAnonymous();
    }

    public override async Task<AuthenticationResponse> ExecuteAsync(CancellationToken ct)
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        return new()
        {
            Message = "Utloggningen lyckades"
        };
    }
}