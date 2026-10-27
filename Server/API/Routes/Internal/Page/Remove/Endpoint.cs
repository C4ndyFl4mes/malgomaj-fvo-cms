using FastEndpoints;
using Server.API.Exceptions;

namespace Server.API.Routes.Internal.Page.Remove;

public class PageRemoveEndpoint(PageRemoveData data) : EndpointWithoutRequest<RemovePageResponse>
{
    public override void Configure()
    {
        Delete("/api/pages/{pageId}");
        Policies("InternalUsers");
    }

    public override async Task<RemovePageResponse> ExecuteAsync(CancellationToken ct)
    {
        string pageId = Route<string>("pageId", isRequired: true) ??
            throw new BadRequestException("PageId must be specified.");

        if(!Guid.TryParse(pageId, out Guid id))
            throw new BadRequestException("Unable to parse pageId as Guid.");
        
        await data.RemoveAsync(HttpContext.User, id, ct);

        return new()
        {
            Message = $"Raderingen av sidan med ID:[{id}] lyckades."
        };
    }
}