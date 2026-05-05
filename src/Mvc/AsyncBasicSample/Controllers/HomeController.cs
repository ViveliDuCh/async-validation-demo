using Microsoft.AspNetCore.Mvc;

namespace AsyncBasicSample.Controllers;

public class HomeController : Controller
{
    public IActionResult Index() => View();
}
