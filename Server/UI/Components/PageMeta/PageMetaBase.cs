using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Server.API.Routes.Internal.Page;
using Server.API.Routes.Internal.Page.List;
using Server.Models;
// using Server.API.Routes.Page.GET.List;

namespace Server.UI.Components.PageMeta;

public class PageMetaBase : ComponentBase, IDisposable
{
    [Inject] protected PageListGetData? PageListGetData { get; set; }

    [Parameter] public required PageMetaModel Meta { get; set; } = default!;

    // Events
    [Parameter] public EventCallback<PageMetaModel> MetaChanged { get; set; }
    [Parameter] public EventCallback<bool> OnPublish { get; set; }
    [Parameter] public EventCallback<bool> OnUnpublish { get; set; }

    protected bool IsUnpublishing { get; set; } = false;
    protected bool IsPublishing { get; set; } = false;
    protected Dictionary<string, string[]> ValidationErrors { get; set; } = [];


    private CancellationTokenSource? _cts = null;

    public async Task StopPublishing()
    {
        await HandleOnPublish();
    }

    public async Task StopRedacting()
    {
        await HandleOnUnpublish();
    }

    protected async Task HandleMetaChange()
    {
        Meta.Slug = Slugify(Meta.Title);
        await MetaChanged.InvokeAsync(Meta);
    }

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

    protected async Task HandleOnPublish()
    {
        IsPublishing = !IsPublishing;
        await OnPublish.InvokeAsync(IsPublishing);
    }

    protected async Task HandleOnUnpublish()
    {
        IsUnpublishing = !IsUnpublishing;
        await OnUnpublish.InvokeAsync(IsUnpublishing);
    }

    protected async Task HandlePublishToggleAsync()
    {
        if (!Meta.IsPublished)
            await HandleOnPublish();
        if (Meta.IsPublished)
            await HandleOnUnpublish();

        if (PageListGetData is null) return;

        CancellationTokenSource nextCts = new();
        CancellationTokenSource? previousCts = Interlocked.Exchange(ref _cts, nextCts);
        previousCts?.Cancel();
        previousCts?.Dispose();

        try
        {
            PageListGetResponse response = await PageListGetData.GetListAsync(nextCts.Token);

            if (nextCts.IsCancellationRequested)
            {
                if (!Meta.IsPublished)
                    await HandleOnPublish();
                if (Meta.IsPublished)
                    await HandleOnUnpublish();

                return;
            }

            if (Meta.IsPublished)
            {
                Meta.IsPublished = false;
            }
            else
            {
                bool slugConflict = response.PageItems.Any(p => p.PageId != Meta.Id && p.IsPublished && !string.IsNullOrWhiteSpace(Meta.Slug) && p.Slug == Meta.Slug);
                if (slugConflict)
                {
                    ValidationErrors["slug"] = ["En annan publicerad sida har samma slug. Vänligen ändra slug innan du publicerar."];
                    return;
                }
                Meta.IsPublished = true;
                ValidationErrors.Remove("slug");
            }
            await HandleMetaChange();
        }
        catch (OperationCanceledException)
        {
            // Förväntat beteende, gör inget.
        }
        catch (Exception ex)
        {
            ValidationErrors["pageList"] = [$"Kunde inte läsa in alla sidor: {ex.Message}"];
        }
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
