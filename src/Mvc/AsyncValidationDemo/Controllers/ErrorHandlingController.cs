using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SharedModels.EntityClasses;

namespace AsyncValidationDemo.Controllers;

public class ErrorHandlingController : Controller
{
    public IActionResult Index()
    {
        var model = new UserRegistration
        {
            Username = "error-trigger",
            Email = "new@example.com",
            Password = "SecureP@ss123"
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Index(UserRegistration model)
    {
        // MVC's async validation pipeline populates ModelState during model binding.
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        ViewBag.SuccessMessage = "✅ This should not appear for 'error-trigger'.";
        return View(model);
    }

    [Route("ErrorHandling/Error")]
    public IActionResult Error()
    {
        var exceptionFeature = HttpContext.Features.Get<IExceptionHandlerFeature>();
        ViewBag.ErrorMessage = exceptionFeature?.Error.Message ?? "An unknown error occurred.";
        ViewBag.ErrorType = exceptionFeature?.Error.GetType().Name ?? "Unknown";
        return View();
    }
}
