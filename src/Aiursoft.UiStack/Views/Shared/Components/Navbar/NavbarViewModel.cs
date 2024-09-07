using Aiursoft.UiStack.Views.Shared.Components.MegaMenu;
using Aiursoft.UiStack.Views.Shared.Components.SearchForm;

namespace Aiursoft.UiStack.Views.Shared.Components.Navbar;

public class NavbarViewModel
{
    public MegaMenuViewModel? MegaMenu { get; init; }
    
    public SearchFormViewModel? SearchForm { get; init; }
}