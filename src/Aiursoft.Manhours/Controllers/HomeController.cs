using Aiursoft.UiStack;
using Aiursoft.UiStack.Layout;
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
            Theme = UiTheme.Light,
            SidebarTheme = UiSidebarTheme.Dark
        };
        return this.UiStackView(model);
    }
}