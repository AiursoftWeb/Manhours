using Aiursoft.UiStack.Views.Shared.Components.LanguagesDropdown;
using Aiursoft.UiStack.Views.Shared.Components.MegaMenu;
using Aiursoft.UiStack.Views.Shared.Components.MessagesDropdown;
using Aiursoft.UiStack.Views.Shared.Components.NotificationsDropdown;
using Aiursoft.UiStack.Views.Shared.Components.SearchForm;
using Aiursoft.UiStack.Views.Shared.Components.UserDropdown;

namespace Aiursoft.UiStack.Views.Shared.Components.Navbar;

public class NavbarViewModel
{
    public SearchFormViewModel? SearchForm { get; init; }
    public MegaMenuViewModel? MegaMenu { get; init; }

    public MessagesDropdownViewModel? MessagesDropdown { get; init; }
    
    public NotificationsDropdownViewModel? NotificationsDropdown { get; init; }
    
    public LanguagesDropdownViewModel? LanguagesDropdown { get; init; }
    
    public UserDropdownViewModel? UserDropdown { get; init; }
}