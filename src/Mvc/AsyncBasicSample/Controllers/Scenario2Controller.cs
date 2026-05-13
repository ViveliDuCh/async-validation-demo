using Microsoft.AspNetCore.Mvc;
using SharedModels.EntityClasses;

namespace AsyncBasicSample.Controllers;

public class Scenario2Controller : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return View(new Order
        {
            ProductName = "Unknown",
            Quantity = 1000,
            UnitPrice = 120m,
            Delay = 100
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Index(Order model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        ViewBag.SuccessMessage = $"✅ Order for {model.Quantity}x '{model.ProductName}' validated successfully!";
        return View(model);
    }
}
