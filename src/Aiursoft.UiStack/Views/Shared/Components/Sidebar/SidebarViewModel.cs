using Aiursoft.UiStack.Views.Shared.Components.SideAdvertisement;
using Aiursoft.UiStack.Views.Shared.Components.SideLogo;
using Aiursoft.UiStack.Views.Shared.Components.SideMenu;

namespace Aiursoft.UiStack.Views.Shared.Components.Sidebar;

public class SidebarViewModel
{
    public SideLogoViewModel? SideLogo { get; init; }
    
    public SideMenuViewModel? SideMenu { get; init; }
    
    public SideAdvertisementViewModel? SideAdvertisement { get; init; }
}