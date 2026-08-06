using Microsoft.AspNetCore.Components;

namespace Server.UI.Layout.MenuItem;

public class MenuItemBase : ComponentBase
{
    [Parameter]
    public required MenuItemModel MenuItem { get; set; }

    protected bool IsOpen = false;
    protected string ParentBottomMargin => IsOpen ? "mb-0" : "mb-3";

    protected void ToggleSubMenuItems()
    {
        IsOpen = !IsOpen;
    }
}