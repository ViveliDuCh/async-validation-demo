// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;
using SharedModels.EntityClasses;

namespace AsyncValidationDemo.Controllers;

public class Scenario4Controller : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return View(new Event
        {
            Title = "Test Event",
            StartDate = new DateTime(2027, 1, 1),
            EndDate = new DateTime(2026, 12, 31)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Index(Event model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        ViewBag.SuccessMessage = $"✅ Event '{model.Title}' validated successfully!";
        return View(model);
    }
}
