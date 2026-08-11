using Microsoft.AspNetCore.Components;
using Server.API.Exceptions;
using Server.API.Routes.Internal.Page;
using Server.API.Routes.Internal.Page.Editor;
using Server.API.Routes.Internal.Page.Save;
using Server.Models;
using Server.UI.Components.PageMeta;
using Server.UI.Layout;
using Server.UI.States;

namespace Server.UI.Pages.Content.PagesPage.EditorPage;

public class EditorPageBase : ComponentBase, IDisposable
{
    [Inject] protected NavigationState? NavigationState { get; set; }
    [Inject] protected EditorGetData? EditorGetData { get; set; }
    [Inject] protected PageSaveData? PageSaveData { get; set; }

    [Parameter] public Guid PageId { get; set; }

    protected PageEditorModel? PageEditorModel { get; set; }

    protected Dictionary<string, string[]> ValidationErrors { get; set; } = [];
    // protected GetImagesResponse? ResponseCache { get; set; } = null; // Cache för att undvika onödiga API-anrop.
    protected bool IsPublishing { get; set; } = false;
    protected bool IsUnpublishing { get; set; } = false;
    protected bool IsSaving { get; set; } = false;
    protected PageMeta? PageMetaRef { get; set; }
    protected DateTime? LocalSavedAt { get; set; } = null;

    // Debounce- och versionshantering för att optimera sparandet av sidan när flera förändringar sker i snabb följd.
    private static readonly TimeSpan SaveDebounceDelay = TimeSpan.FromSeconds(2);
    private CancellationTokenSource? DebounceCts = null;
    private readonly SemaphoreSlim SaveLock = new(1, 1); // Lås för att säkerställa att endast en save-operation sker åt gången.
    private int ChangeVersion = 0;
    private int SavedVersion = 0;
    private bool Disposed = false;

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
            },
            new BreadcrumbModel
            {
                Title = "Redigera",
                Href = $"/admin/content/pages/edit/{PageId}"
            }
        ]);

        if (PageEditorModel is null)
        {
            await InitializePageEditorAsync();
        }
    }

    // Initialiserar editorn genom att hämta sidans data från API:t. Om sidan inte finns (KeyNotFoundException) skapas en ny PageEditorModel med standardvärden.
    protected async Task InitializePageEditorAsync()
    {
        if (EditorGetData is null)
        {
            ValidationErrors["init"] = ["EditorGetData är inte tillgänglig."];
            return;
        }
        try
        {
            CancellationTokenSource nextCts = new();
            CancellationTokenSource? previousCts = Interlocked.Exchange(ref _cts, nextCts);
            previousCts?.Cancel();
            previousCts?.Dispose();


            PageEditorModel = await EditorGetData.GetAsync(PageId, nextCts.Token);
        }
        catch (NotFoundException)
        {
            PageEditorModel = new()
            {
                Id = PageId,
                Meta = new PageMetaModel()
                {
                    Id = PageId,
                    Title = "Namnlös sida",
                    Slug = "namnlos-sida",
                    Description = string.Empty,
                    Keywords = string.Empty,
                    IsPublished = false,
                    PublishedAt = null,
                    SavedAt = DateTime.UtcNow
                },
                ContentDeltaJSON = string.Empty
            };
        }
        catch (Exception ex)
        {
            ValidationErrors = new Dictionary<string, string[]>
            {
                ["init"] = [$"Det gick inte att ladda sidan: {ex.Message}"]
            };
        }
    }

    protected async Task OnMetaChangedAsync()
    {
        QueueSave();
        return;
    }

    protected async Task OnContentChangedAsync(string contentDeltaJSON)
    {
        if (PageEditorModel is null) return;

        PageEditorModel.ContentDeltaJSON = contentDeltaJSON;
        QueueSave();
        return;
    }

    // När metadata eller innehåll förändras, antingen genom PageMeta-komponenten eller TranslationTabs-komponenten,
    // så köas en sparning av sidan. Om flera förändringar sker inom en kort tidsperiod (2 sekunder)
    // så kommer endast den senaste att sparas, vilket minskar onödiga API-anrop och förbättrar prestandan.
    private void QueueSave()
    {
        Interlocked.Increment(ref ChangeVersion);

        CancellationTokenSource next = new();
        CancellationTokenSource? previous = Interlocked.Exchange(ref DebounceCts, next);
        previous?.Cancel();
        previous?.Dispose();

        IsSaving = true;

        _ = DebounceThenSaveAsync(next.Token);
    }

    // När debounce-tiden har gått utan att nya förändringar sker, så sparas den senaste versionen av sidan.
    private async Task DebounceThenSaveAsync(CancellationToken token)
    {
        try
        {
            await Task.Delay(SaveDebounceDelay, token);
            await SaveLatestAsync(token);
        }
        catch (OperationCanceledException)
        {
            // Förväntat beteende, gör inget.
        }
    }

    // Sparar den senaste versionen av sidan.
    protected async Task SaveLatestAsync(CancellationToken ct)
    {
        if (PageEditorModel is null || PageSaveData is null || Disposed) return;

        await SaveLock.WaitAsync(ct);

        try
        {
            int targetVersion = Volatile.Read(ref ChangeVersion);
            if (targetVersion <= Volatile.Read(ref SavedVersion)) return;

            
            SavePageResponse response = await PageSaveData.SaveAsync(PageEditorModel, ct);

            PageEditorModel.Meta.SavedAt = response.SavedAt;
            PageEditorModel.Meta.PublishedAt = response.PublishedAt;

            Volatile.Write(ref SavedVersion, targetVersion);
            ValidationErrors.Remove("save");
        }
        catch (Exception ex)
        {
            ValidationErrors["save"] = [$"Det gick inte att spara sidan: {ex.Message}"];
        }
        finally
        {
            if (PageMetaRef is not null)
            {
                if (IsPublishing)
                    await PageMetaRef.StopPublishing();

                if (IsUnpublishing)
                    await PageMetaRef.StopRedacting();
            }
            
            SaveLock.Release();

            IsSaving = false;
            LocalSavedAt = DateTime.UtcNow;
            await InvokeAsync(StateHasChanged);
        }

        if (Volatile.Read(ref ChangeVersion) > Volatile.Read(ref SavedVersion))
        {
            await SaveLatestAsync(ct);
        }
    }

    // In the following two methods, the publishing and redaction services will be used.
    // When publishing, it will be optimized for the frontend.
    protected async Task Publish(bool v)
    {
        IsPublishing = v;
    }

    protected async Task Redact(bool v)
    {
        IsUnpublishing = v;
    }

    public void Dispose()
    {
        Disposed = true;
        DebounceCts?.Cancel();
        DebounceCts?.Dispose();
        SaveLock.Dispose();

        PageEditorModel = null;
        // ResponseCache = null;
        ValidationErrors.Clear();
    }
}
