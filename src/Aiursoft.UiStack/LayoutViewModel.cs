namespace Aiursoft.UiStack;

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

public class LayoutViewModel
{
    public required string PageTitle { get; init; }
    public required string AppName { get; init; }
    public required string Description { get; init; }
    public string? CanonicalUrl { get; set; }
    
    public UiTheme Theme { get; set; } = UiTheme.Dark;
    public UiLayout Layout { get; set; } = UiLayout.Fluid;
    public UiSidebarTheme SidebarTheme { get; set; } = UiSidebarTheme.Dark;
    public UiSidebarPosition SidebarPosition { get; set; } = UiSidebarPosition.Left;
    public UiSidebarBehavior SidebarBehavior { get; set; } = UiSidebarBehavior.Sticky;
}