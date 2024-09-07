using Aiursoft.UiStack.Views.Shared.Components.MegaMenu;
using Aiursoft.UiStack.Views.Shared.Components.SearchForm;

namespace Aiursoft.UiStack.Views.Shared.Components.Navbar;

public class NavbarViewModel
{
    public SearchFormViewModel? SearchForm { get; init; }
    public MegaMenuViewModel? MegaMenu { get; init; }
}