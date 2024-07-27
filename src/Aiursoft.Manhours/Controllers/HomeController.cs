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
            PageTitle = "Badge Generator",
            AppName = "ManHours",
            Theme = UiTheme.Dark,
            SidebarTheme = UiSidebarTheme.Dark,
            TopMenu = new TopMenu
            {
                MegaMenu = new MegaMenuViewModel
                {
                    MenuName = "My Mega Menu",
                    DropDowns =
                    [
                        new DropDown
                        {
                            Header = "Home",
                            Links =
                            [
                                new Link { Text = "Index", Href = "/" },
                                new Link { Text = "Privacy", Href = "/" },
                                new Link { Text = "About", Href = "/" },
                                new Link { Text = "Badge", Href = "/" },
                            ]
                        },
                        new DropDown
                        {
                            Header = "Account",
                            Links =
                            [
                                new Link { Text = "Sign in", Href = "/" },
                                new Link { Text = "Sign up", Href = "/" },
                            ]
                        }
                    ]
                }
            },
            FooterMenu = new FooterMenuViewModel
            {
                AppBrand = new Link { Text = "ManHours", Href = "https://www.youtube.com/results?search_query=test" },
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