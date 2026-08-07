using FastEndpoints;

namespace Server.API.Routes.Internal.ExampleInternal.GET;

public class ExampleInternalEndpoint(): EndpointWithoutRequest<string>
{
    public override void Configure()
    {
        Get("/api/exampleinternal");
        AllowAnonymous(); // Since it is an internal, it actually shouldn't be public.
    }

    public override async Task<string> ExecuteAsync(CancellationToken ct)
    {
        return "Hello, World! This is internal speaking.";
    }
}