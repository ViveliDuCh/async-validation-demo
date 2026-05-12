using Microsoft.AspNetCore.Mvc;
using SharedModels.EntityClasses;

namespace AsyncBasicSample.Controllers;

public class UserRegistrationController : Controller
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

        ViewBag.SuccessMessage = $"✅ Registration for '{model.Username}' validated successfully!";
        return View(model);
    }
}
