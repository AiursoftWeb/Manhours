using Microsoft.AspNetCore.Mvc;

namespace Aiursoft.ManHours.Controllers;

public class HomeController : ControllerBase
{
    public IActionResult Index()
    {
        return Ok("Welcome to Aiursoft ManHours server! This link is not a web page! Please use it as a badge generator. You can access: '/gitlab/gitlab.aiursoft.cn/anduin/flyclass' to try!");
    }
}