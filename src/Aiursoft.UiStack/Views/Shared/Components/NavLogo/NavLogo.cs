using Microsoft.AspNetCore.Mvc;

namespace Aiursoft.UiStack.Views.Shared.Components.NavLogo;

public class NavLogo : ViewComponent
{
    public IViewComponentResult Invoke(NavLogoViewModel model)
    {
        return View(model);
    }
}