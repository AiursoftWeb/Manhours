using Microsoft.AspNetCore.Mvc;

namespace Aiursoft.UiStack.Views.Shared.Components.NotificationsDropdown;

public class NotificationsDropdown: ViewComponent
{
    public IViewComponentResult Invoke(NotificationsDropdownViewModel model)
    {
        return View(model);
    }
}