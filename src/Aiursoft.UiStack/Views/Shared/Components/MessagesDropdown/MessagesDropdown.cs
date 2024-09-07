using Microsoft.AspNetCore.Mvc;

namespace Aiursoft.UiStack.Views.Shared.Components.MessagesDropdown;

public class MessagesDropdown : ViewComponent
{
    public IViewComponentResult Invoke(MessagesDropdownViewModel model)
    {
        return View(model);
    }
}