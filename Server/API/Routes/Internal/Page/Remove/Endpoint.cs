using FastEndpoints;
using Server.API.Data;
using Server.API.Exceptions;

namespace Server.API.Routes.Internal.Page.Remove;

public class PageRemoveEndpoint(AppDbContext ctx) : EndpointWithoutRequest<RemovePageResponse>
{
    public override void Configure()
    {
        Delete("/api/pages/{pageId}");
        AllowAnonymous();
    }

    public override async Task<RemovePageResponse> ExecuteAsync(CancellationToken ct)
    {
        string pageId = Route<string>("pageId", isRequired: true) ??
            throw new BadRequestException("PageId must be specified.");

        if(!Guid.TryParse(pageId, out Guid id))
            throw new BadRequestException("Unable to parse pageId as Guid.");
        
        PageRemoveData data = new(ctx);
        await data.RemoveAsync(id, ct);

        return new()
        {
            Message = $"Raderingen av sidan med ID:[{id}] lyckades."
        };
    }
}