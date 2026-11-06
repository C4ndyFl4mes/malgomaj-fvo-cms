using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using Server.Services;
namespace Server.UI.States;

public sealed class CmsAuthenticationStateProvider(
    ILoggerFactory loggerFactory,
    IServiceScopeFactory scopeFactory
) : RevalidatingServerAuthenticationStateProvider(loggerFactory)
{
    protected override TimeSpan RevalidationInterval => TimeSpan.FromMinutes(5);

    protected override async Task<bool> ValidateAuthenticationStateAsync(AuthenticationState authenticationState, CancellationToken ct)
    {
        string? userId = authenticationState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(userId))
            return false;

        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();

        UserSessionValidator sessionValidator = scope.ServiceProvider.GetRequiredService<UserSessionValidator>();

        return await sessionValidator.IsSessionValidAsync(userId, ct);
    }
}