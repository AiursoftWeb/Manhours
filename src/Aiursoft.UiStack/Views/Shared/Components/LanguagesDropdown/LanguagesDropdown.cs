using Microsoft.AspNetCore.Mvc;

namespace Aiursoft.UiStack.Views.Shared.Components.LanguagesDropdown;

public class LanguagesDropdown : ViewComponent
{
    public IViewComponentResult Invoke(LanguagesDropdownViewModel model)
    {
        return View(model);
    }
}