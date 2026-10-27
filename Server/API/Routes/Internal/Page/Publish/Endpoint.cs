using FastEndpoints;
using Server.Models;

namespace Server.API.Routes.Internal.Page.Publish;

public class PublishPageEndpoint(PagePublishData data) : Endpoint<PageEditorModel, PublishPageResponse>
{
    public override void Configure()
    {
        Post("/api/pages/publish");
        Policies("InternalUsers");
    }

    public override async Task<PublishPageResponse> ExecuteAsync(PageEditorModel model, CancellationToken ct)
    {
        return new PublishPageResponse
        {
            PublishedAt = await data.PublishAsync(HttpContext.User, model, ct)
        };
    }
}
