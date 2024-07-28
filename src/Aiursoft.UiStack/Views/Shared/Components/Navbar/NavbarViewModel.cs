using Aiursoft.UiStack.Views.Shared.Components.NavAdvertisement;
using Aiursoft.UiStack.Views.Shared.Components.NavLogo;
using Aiursoft.UiStack.Views.Shared.Components.NavMenu;

namespace Aiursoft.UiStack.Views.Shared.Components.Navbar;

public class NavbarViewModel
{
    public NavLogoViewModel? NavLogo { get; init; }
    
    public NavMenuViewModel? NavMenu { get; init; }
    
    public NavAdvertisementViewModel? NavAdvertisement { get; init; }
}