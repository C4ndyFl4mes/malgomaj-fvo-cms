using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Server.API.Routes.Internal.Authentication;
using Server.Enums;
using Server.UI.Components.QuickOverlay;
using Server.UI.States;

namespace Server.UI.Pages.AuthPage;

public class AuthPageBase : ComponentBase, IAsyncDisposable
{
    [Inject] protected IJSRuntime JS { get; set; } = default!;
    [Inject] protected IValidator<UserModel> AuthValidator { get; set; } = default!;
    [Inject] protected NavigationManager NavigationManager { get; set; } = default!;
    [Inject] protected OverlayHostState OverlayHostState { get; set; } = default!;

    protected UserModel AuthFields { get; set; } = new()
    {
        Email = string.Empty,
        Password = string.Empty,
        RememberMe = false
    };

    protected bool IsSubmittingSignInRequest { get; set; } = false;
    protected Dictionary<string, string[]> ValidationErrors { get; set; } = [];

    private IJSObjectReference? _authModule;

    protected async Task AuthenticateAsync()
    {
        if (IsSubmittingSignInRequest)
            return;

        ValidationErrors.Clear();
        ValidationResult validation = AuthValidator.Validate(AuthFields);

        IsSubmittingSignInRequest = true;
        try
        {
            if (!validation.IsValid)
            {
                ValidationErrors = validation.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray()
                    );
            }
            else
            {
                ClientCallResponse<AuthenticationResponse> response = await AuthenticateFromBrowserAsync(AuthFields);
                if (!response.IsSuccess || response.Data is null)
                {
                    OverlayHostState.Append(new()
                    {
                        Id = Guid.NewGuid(),
                        ComponentType = typeof(QuickOverlay),
                        Parameters = new Dictionary<string, object>
                        {
                            ["Message"] = response.Error?.Detail ?? "Ett oväntat fel inträffade.",
                            ["Selection"] = QuickOverlaySelection.Error
                        }
                    });
                }
                else
                {
                    NavigationManager.NavigateTo("/admin/panel");
                    OverlayHostState.Append(new()
                    {
                        Id = Guid.NewGuid(),
                        ComponentType = typeof(QuickOverlay),
                        Parameters = new Dictionary<string, object>
                        {
                            ["Message"] = response.Data.Message,
                            ["Selection"] = QuickOverlaySelection.Success
                        }
                    });
                }
            }
        }
        finally
        {
            IsSubmittingSignInRequest = false;
        }
    }

    private async Task<ClientCallResponse<AuthenticationResponse>> AuthenticateFromBrowserAsync(UserModel model)
    {
        _authModule ??= await JS.InvokeAsync<IJSObjectReference>("import", "/otherJS/auth.js");
        return await _authModule.InvokeAsync<ClientCallResponse<AuthenticationResponse>>("signin", model);
    }

    public async ValueTask DisposeAsync()
    {
        if (_authModule is not null)
        {
            try
            {
                GC.SuppressFinalize(this);
                await _authModule.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
                // Ignored.
            }
        }
    }
}