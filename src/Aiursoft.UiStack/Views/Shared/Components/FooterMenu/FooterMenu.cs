using Microsoft.AspNetCore.Mvc;

namespace Aiursoft.UiStack.Views.Shared.Components.FooterMenu;

public class FooterMenu : ViewComponent
{
    public IViewComponentResult Invoke(FooterMenuViewModel model)
    {
        return View(model);
    }
}