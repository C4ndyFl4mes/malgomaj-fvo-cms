using FastEndpoints;
using Server.API.Data;
using Server.Models;

namespace Server.API.Routes.Internal.Page.Publish;

public class PublishPageEndpoint(AppDbContext ctx) : Endpoint<PageEditorModel, PublishPageResponse>
{
    public override void Configure()
    {
        Post("/api/pages/publish");
        AllowAnonymous();
    }

    public override async Task<PublishPageResponse> ExecuteAsync(PageEditorModel model, CancellationToken ct)
    {
        PagePublishData data = new(ctx);

        return new PublishPageResponse
        {
            PublishedAt = await data.PublishAsync(model, ct)
        };
    }
}
