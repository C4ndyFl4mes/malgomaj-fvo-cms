namespace Server.UI.Layout;

public record MenuItemModel
{
    public required string Title { get; set; } 
    public required string Href { get; set; }
    public required string Icon { get; set; }
    public List<MenuItemModel> SubMenuItems { get; set; } = [];
}

public record BreadcrumbModel
{
    public required string Title { get; set; }
    public required string Href { get; set; }
}