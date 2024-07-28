using Microsoft.AspNetCore.Mvc;

namespace Aiursoft.UiStack.Views.Shared.Components.Sidebar;

public class Sidebar : ViewComponent
{
    public IViewComponentResult Invoke(SidebarViewModel model)
    {
        return View(model);
    }
}