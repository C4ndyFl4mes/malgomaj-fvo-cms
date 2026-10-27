using FastEndpoints;

namespace Server.API.Routes.Internal.Authentication.SignUp;

public class SignUpEndpoint(SignUpData data) : Endpoint<UserModel, AuthenticationResponse>
{
    public override void Configure()
    {
        Post("/api/auth/signup");
        Policies("AdministratorsOnly");
    }

    public override async Task<AuthenticationResponse> ExecuteAsync(UserModel model, CancellationToken ct)
    {
        return await data.SignUp(HttpContext.User, model, ct);
    }
}