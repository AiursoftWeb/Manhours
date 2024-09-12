namespace Aiursoft.UiStack.Views.Shared.Components.UserDropdown;

public class UserDropdownViewModel
{
    public required string UserName { get; init; }
    
    public required string UserAvatarUrl { get; init; }
    
    public required IconLinkGroup[] IconLinkGroups { get; init; }
}

public class IconLinkGroup
{
    public required IconLink[] Links { get; init; } = [];
}

public class IconLink
{
    public required string Icon { get; init; }
    
    public required string Text { get; init; }
    
    public required string Href { get; init; }
}