using FastEndpoints;
using Server.API.Data;
using Server.Models;

namespace Server.API.Routes.Internal.Page.Save;

public class SavePageEndpoint(AppDbContext ctx) : Endpoint<PageEditorModel, SavePageResponse>
{
    public override void Configure()
    {
        Post("/api/pages/save");
        AllowAnonymous();
    }

    public override async Task<SavePageResponse> ExecuteAsync(PageEditorModel model, CancellationToken ct)
    {
        PageSaveData data = new(ctx);
        
        return await data.SaveAsync(model, ct);
    }
}