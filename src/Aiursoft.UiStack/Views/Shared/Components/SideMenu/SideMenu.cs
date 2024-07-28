using Microsoft.AspNetCore.Mvc;

namespace Aiursoft.UiStack.Views.Shared.Components.SideMenu;

public class SideMenu : ViewComponent
{
    public IViewComponentResult Invoke(SideMenuViewModel model)
    {
        return View(model);
    }
}