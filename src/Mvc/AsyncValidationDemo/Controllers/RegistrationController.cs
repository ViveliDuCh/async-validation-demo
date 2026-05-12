using Microsoft.AspNetCore.Mvc;
using SharedModels.EntityClasses;

namespace AsyncValidationDemo.Controllers;

public class RegistrationController : Controller
{
    public IActionResult Index()
    {
        var model = new UserRegistration
        {
            Username = "admin",
            Email = "admin@example.com",
            Password = "adminPass"
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(UserRegistration model)
    {
        // MVC's async validation pipeline populates ModelState during model binding.
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        ViewBag.SuccessMessage = $"✅ Registration successful for '{model.Username}'!";
        return View(model);
    }
}
