using Microsoft.AspNetCore.Components;
using Server.API.Exceptions;
using Server.API.Routes.Internal.Page;
using Server.API.Routes.Internal.Page.Editor;
using Server.API.Routes.Internal.Page.Publish;
using Server.API.Routes.Internal.Page.Redact;
using Server.API.Routes.Internal.Page.Remove;
using Server.API.Routes.Internal.Page.Save;
using Server.Models;
using Server.Services;
using Server.UI.Components.PageMeta;
using Server.UI.Layout;
using Server.UI.States;

namespace Server.UI.Pages.Content.PagesPage.EditorPage;

public class EditorPageBase : ComponentBase, IDisposable
{
    [Inject] protected NavigationState? NavigationState { get; set; }
    [Inject] protected EditorGetData? EditorGetData { get; set; }
    [Inject] protected PageSaveData? PageSaveData { get; set; }
    [Inject] protected PagePublishData? PagePublishData { get; set; }
    [Inject] protected PageRedactData? PageRedactData { get; set; }
    [Inject] protected PageRemoveData? PageRemoveData { get; set; }
    [Inject] protected ComponentExceptionHandler? ComponentExceptionHandler { get; set; }
    [Inject] protected NavigationManager? NavigationManager { get; set; }

    [Parameter] public Guid PageId { get; set; }

    protected PageEditorModel? PageEditorModel { get; set; }

    protected Dictionary<string, string[]> Errors { get; set; } = [];
    // protected GetImagesResponse? ResponseCache { get; set; } = null; // Cache för att undvika onödiga API-anrop.
    protected bool IsSaving { get; set; } = false;
    protected PageMeta? PageMetaRef { get; set; }
    protected DateTime? LocalSavedAt { get; set; } = null;

    // Debounce- and version management for optimizing the saving of the page when multiple changes occur rapidly.
    private static readonly TimeSpan _saveDebounceDelay = TimeSpan.FromSeconds(2);
    private readonly SemaphoreSlim _saveLock = new(1, 1); // Lock to ensure that only one save-operation occurs at a time.
    private int _changeVersion = 0;
    private int _savedVersion = 0;
    private bool _dispose = false;
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

        var exception = await ComponentExceptionHandler.RunAsync(async () => await EditorGetData.GetAsync(PageId, nextCts.Token));
        if (exception.IsCanceled) return;
        if (exception.IsSuccess && exception.Value is not null)
        {
            PageEditorModel = exception.Value;
        }
        else if (exception.Error is not null)
        {
            Errors["init"] = [$"Det gick inte att ladda sidan: {exception.Error.Message}"];
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

        PageEditorModel.ContentJSON = contentDeltaJSON;
        QueueSave();
        return;
    }

    // When meta data or content changes, the saving will be queued so that only the latest change will be sent to the API.
    // The timing is two seconds.
    protected void QueueSave()
    {
        Interlocked.Increment(ref _changeVersion);

        CancellationTokenSource next = new();
        CancellationTokenSource? previous = Interlocked.Exchange(ref _cts, next);
        previous?.Cancel();
        previous?.Dispose();

        IsSaving = true;

        _ = DebounceThenSaveAsync(next.Token);
    }

    // When the debounce time has passed without any new changes, it will save the latest version of the page.
    private async Task DebounceThenSaveAsync(CancellationToken ct)
    {
        if (ComponentExceptionHandler is null) return;

        var exception = await ComponentExceptionHandler.RunAsync(
            async () =>
            {
                await Task.Delay(_saveDebounceDelay, ct);
                await SaveLatestAsync(ct);
                return true;
            }
        );

        if (exception.IsCanceled) return;
    }

    // Saves the latest version of the page.
    protected async Task SaveLatestAsync(CancellationToken ct)
    {
        if (PageMetaRef is null || PageEditorModel is null || PageSaveData is null || ComponentExceptionHandler is null || _dispose) return;

        var exception = await ComponentExceptionHandler.RunAsync(
            async () =>
            {
                await _saveLock.WaitAsync(ct);

                int targetVersion = Volatile.Read(ref _changeVersion);
                if (targetVersion <= Volatile.Read(ref _savedVersion)) return true;

                SavePageResponse saveResponse = await PageSaveData.SaveAsync(PageEditorModel, ct);

                PageEditorModel.Meta.SavedAt = saveResponse.SavedAt;
                PageMetaRef.DetermineUnPublishedChanges(saveResponse.SavedAt);
                Volatile.Write(ref _savedVersion, targetVersion);
                Errors.Remove("save");

                return true;
            },
            async () =>
            {
                _saveLock.Release();

                IsSaving = false;
                LocalSavedAt = DateTime.UtcNow;
                await InvokeAsync(StateHasChanged);
            }
        );

        if (exception.IsCanceled) return;
        if (!exception.IsSuccess && exception.Error is not null)
        {
            Errors["save"] = [$"Det gick inte att spara sidan: {exception.Error.Message}"];
        }

        if (Volatile.Read(ref _changeVersion) > Volatile.Read(ref _savedVersion))
        {
            await SaveLatestAsync(ct);
        }
    }

    // Publishes and then saves the newly changed PublishedAt and IsPublished. It also forces unpublished changes to be false.
    protected async Task PublishAsync()
    {
        if (PageMetaRef is null || PageEditorModel is null || ComponentExceptionHandler is null || PagePublishData is null || PageSaveData is null)
            return;

        CancellationTokenSource nextCts = new();
        CancellationTokenSource? previousCts = Interlocked.Exchange(ref _cts, nextCts);
        previousCts?.Cancel();
        previousCts?.Dispose();

        var exception = await ComponentExceptionHandler.RunAsync(
            async () =>
            {
                PageEditorModel.Meta.IsPublished = true;

                if (!string.IsNullOrEmpty(PageEditorModel.ContentJSON))
                {

                    PageEditorModel publishModel = new()
                    {   
                        Id = PageEditorModel.Id,
                        Meta = PageEditorModel.Meta,
                        ContentJSON = DeltaToNestedNormalizationService.Normalize(PageEditorModel.ContentJSON)
                    };

                    PageEditorModel.Meta.PublishedAt = await PagePublishData.PublishAsync(publishModel, nextCts.Token);

                    SavePageResponse saveResponse = await PageSaveData.SaveAsync(PageEditorModel, nextCts.Token);
                    PageMetaRef.ForceHasUnPublishedChangesToFalse();

                    return true;
                }
                throw new Exception("Sidan är tom.");
            },
            async () =>
            {
                PageMetaRef.IsPublishing = false;
            }
        );

        if (exception.IsCanceled) return;
        if (!exception.IsSuccess && exception.Error is not null)
        {
            Console.WriteLine($"Det gick inte att publicera sidan: {exception.Error.Message}"); // Temporary message as error display will be reworked.
            PageEditorModel.Meta.IsPublished = false;
        }
    }

    // Redacts and then saves the IsPublished and PublishedAt changes.
    protected async Task RedactAsync()
    {
        if (PageMetaRef is null || PageEditorModel is null || ComponentExceptionHandler is null || PageRedactData is null || PageSaveData is null)
            return;

        CancellationTokenSource nextCts = new();
        CancellationTokenSource? previousCts = Interlocked.Exchange(ref _cts, nextCts);
        previousCts?.Cancel();
        previousCts?.Dispose();

        var exception = await ComponentExceptionHandler.RunAsync(
            async () =>
            {
                if (await PageRedactData.RedactAsync(PageEditorModel.Id, nextCts.Token))
                {
                    PageEditorModel.Meta.IsPublished = false;
                    PageEditorModel.Meta.PublishedAt = null;
                    SavePageResponse saveResponse = await PageSaveData.SaveAsync(PageEditorModel, nextCts.Token);
                    PageMetaRef.DetermineUnPublishedChanges(saveResponse.SavedAt);
                    return true;
                }

                return true;
            },
            async () =>
            {
                PageMetaRef.IsRedacting = false;
            }
        );

        if (exception.IsCanceled) return;
        if (!exception.IsSuccess && exception.Error is not null)
        {
            Console.WriteLine($"Det gick inte att avpublicera sidan: {exception.Error.Message}"); // Temporary message as error display will be reworked.
        }
    }

    // Removes the page and redirects the user on success.
    protected async Task RemoveAsync()
    {
        if (PageRemoveData is null || PageEditorModel is null || ComponentExceptionHandler is null || PageMetaRef is null || NavigationManager is null) return;

        CancellationTokenSource nextCts = new();
        CancellationTokenSource? previousCts = Interlocked.Exchange(ref _cts, nextCts);
        previousCts?.Cancel();
        previousCts?.Dispose();

        var exception = await ComponentExceptionHandler.RunAsync(
            async () =>
            {
                if (!IsSaving && !PageMetaRef.IsPublishing && !PageMetaRef.IsRedacting)
                {
                    await PageRemoveData.RemoveAsync(PageEditorModel.Id, nextCts.Token);
                    return true;
                }
                else
                {
                    throw new Exception("Sidan sparas, publiceras eller avpubliceras.");
                }
            }
        );

        if (exception.IsCanceled) return;
        if (!exception.IsSuccess && exception.Error is not null)
        {
            Console.WriteLine($"Det gick inte att radera sidan: {exception.Error.Message}"); // Temporary...
            if (IsSaving) QueueSave(); // To continue the save after the exception.
        }
        else
        {
            Console.WriteLine($"Sidan {PageEditorModel.Meta.Title} raderades!");
            NavigationManager.NavigateTo("/admin/content/pages");
        }
    }

    public void Dispose()
    {
        _dispose = true;
        _cts?.Cancel();
        _cts?.Dispose();
        _saveLock.Dispose();

        PageEditorModel = null;
        // ResponseCache = null;
        Errors.Clear();
    }
}
