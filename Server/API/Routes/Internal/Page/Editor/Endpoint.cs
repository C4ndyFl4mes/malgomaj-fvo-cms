using FastEndpoints;
using Server.API.Exceptions;
using Server.Models;

namespace Server.API.Routes.Internal.Page.Editor;

public class EditorGetEndpoint(EditorGetData data) : EndpointWithoutRequest<PageEditorModel>
{
    public override void Configure()
    {
        Get("/api/pages/{pageId}");
        Policies("InternalUsers");
    }

    public override async Task<PageEditorModel> ExecuteAsync(CancellationToken ct)
    {
        string pageId = Route<string>("pageId", isRequired: true) ??
            throw new BadRequestException("PageId must be specified.");

        if(!Guid.TryParse(pageId, out Guid id))
            throw new BadRequestException("Unable to parse pageId as Guid.");

        return await data.GetAsync(HttpContext.User, id, ct);
    }
}