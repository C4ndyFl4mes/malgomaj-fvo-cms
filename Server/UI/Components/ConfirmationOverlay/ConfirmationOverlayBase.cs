using Microsoft.AspNetCore.Components;
using Server.Enums;
using Server.UI.States;

namespace Server.UI.Components.ConfirmationOverlay;

public class ConfirmationOverlayBase : ComponentBase
{
    [Parameter] public string Message { get; set; } = string.Empty;
    [Parameter] public string AcceptBtnText { get; set; } = string.Empty;
    [Parameter] public bool IsDanger { get; set; } = false;
    [Parameter] public EventCallback OnAccept { get; set; }

    [CascadingParameter] private OverlayCloseContext CloseContext { get; set; } = default!;

    protected string AcceptButtonBgColor => IsDanger ? "bg-red-400" : "bg-green-400";
    protected bool IsSubmitting { get; set; } = false;

    protected async Task AcceptAsync()
    {
        try
        {
            IsSubmitting = true;
            await OnAccept.InvokeAsync();
        }
        finally
        {
            IsSubmitting = false;
            await CloseContext.CloseAsync(OverlayCloseReason.Accepted);
        }
    }

    protected Task CancelAsync() => CloseContext.CloseAsync(OverlayCloseReason.Cancelled);
}