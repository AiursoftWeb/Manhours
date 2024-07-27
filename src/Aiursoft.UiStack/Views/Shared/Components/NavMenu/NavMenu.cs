using Microsoft.AspNetCore.Mvc;

namespace Aiursoft.UiStack.Views.Shared.Components.NavMenu;

public class NavMenu : ViewComponent
{
    public IViewComponentResult Invoke(NavMenuViewModel model)
    {
        return View(model);
    }
}