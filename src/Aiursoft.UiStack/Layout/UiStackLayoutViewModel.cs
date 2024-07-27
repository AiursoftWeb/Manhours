using Aiursoft.UiStack.Views.Shared.Components.FooterMenu;
using Aiursoft.UiStack.Views.Shared.Components.MegaMenu;
using Aiursoft.UiStack.Views.Shared.Components.NavMenu;

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

public class TopMenu
{
    public MegaMenuViewModel? MegaMenu { get; init; }
}

public class UiStackLayoutViewModel
{
    public required string PageTitle { get; init; }
    public required string AppName { get; init; }
    public string? Description { get; init; }
    public string? CanonicalUrl { get; set; }
    
    public UiTheme Theme { get; set; } = UiTheme.Dark;
    public UiLayout Layout { get; set; } = UiLayout.Fluid;
    public UiSidebarTheme SidebarTheme { get; set; } = UiSidebarTheme.Dark;
    public UiSidebarPosition SidebarPosition { get; set; } = UiSidebarPosition.Left;
    public UiSidebarBehavior SidebarBehavior { get; set; } = UiSidebarBehavior.Sticky;

    public TopMenu TopMenu { get; init; } = new();
    
    public FooterMenuViewModel? FooterMenu { get; init; }
    
    public NavMenuViewModel? NavMenu { get; init; }
}