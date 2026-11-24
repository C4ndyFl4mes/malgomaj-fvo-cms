using Microsoft.AspNetCore.Components;
using Server.Enums;
using Server.UI.States;

namespace Server.UI.Components.QuickOverlay;

public class QuickOverlayBase : ComponentBase, IDisposable
{
    [Parameter] public string Message { get; set; } = string.Empty;
    [Parameter] public QuickOverlaySelection Selection { get; set; } = QuickOverlaySelection.Success;

    [CascadingParameter] private OverlayCloseContext CloseContext { get; set; } = default!;

    private readonly TimeSpan _autoCloseAfter = TimeSpan.FromSeconds(5);
    private readonly CancellationTokenSource _cts = new();

    protected string BgColor => Selection switch
    {
        QuickOverlaySelection.Success => "bg-green-400",
        QuickOverlaySelection.Warning => "bg-yellow-400",
        QuickOverlaySelection.Error => "bg-red-400",
        _ => string.Empty
    };

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        try
        {
            await Task.Delay(_autoCloseAfter, _cts.Token);
        }
        catch (OperationCanceledException) when (_cts.IsCancellationRequested)
        {
            return;
        }

        await CloseContext.CloseAsync(OverlayCloseReason.TimedOut);
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}