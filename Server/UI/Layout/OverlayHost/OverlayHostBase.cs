using System.ComponentModel;
using Microsoft.AspNetCore.Components;
using Server.Enums;
using Server.Models;
using Server.UI.Components.QuickOverlay;
using Server.UI.States;

namespace Server.UI.Layout.OverlayHost;

public class OverlayHostBase : ComponentBase, IDisposable
{
    [Inject] protected OverlayHostState OverlayState { get; set; } = default!;

    private readonly HashSet<Guid> _closingQuickOverlayIds = []; // This is used to track QuickOverlay instances that are in the process of closing, to prevent multiple close requests for the same overlay.

    protected IReadOnlyList<OverlayModel> Overlays => OverlayState.Stack;

    protected override void OnInitialized()
    {
        OverlayState.PropertyChanged += HandleStateChanged;   
    }

    protected bool IsQuickOverlayClosing(Guid id) => _closingQuickOverlayIds.Contains(id);

    protected OverlayCloseContext CreateCloseContext(Guid id)
    {
        return new OverlayCloseContext(reason => CloseOverlayAsync(id, reason));
    }

    protected Task CloseFromHostAsync(Guid id) => CloseOverlayAsync(id, OverlayCloseReason.Closed);

    private Task CloseOverlayAsync(Guid id, OverlayCloseReason reason)
    {
        OverlayModel? overlay = Overlays.FirstOrDefault(candidate => candidate.Id == id);
        if (overlay?.ComponentType == typeof(QuickOverlay))
        {
            if (_closingQuickOverlayIds.Add(id))
            {
                return InvokeAsync(StateHasChanged);
            }

            return Task.CompletedTask;
        }

        OverlayState.Detach(id);
        return Task.CompletedTask;
    }

    protected Task CompleteQuickOverlayCloseAsync(Guid id)
    {
        if (_closingQuickOverlayIds.Remove(id))
        {
            OverlayState.Detach(id);
        }

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