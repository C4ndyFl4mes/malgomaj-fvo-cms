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
    [Inject] protected ComponentExceptionHandler? ComponentExceptionHandler { get; set; }

    [Parameter] public Guid PageId { get; set; }

    protected PageEditorModel? PageEditorModel { get; set; }

    protected Dictionary<string, string[]> Errors { get; set; } = [];
    // protected GetImagesResponse? ResponseCache { get; set; } = null; // Cache för att undvika onödiga API-anrop.
    protected bool IsPublishing { get; set; } = false;
    protected bool IsUnpublishing { get; set; } = false;
    protected bool IsSaving { get; set; } = false;
    protected PageMeta? PageMetaRef { get; set; }
    protected DateTime? LocalSavedAt { get; set; } = null;

    // Debounce- and version management for optimizing the saving of the page when multiple changes occur rapidly.
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

    // Initializing the editor throught fetching the page's data from the API:
    protected async Task InitializePageEditorAsync()
    {
        if (EditorGetData is null || ComponentExceptionHandler is null) return;

        CancellationTokenSource nextCts = new();
        CancellationTokenSource? previousCts = Interlocked.Exchange(ref _cts, nextCts);
        previousCts?.Cancel();
        previousCts?.Dispose();

        var response = await ComponentExceptionHandler.RunAsync(async () => await EditorGetData.GetAsync(PageId, nextCts.Token));
        if (response.IsCanceled) return;
        if (response.IsSuccess && response.Value is not null)
        {
            PageEditorModel = response.Value;
        }
        else if (response.Error is not null)
        {
            Errors["init"] = [$"Det gick inte att ladda sidan: {response.Error.Message}"];
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

    // When meta data or content changes, the saving will be queued so that only the latest change will be sent to the API.
    // The timing is two seconds.
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

    // When the debounce time has passed without any new changes, it will save the latest version of the page.
    private async Task DebounceThenSaveAsync(CancellationToken ct)
    {
        if (ComponentExceptionHandler is null) return;

        var response = await ComponentExceptionHandler.RunAsync(async () =>
        {
            await Task.Delay(SaveDebounceDelay, ct);
            await SaveLatestAsync(ct);
            return true;
        });

        if (response.IsCanceled) return;
    }

    // Saves the latest version of the page.
    protected async Task SaveLatestAsync(CancellationToken ct)
    {
        if (PageEditorModel is null || PageSaveData is null || ComponentExceptionHandler is null || Disposed) return;

        var response = await ComponentExceptionHandler.RunAsync(
            async () =>
            {
                await SaveLock.WaitAsync(ct);

                int targetVersion = Volatile.Read(ref ChangeVersion);
                if (targetVersion <= Volatile.Read(ref SavedVersion)) return true;

                SavePageResponse saveResponse = await PageSaveData.SaveAsync(PageEditorModel, ct);

                PageEditorModel.Meta.SavedAt = saveResponse.SavedAt;
                PageEditorModel.Meta.PublishedAt = saveResponse.PublishedAt;

                Volatile.Write(ref SavedVersion, targetVersion);
                Errors.Remove("save");

                return true;
            },
            async () =>
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
        );

        if (response.IsCanceled) return;
        if (!response.IsSuccess && response.Error is not null)
        {
            Errors["save"] = [$"Det gick inte att spara sidan: {response.Error.Message}"];
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
        Errors.Clear();
    }
}
