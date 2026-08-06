using Microsoft.AspNetCore.Components;

namespace Server.UI.Layout.Menu;

public class MenuBase : ComponentBase
{
    protected List<MenuItemModel> MenuItems { get; set; } = [];

    protected override void OnInitialized()
    {
        MenuItems = [
            new MenuItemModel {
                Title = "Panelen",
                Href = "/admin",
                Icon = GetIcon("dashboard")
            },
            new MenuItemModel {
                Title = "Filer",
                Href = "/admin/files",
                Icon = GetIcon("file"),
                SubMenuItems = [
                    new MenuItemModel {
                        Title = "Bilder",
                        Href = "/admin/files/images",
                        Icon = GetIcon("images")
                    }
                ]
            },
            new MenuItemModel {
                Title = "Innehåll",
                Href = "/admin/content",
                Icon = GetIcon("content"),
                SubMenuItems = [
                    new MenuItemModel {
                        Title = "Meny",
                        Href = "/admin/content/menu",
                        Icon = GetIcon("menu")
                    },
                    new MenuItemModel {
                        Title = "Sidor",
                        Href = "/admin/content/pages",
                        Icon = GetIcon("page")
                    },
                    new MenuItemModel {
                        Title = "Media",
                        Href = "/admin/content/media",
                        Icon = GetIcon("media")
                    },
                    new MenuItemModel {
                        Title = "Bildspel",
                        Href = "/admin/content/slideshow",
                        Icon = GetIcon("slideshow")
                    },
                    new MenuItemModel {
                        Title = "Kontakt",
                        Href = "/admin/content/contact",
                        Icon = GetIcon("contact")
                    },
                    new MenuItemModel {
                        Title = "Styrelse",
                        Href = "/admin/content/board",
                        Icon = GetIcon("board")
                    },
                ]
            },
            new MenuItemModel {
                Title = "Hantering",
                Href = "/admin/management",
                Icon = GetIcon("management"),
                SubMenuItems = [
                    new MenuItemModel {
                        Title = "Användare",
                        Href = "/admin/management/users",
                        Icon = GetIcon("users")
                    },
                ]
            },
            new MenuItemModel {
                Title = "Profil",
                Href = "/admin/profile",
                Icon = GetIcon("profile")
            }
        ];
    }
    protected static string GetIcon(string icon) => $"icons/{icon}.svg";
}

