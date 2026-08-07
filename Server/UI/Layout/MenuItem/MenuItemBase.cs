using Microsoft.AspNetCore.Components;

namespace Server.UI.Layout.MenuItem;

public class MenuItemBase : ComponentBase
{
    [Parameter] public required MenuItemModel MenuItem { get; set; }
}