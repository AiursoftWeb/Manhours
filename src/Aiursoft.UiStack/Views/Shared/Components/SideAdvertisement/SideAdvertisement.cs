using Microsoft.AspNetCore.Mvc;

namespace Aiursoft.UiStack.Views.Shared.Components.SideAdvertisement;

public class SideAdvertisement : ViewComponent
{
    public IViewComponentResult Invoke(SideAdvertisementViewModel model)
    {
        return View(model);
    }
}