using Microsoft.AspNetCore.Mvc;

namespace Aiursoft.UiStack.Components.MegaMenu;

public class MegaMenu : ViewComponent
{
    public IViewComponentResult Invoke()
    {
        var model = new MegaMenuViewModel();
        return View(model);
    }
}