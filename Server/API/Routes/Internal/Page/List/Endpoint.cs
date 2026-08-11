using FastEndpoints;
using Server.API.Data;

namespace Server.API.Routes.Internal.Page.List;

public class PageListGetEndpoint(AppDbContext ctx) : EndpointWithoutRequest<PageListGetResponse>
{
    public override void Configure()
    {
        Get("/api/pages");
        AllowAnonymous();
    }

    public override async Task<PageListGetResponse> ExecuteAsync(CancellationToken ct)
    {
        PageListGetData data = new(ctx);

        return await data.GetListAsync(ct);
    }
}