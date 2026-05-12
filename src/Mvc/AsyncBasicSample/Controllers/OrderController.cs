using Microsoft.AspNetCore.Mvc;
using SharedModels.EntityClasses;

namespace AsyncBasicSample.Controllers;

public class OrderController : Controller
{
    public IActionResult Index()
    {
        var model = new Order
        {
            ProductName = "Unknown",
            Quantity = 1000,
            UnitPrice = 120m,
            Delay = 100
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(Order model)
    {
        // MVC's async validation pipeline populates ModelState during model binding.
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        ViewBag.SuccessMessage = $"✅ Order for {model.Quantity}x '{model.ProductName}' validated!";
        return View(model);
    }
}
