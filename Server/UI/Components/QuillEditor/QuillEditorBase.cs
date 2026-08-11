using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Server.UI.Components.QuillEditor;

public class QuillEditorBase : ComponentBase
{
    [Inject] protected IJSRuntime? JS { get; set; }

    [Parameter] public string ContentDeltaJSON { get; set; } = string.Empty;
    [Parameter] public EventCallback<string> ContentChanged { get; set; }

    protected IJSObjectReference? Module { get; set; }
    protected string EditorId = "page-editor";

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && JS is not null)
        {
            Module = await JS.InvokeAsync<IJSObjectReference>("import", "/js/editor.bundle.js");

            await Module.InvokeVoidAsync("initQuill", EditorId, DotNetObjectReference.Create(this));

            if (!string.IsNullOrEmpty(ContentDeltaJSON))
            {
                await Module.InvokeVoidAsync("setDeltaJSONContent", EditorId, ContentDeltaJSON);
            }
        }
    }

    [JSInvokable] public async Task UpdateDeltaJSONContent(string contentDeltaJSON)
    {
        ContentDeltaJSON = contentDeltaJSON;
        await ContentChanged.InvokeAsync(contentDeltaJSON);
    }
}