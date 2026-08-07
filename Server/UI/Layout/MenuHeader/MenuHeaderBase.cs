using Microsoft.AspNetCore.Components;

namespace Server.UI.Layout.MenuHeader;

public class MenuHeaderBase : ComponentBase
{
    [Inject] protected NavigationManager? NavigationManager { get; set; }

    [Parameter] public required MenuItemModel MenuItem { get; set; }

    protected bool IsOpen = false;
    protected string ParentBottomMargin => IsOpen ? "mb-0" : "mb-3";

    protected override void OnInitialized()
    {
        if (NavigationManager is not null)
        {
            string[] paths = NavigationManager.Uri.Split("/");
            if (paths.Length >= 5)
                IsOpen = $"/{paths[3]}/{paths[4]}" == MenuItem.Href;
        }
    }

    protected void ToggleSubMenuItems()
    {
        IsOpen = !IsOpen;
    }
}