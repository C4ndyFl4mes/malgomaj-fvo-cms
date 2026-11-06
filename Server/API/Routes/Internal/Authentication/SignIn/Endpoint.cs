using System.Security.Claims;
using FastEndpoints;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Server.API.Entities;
using Server.API.Exceptions;

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
        UserEntity user = await data.VerifyCredentialsAsync(model, ct);

        List<Claim> claims =
        [
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.Name)
        ];

        ClaimsIdentity identity = new(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme
        );

        ClaimsPrincipal principal = new(identity);

        AuthenticationProperties properties = new()
        {
            IsPersistent = false,
            AllowRefresh = true  
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            properties
        );

        return new()
        {
            Message = "Inloggningen lyckades."
        };
    }
}