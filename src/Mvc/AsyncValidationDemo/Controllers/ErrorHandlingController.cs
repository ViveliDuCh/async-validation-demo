using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
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
    public async Task<IActionResult> Index(UserRegistration model)
    {
        try
        {
            var results = new List<ValidationResult>();
            var context = new ValidationContext(model, HttpContext.RequestServices, null);
            bool isValid = await Validator.TryValidateObjectAsync(model, context, results, validateAllProperties: true);

            if (!isValid)
            {
                foreach (var result in results)
                {
                    foreach (var member in result.MemberNames)
                    {
                        ModelState.AddModelError(member, result.ErrorMessage ?? "Validation failed.");
                    }

                    if (!result.MemberNames.Any())
                    {
                        ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Validation failed.");
                    }
                }

                return View(model);
            }

            ViewBag.SuccessMessage = "✅ This should not appear for 'error-trigger'.";
            return View(model);
        }
        catch (InvalidOperationException)
        {
            throw;
        }
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
