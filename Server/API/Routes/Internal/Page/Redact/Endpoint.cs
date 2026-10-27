using FastEndpoints;
using Server.API.Exceptions;

namespace Server.API.Routes.Internal.Page.Redact;

public class PageRedactEndpoint(PageRedactData data) : EndpointWithoutRequest<RedactPageResponse>
{
    public override void Configure()
    {
        Post("/api/pages/redact/{pageId}");
        Policies("InternalUsers");
    }

    public override async Task<RedactPageResponse> ExecuteAsync(CancellationToken ct)
    {
        string pageId = Route<string>("pageId", isRequired: true) ??
            throw new BadRequestException("PageId must be specified.");

        if(!Guid.TryParse(pageId, out Guid id))
            throw new BadRequestException("Unable to parse pageId as Guid.");

        await data.RedactAsync(HttpContext.User, id, ct);

        return new()
        {
            Message = $"Avpubliceringen av sidan med ID:[{id}] lyckades."
        };
    }
}