using Aiursoft.UiStack;
using Aiursoft.UiStack.Layout;
using Aiursoft.UiStack.Views.Shared.Components.FooterMenu;
using Aiursoft.UiStack.Views.Shared.Components.MegaMenu;
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
                    new Link { Text = "Aiursoft", Href = "https://www.aiursoft.com" },
                ]
            }
        };
        return this.UiStackView(model);
    }
}
