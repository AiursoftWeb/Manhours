namespace Aiursoft.UiStack.Views.Shared.Components.SideMenu;

public class SideMenuViewModel
{
    public required NavGroup[] Groups { get; init; } = [];
}

public class NavGroup
{
    public required string Name { get; init; }
    
    public required SideBarItem[] Items { get; init; } = [];
}

public abstract class SideBarItem
{
    /// <summary>
    /// Represents the Lucide icon associated with a <see cref="SideBarItem"/>. Get one from: https://lucide.dev/icons/
    ///
    /// Sample: "air-vent"
    /// </summary>
    public required string LucideIcon { get; set; }
    
    public required string Text { get; set; }
    
    public Decoration? Decoration { get; set; }
    
    public bool IsActive { get; set; }
}


public class CascadedSideBarItem : SideBarItem
{
    public required string UniqueId { get; init; }
    
    public CascadedLink[] Links { get; init; } = [];
}

public class CascadedLink
{
    public required string Text { get; init; }
    
    public required string Href { get; init; }
    
    public Decoration? Decoration { get; set; }
    
    public bool IsActive { get; set; }
}


public class LinkSideBarItem : SideBarItem
{
    public required string Href { get; init; }
}

public class Decoration
{
    public required string Text { get; init; }

    public required string ColorClass { get; init; } // Can be: primary, warning, danger, success, info, secondary, dark, light
}