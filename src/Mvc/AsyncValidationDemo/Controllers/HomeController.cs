using Microsoft.AspNetCore.Mvc;

namespace AsyncValidationDemo.Controllers;

public class HomeController : Controller
{
    public IActionResult Index() => View();
}
