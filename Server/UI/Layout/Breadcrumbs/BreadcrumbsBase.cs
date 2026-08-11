using Microsoft.AspNetCore.Components;
using Server.UI.States;

namespace Server.UI.Layout.Breadcrumbs;

public class BreadcrumbsBase : ComponentBase, IDisposable
{
    [Inject]
    protected NavigationState NavigationState { get; set; } = default!;

    protected override void OnInitialized()
    {
        NavigationState?.OnChange += StateHasChanged;
    }

    public void Dispose()
    {
        NavigationState.Dispose();
    }
}