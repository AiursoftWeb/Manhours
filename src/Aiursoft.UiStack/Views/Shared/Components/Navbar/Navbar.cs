using Microsoft.AspNetCore.Mvc;

namespace Aiursoft.UiStack.Views.Shared.Components.Navbar;

public class Navbar : ViewComponent
{
    public IViewComponentResult Invoke(NavbarViewModel model)
    {
        return View(model);
    }
}