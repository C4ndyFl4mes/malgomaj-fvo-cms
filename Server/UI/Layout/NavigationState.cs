using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace Server.UI.Layout;

public class NavigationState : IDisposable
{
    private readonly NavigationManager _nav;
    public event Action? OnChange;
    public List<BreadcrumbModel> Breadcrumbs { get; private set; } = [];

    public NavigationState(NavigationManager nav)
    {
        _nav = nav;
        _nav.LocationChanged += OnLocationChanged;
    }

    public void SetBreadcrumbs(List<BreadcrumbModel> breadcrumbs)
    {
        Breadcrumbs = breadcrumbs;
        OnChange?.Invoke();
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        OnChange?.Invoke();
    }

    public void Dispose() => _nav.LocationChanged -= OnLocationChanged;
}