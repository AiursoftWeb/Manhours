using Aiursoft.Manhours.Models.SelfHostViewModels;
using Aiursoft.Manhours.Services;
using Aiursoft.UiStack.Navigation;
using Aiursoft.WebTools.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace Aiursoft.Manhours.Controllers;

[LimitPerMin]
public class SelfHostController : Controller
{
    [RenderInNavBar(
        NavGroupName = "Deployment",
        NavGroupOrder = 2,
        CascadedLinksGroupName = "Self Host",
        CascadedLinksIcon = "server",
        CascadedLinksOrder = 1,
        LinkText = "Self host a new server",
        LinkOrder = 1)]
    public IActionResult Index()
    {
        return this.StackView(new IndexViewModel());
    }
}
