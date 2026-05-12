using Microsoft.AspNetCore.Mvc;
using SharedModels.EntityClasses;

namespace AsyncBasicSample.Controllers;

public class EventController : Controller
{
    public IActionResult Index()
    {
        var model = new Event
        {
            Title = "Test Event",
            StartDate = new DateTime(DateTime.Today.Year + 1, 1, 1),
            EndDate = new DateTime(DateTime.Today.Year, 12, 31),
            Delay = 100
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(Event model)
    {
        // MVC's async validation pipeline populates ModelState during model binding.
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        ViewBag.SuccessMessage = $"✅ Event '{model.Title}' validated!";
        return View(model);
    }
}
