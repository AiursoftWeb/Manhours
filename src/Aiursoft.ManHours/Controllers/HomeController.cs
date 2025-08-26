using Aiursoft.UiStack;
using Aiursoft.UiStack.Layout;
using Aiursoft.UiStack.Views.Shared.Components.FooterMenu;
using Aiursoft.UiStack.Views.Shared.Components.LanguagesDropdown;
using Aiursoft.UiStack.Views.Shared.Components.MegaMenu;
using Aiursoft.UiStack.Views.Shared.Components.MessagesDropdown;
using Aiursoft.UiStack.Views.Shared.Components.Navbar;
using Aiursoft.UiStack.Views.Shared.Components.NotificationsDropdown;
using Aiursoft.UiStack.Views.Shared.Components.SearchForm;
using Aiursoft.UiStack.Views.Shared.Components.SideAdvertisement;
using Aiursoft.UiStack.Views.Shared.Components.Sidebar;
using Aiursoft.UiStack.Views.Shared.Components.SideLogo;
using Aiursoft.UiStack.Views.Shared.Components.SideMenu;
using Aiursoft.UiStack.Views.Shared.Components.UserDropdown;
using Microsoft.AspNetCore.Mvc;

namespace Aiursoft.ManHours.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        var model = new UiStackLayoutViewModel
        {
            // General
            PageTitle = "Badge Generator",
            AppName = "ManHours",
            Theme = UiTheme.Dark,
            SidebarTheme = UiSidebarTheme.Dark,
            Layout = UiLayout.Fluid,

            // Footer
            FooterMenu = new FooterMenuViewModel
            {
                AppBrand = new Link { Text = "ManHours", Href = "/" },
                Links =
                [
                    new Link { Text = "Home", Href = "/" },
                    new Link { Text = "Privacy", Href = "/" },
                    new Link { Text = "About", Href = "/" },
                    new Link { Text = "Badge", Href = "/" },
                ]
            }
        };
        return this.UiStackView(model);
    }
}
