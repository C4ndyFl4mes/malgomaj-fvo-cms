using AngleSharp.Text;
using Microsoft.AspNetCore.Components;
using Server.API.Exceptions;
using Server.API.Routes.Internal.Page;
using Server.API.Routes.Internal.Page.List;
using Server.UI.Layout;
using Server.UI.States;

namespace Server.UI.Pages.Content.PagesPage;

public class PagesBase : ComponentBase, IDisposable
{
    [Inject] protected NavigationState? NavigationState { get; set; }
    [Inject] protected PageListGetData? PageListGetData { get; set; }
    [Inject] protected ComponentExceptionHandler? ComponentExceptionHandler { get; set; }

    protected Guid Id { get; set; } = Guid.NewGuid();
    protected string Href { get; set; } = string.Empty; // Href for navigation to the editor, can be set by Id or other logic.
    protected List<PageItem> DraftPages { get; set; } = [];
    protected List<PageItem> PublishedPages { get; set; } = [];
    protected Dictionary<string, string[]> Errors { get; set; } = new();

    private CancellationTokenSource? _cts = null;

    protected override async Task OnInitializedAsync()
    {
        if (NavigationState is null) return;

        NavigationState.SetBreadcrumbs([
            new BreadcrumbModel
            {
                Title = "Panel",
                Href = "/admin"
            },
            new BreadcrumbModel
            {
                Title = "Innehåll"
            },
            new BreadcrumbModel
            {
                Title = "Sidor",
                Href = "/admin/content/pages"
            }
        ]);

        Href = $"/admin/content/pages/edit/{Id}";

        if (PageListGetData is not null)
            await LoadPages();
    }

    // Loads in pages (draft and published).
    private async Task LoadPages()
    {
        if (PageListGetData is null || ComponentExceptionHandler is null) return;

        CancellationTokenSource nextCts = new();
        CancellationTokenSource? previousCts = Interlocked.Exchange(ref _cts, nextCts);
        previousCts?.Cancel();
        previousCts?.Dispose();

        var response = await ComponentExceptionHandler.RunAsync(async () => await PageListGetData.GetListAsync(nextCts.Token));
        if (response.IsCanceled) return;
        if (response.IsSuccess && response.Value is not null)
        {
            foreach (PageItem page in response.Value.PageItems)
            {
                if (page.IsPublished)
                {
                    PublishedPages.Add(page);
                }
                else
                {
                    DraftPages.Add(page);
                }
            }

            PublishedPages = PublishedPages.OrderByDescending(p => p.PublishedAt).ToList();
            DraftPages = DraftPages.OrderByDescending(p => p.SavedAt).ToList();
        }
        else if (response.Error is not null)
        {
            Errors["loadPages"] = [$"Ett fel inträffade vid inläsning av sidor: {response.Error.Message}"];
        }
    }

    // Creates an excerpt for title.
    protected string TitleExcerpt(string title)
    {
        string[] words = title.SplitSpaces();
        string excerpt = "";

        foreach (string word in words)
        {
            if ($"{excerpt} {word}".Count() <= 17)
            {
                excerpt = $"{excerpt} {word}";
            }
            else
            {
                excerpt = $"{excerpt}...";
                break;
            }
        }

        return excerpt;
    }

    public void Dispose()
    {
        CancellationTokenSource? cts = Interlocked.Exchange(ref _cts, null);
        cts?.Cancel();
        cts?.Dispose();
        Errors.Clear();
    }
}
