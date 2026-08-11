using Microsoft.AspNetCore.Components;
using Server.UI.States;
using Server.UI.Layout;

namespace Server.UI.Pages.PanelPage;

public class PanelBase : ComponentBase
{
    [Inject]
    protected NavigationState NavigationState { get; set; } = default!;

    protected override void OnInitialized()
    {
        NavigationState.SetBreadcrumbs([
            new BreadcrumbModel
            {
                Title = "Panel",
                Href = "/admin"
            }
        ]);
    }
}