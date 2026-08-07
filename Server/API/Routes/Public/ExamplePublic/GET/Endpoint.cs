using FastEndpoints;

namespace Server.API.Routes.Public.ExamplePublic.GET;

public class ExamplePublicEndpoint(): EndpointWithoutRequest<string>
{
    public override void Configure()
    {
        Get("/api/examplepublic");
        AllowAnonymous();
    }

    public override async Task<string> ExecuteAsync(CancellationToken ct)
    {
        return "Hello, World! This is public speaking.";
    }
}