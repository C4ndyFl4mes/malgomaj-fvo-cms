using FastEndpoints;
using Server.API.Data;
using Server.API.Exceptions;
using Server.Services;
using Server.Utils;

namespace Server.API.Routes.Internal.Authentication.Refresh;

// It automatically refreshes the authentication cookie because of the HTTP request.
public class RefreshEndpoint() : EndpointWithoutRequest<AuthenticationResponse>
{
    public override void Configure()
    {
        Post("/api/auth/refresh");
        Policies("InternalUsers");
    }

    public override async Task<AuthenticationResponse> ExecuteAsync(CancellationToken ct)
    {
        return new()
        {
            Message = "Autenthentication Cookie has been refreshed at "
        };
    }
}