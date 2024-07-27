using Microsoft.AspNetCore.Mvc;

namespace Aiursoft.UiStack.Views.Shared.Components.MegaMenu;

public class MegaMenu : ViewComponent
{
    public IViewComponentResult Invoke(MegaMenuViewModel model)
    {
        return View(model);
    }
}