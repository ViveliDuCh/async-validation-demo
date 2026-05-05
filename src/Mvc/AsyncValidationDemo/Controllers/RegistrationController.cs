using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
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
            Password = "SecureP@ss123"
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(UserRegistration model)
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

        ViewBag.SuccessMessage = $"✅ Registration successful for '{model.Username}'!";
        return View(model);
    }
}
