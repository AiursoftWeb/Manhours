using Microsoft.AspNetCore.Mvc;

namespace Aiursoft.Manhours.Views.Shared.Components.MarketingFooter;

public class MarketingFooter : ViewComponent
{
    public IViewComponentResult Invoke(MarketingFooterViewModel? model = null)
    {
        model ??= new MarketingFooterViewModel();
        return View(model);
    }
}
