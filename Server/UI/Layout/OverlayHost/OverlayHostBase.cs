using System.ComponentModel;
using Microsoft.AspNetCore.Components;
using Server.Enums;
using Server.Models;
using Server.UI.States;

namespace Server.UI.Layout.OverlayHost;

public class OverlayHostBase : ComponentBase, IDisposable
{
    [Inject] protected OverlayHostState OverlayState { get; set; } = default!;

    protected IReadOnlyList<OverlayModel> Overlays => OverlayState.Stack;

    protected override void OnInitialized()
    {
        OverlayState.PropertyChanged += HandleStateChanged;   
    }

    protected OverlayCloseContext CreateCloseContext(Guid id)
    {
        return new OverlayCloseContext(reason => CloseOverlayAsync(id, reason));
    }

    protected Task CloseFromHostAsync(Guid id) => CloseOverlayAsync(id, OverlayCloseReason.Closed);

    private Task CloseOverlayAsync(Guid id, OverlayCloseReason reason)
    {
        OverlayState.Detach(id);
        return Task.CompletedTask;
    }

    private void HandleStateChanged(object? sender, PropertyChangedEventArgs e)
    {
        _ = InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        OverlayState.PropertyChanged -= HandleStateChanged;
    }
}