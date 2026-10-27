using FastEndpoints;

namespace Server.API.Routes.Internal.Page.List;

public class PageListGetEndpoint(PageListGetData data) : EndpointWithoutRequest<PageListGetResponse>
{
    public override void Configure()
    {
        Get("/api/pages");
        Policies("InternalUsers");
    }

    public override async Task<PageListGetResponse> ExecuteAsync(CancellationToken ct)
    {
        return await data.GetListAsync(HttpContext.User, ct);
    }
}