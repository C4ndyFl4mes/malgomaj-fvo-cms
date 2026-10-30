using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Server.API.Routes.Internal.Authentication;

namespace Server.UI.Pages.AuthPage;

public class AuthPageBase : ComponentBase, IAsyncDisposable
{
    [Inject] protected IJSRuntime JS { get; set; } = default!;
    [Inject] protected IValidator<UserModel> AuthValidator { get; set; } = default!;
    [Inject] protected NavigationManager NavigationManager { get; set; } = default!;

    protected UserModel AuthFields { get; set; } = new()
    {
        Email = string.Empty,
        Password = string.Empty
    };

    protected Dictionary<string, string[]> ValidationErrors { get; set; } = [];
    protected (bool IsSuccess, string Message) ResultMessage { get; set; } = (false, string.Empty);

    private IJSObjectReference? _authModule;

    protected async Task AuthenticateAsync()
    {
        ValidationErrors.Clear();
        ValidationResult validation = AuthValidator.Validate(AuthFields);

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
                ResultMessage = (false, response.Error?.Detail ?? "Ett oväntat fel inträffade.");
            }
            else
            {
                ResultMessage = (true, response.Data.Message);

                await Task.Delay(2000);
                NavigationManager.NavigateTo("/admin/panel", forceLoad: true);
            }
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