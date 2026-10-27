using FastEndpoints;
using Server.Models;

namespace Server.API.Routes.Internal.Page.Save;

public class SavePageEndpoint(PageSaveData data) : Endpoint<PageEditorModel, SavePageResponse>
{
    public override void Configure()
    {
        Post("/api/pages/save");
        Policies("InternalUsers");
    }

    public override async Task<SavePageResponse> ExecuteAsync(PageEditorModel model, CancellationToken ct)
    {
        return await data.SaveAsync(HttpContext.User, model, ct);
    }
}
