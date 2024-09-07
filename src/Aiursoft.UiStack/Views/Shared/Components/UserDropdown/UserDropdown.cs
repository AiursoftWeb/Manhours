using Microsoft.AspNetCore.Mvc;

namespace Aiursoft.UiStack.Views.Shared.Components.UserDropdown;

public class UserDropdown : ViewComponent
{
    public IViewComponentResult Invoke(UserDropdownViewModel model)
    {
        return View(model);
    }
}