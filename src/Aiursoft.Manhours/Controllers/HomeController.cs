using Microsoft.AspNetCore.Mvc;

namespace Aiursoft.ManHours.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}