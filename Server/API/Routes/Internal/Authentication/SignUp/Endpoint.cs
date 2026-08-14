using FastEndpoints;
using Server.API.Data;

namespace Server.API.Routes.Internal.Authentication.SignUp;

public class SignUpEndpoint(AppDbContext ctx) : Endpoint<UserModel, AuthenticationResponse>
{
    public override void Configure()
    {
        Post("/api/auth/signup");
        Roles("Administrator");
    }

    public override async Task<AuthenticationResponse> ExecuteAsync(UserModel model, CancellationToken ct)
    {
        SignUpData data = new(ctx);

        return await data.SignUp(model, ct);
    }
}