using Microsoft.AspNetCore.Mvc;

namespace Aiursoft.UiStack.Views.Shared.Components.SearchForm;

public class SearchForm : ViewComponent
{
    public IViewComponentResult Invoke(SearchFormViewModel model)
    {
        return View(model);
    }
}