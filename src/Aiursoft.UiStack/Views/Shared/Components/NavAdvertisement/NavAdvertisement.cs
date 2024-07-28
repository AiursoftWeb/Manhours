using Microsoft.AspNetCore.Mvc;

namespace Aiursoft.UiStack.Views.Shared.Components.NavAdvertisement;

public class NavAdvertisement : ViewComponent
{
    public IViewComponentResult Invoke(NavAdvertisementViewModel model)
    {
        return View(model);
    }
}