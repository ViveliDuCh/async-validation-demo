using Microsoft.AspNetCore.Mvc;
using SharedModels.EntityClasses;

namespace AsyncValidationDemo.Controllers;

public class Scenario1Controller : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return View(new User
        {
            Name = "Bob",
            Username = "admin"
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Index(User model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        ViewBag.SuccessMessage = $"✅ User '{model.Username}' validated successfully!";
        return View(model);
    }
}
