using Microsoft.AspNetCore.Mvc;

namespace Aiursoft.UiStack.Views.Shared.Components.SideLogo;

public class SideLogo : ViewComponent
{
    public IViewComponentResult Invoke(SideLogoViewModel model)
    {
        return View(model);
    }
}