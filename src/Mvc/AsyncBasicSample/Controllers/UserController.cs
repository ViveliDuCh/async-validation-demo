using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using SharedModels.EntityClasses;

namespace AsyncBasicSample.Controllers;

public class UserController : Controller
{
    public IActionResult Index()
    {
        var model = new User { Name = "Alice", Email = "alice@contoso.com", Delay = 100 };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(User model)
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

        ViewBag.SuccessMessage = $"✅ User '{model.Name}' validated successfully!";
        return View(model);
    }
}
