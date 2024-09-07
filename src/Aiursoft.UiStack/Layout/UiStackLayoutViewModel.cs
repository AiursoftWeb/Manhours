using Aiursoft.UiStack.Views.Shared.Components.FooterMenu;
using Aiursoft.UiStack.Views.Shared.Components.Navbar;
using Aiursoft.UiStack.Views.Shared.Components.Sidebar;

namespace Aiursoft.UiStack.Layout;

public enum UiTheme
{
    Dark,
    Light
}

public enum UiLayout
{
    Fluid,
    Boxed
}

public enum UiSidebarTheme
{
    Dark,
    Colored,
    Light
}

public enum UiSidebarPosition
{
    Left,
    Right
}

public enum UiSidebarBehavior
{
    Sticky,
    Fixed,
    Compact
}

public class UiStackLayoutViewModel
{
    public required string PageTitle { get; init; }
    public required string AppName { get; init; }
    public string? Description { get; init; }
    public string? CanonicalUrl { get; init; }
    
    public UiTheme Theme { get; init; } = UiTheme.Dark;
    public UiLayout Layout { get; init; } = UiLayout.Fluid;
    public UiSidebarTheme SidebarTheme { get; init; } = UiSidebarTheme.Dark;
    public UiSidebarPosition SidebarPosition { get; init; } = UiSidebarPosition.Left;
    public UiSidebarBehavior SidebarBehavior { get; init; } = UiSidebarBehavior.Sticky;

    public FooterMenuViewModel? FooterMenu { get; init; }
    
    public SidebarViewModel? Sidebar { get; init; }
    
    public NavbarViewModel? Navbar { get; init; }
}