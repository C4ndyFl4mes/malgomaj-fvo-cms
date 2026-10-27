using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Server.API.Routes.Internal.Page;
using Server.API.Routes.Internal.Page.List;
using Server.Models;
using Server.API.Exceptions;
using Microsoft.AspNetCore.Components.Authorization;
// using Server.API.Routes.Page.GET.List;

namespace Server.UI.Components.PageMeta;

public class PageMetaBase : ComponentBase, IDisposable
{
    [Inject] protected PageListGetData PageListGetData { get; set; } = default!;
    [Inject] protected ComponentExceptionHandler ComponentExceptionHandler { get; set; } = default!;
    [Inject] protected AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

    [Parameter] public required PageMetaModel Meta { get; set; }

    // Events
    [Parameter] public EventCallback<PageMetaModel> MetaChanged { get; set; } // Invokes whenever a field value is changed.
    [Parameter] public EventCallback OnPublishing { get; set; } // Invokes by the publish button.
    [Parameter] public EventCallback OnRedacting { get; set; } // Invokes by the redact button.
    [Parameter] public EventCallback OnRemoving { get; set; } // Invokes by the delete button.

    public bool IsPublishing { get; set; } = false;
    public bool IsRedacting { get; set; } = false;
    protected Dictionary<string, string[]> ValidationErrors { get; set; } = [];
    protected bool HasUnPublishedChanges { get; set; } = false;

    private CancellationTokenSource? _cts = null;

    public void DetermineUnPublishedChanges(DateTime savedAt)
    {
        // Console.WriteLine($"SavedAt: {savedAt} and PublishedAt: {Meta.PublishedAt}");
        HasUnPublishedChanges = Meta.PublishedAt is not null && savedAt > Meta.PublishedAt;
    }

    // Used by the Publish-method in EditorPageBase.
    public void ForceHasUnPublishedChangesToFalse()
    {
        HasUnPublishedChanges = false;
    }

    protected string ApplyDisabledStyling(bool doIt)
    {
        return doIt ? "opacity-50 cursor-not-allowed" : "cursor-pointer";
    }

    // Invokes the event that will cause a queued saved.
    protected async Task HandleMetaChange()
    {
        Meta.Slug = Slugify(Meta.Title);
        await MetaChanged.InvokeAsync();
    }

    // To easily set the values from the fields.
    protected async Task HandleMetaFieldChanges(string field, string value)
    {
        switch (field)
        {
            case "title":
                Meta.Title = value;
                break;
            case "keywords":
                Meta.Keywords = value;
                break;
            case "description":
                Meta.Description = value;
                break;
            default:
                // Will never happen beacause they're all specified.
                break;
        }
        await HandleMetaChange();
    }

    // Invokes the event that will try to publish the page.
    protected async Task OnPublish()
    {
        IsRedacting = false;
        IsPublishing = true;

        if (!await CheckSlugConflictAsync())
            await OnPublishing.InvokeAsync();
    }

    // Invokes the event that will try to redact the page.
    protected async Task OnRedact()
    {
        IsPublishing = false;
        IsRedacting = true;
        await OnRedacting.InvokeAsync();
    }

    // Invokes the event that will try to remove the page and redirect the user.
    protected async Task OnRemove()
    {
        await OnRemoving.InvokeAsync();
    }

    protected async Task<bool> CheckSlugConflictAsync()
    {
        CancellationTokenSource nextCts = new();
        CancellationTokenSource? previousCts = Interlocked.Exchange(ref _cts, nextCts);
        previousCts?.Cancel();
        previousCts?.Dispose();

        bool slugConflict = false;

        var exception = await ComponentExceptionHandler.RunAsync(
            async () =>
            {

                AuthenticationState state = await AuthenticationStateProvider.GetAuthenticationStateAsync();
                PageListGetResponse response = await PageListGetData.GetListAsync(state.User, nextCts.Token);

                slugConflict = response.PageItems.Any(p => p.PageId != Meta.Id && p.IsPublished && !string.IsNullOrWhiteSpace(Meta.Slug) && p.Slug == Meta.Slug);
                if (slugConflict)
                {
                    IsPublishing = false;
                    ValidationErrors["slugConflict"] = ["En annan publicerad sida har samma titel. Vänligen ändra titeln innan du publicerar."];
                }

                return true;
            }
         );

        if (exception.IsCanceled) return false;
        if (!exception.IsSuccess && exception.Error is not null)
        {
            ValidationErrors["exception"] = [exception.Error.Message];
        }

        return slugConflict;
    }

    private static string Slugify(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        input = input.ToLowerInvariant();

        input = RemoveDiacritics(input);

        input = Regex.Replace(input, @"[^a-z0-9\s-]", "");
        input = Regex.Replace(input, @"\s+", "-").Trim();
        input = Regex.Replace(input, @"-+", "-");

        return input;
    }

    private static string RemoveDiacritics(string text)
    {
        string normalized = text.Normalize(NormalizationForm.FormD);
        StringBuilder sb = new StringBuilder();

        foreach (char c in normalized)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    public void Dispose()
    {
        CancellationTokenSource? cts = Interlocked.Exchange(ref _cts, null);
        cts?.Cancel();
        cts?.Dispose();
    }
}
