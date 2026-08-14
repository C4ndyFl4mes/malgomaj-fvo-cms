using System.Security.Claims;
using Server.API.Exceptions;

namespace Server.Utils;

public static class UserUtils
{
    private static HttpContext? CurrentHttpContext => new HttpContextAccessor().HttpContext;

    public static Guid GetAuthenticatedUserID()
    {
        Guid currentUserId = Guid.Empty;
        if (CurrentHttpContext?.User.Identity?.IsAuthenticated == true)
        {
            string? userIdRaw =
                CurrentHttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                CurrentHttpContext?.User.FindFirst("nameid")?.Value;
            
            if (userIdRaw is not null)
            {
                if (!Guid.TryParse(userIdRaw, out currentUserId))
                    throw new BadRequestException("Unable to parse Guid for the current user.");
            }
        }

        return currentUserId;
    }
}