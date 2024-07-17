using Aiursoft.UiStack;
using Microsoft.AspNetCore.Mvc;

namespace Aiursoft.ManHours.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        var model = new LayoutViewModel
        {
            PageTitle = "Badge Generator",
            AppName = "ManHours",
            Theme = UiTheme.Light,
            SidebarTheme = UiSidebarTheme.Dark
        };
        return this.UiStackView(model);
    }
}