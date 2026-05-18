// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;
using SharedModels.EntityClasses;

namespace AsyncBasicSample.Controllers;

public class Scenario3Controller : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return View(new MoneyTransfer
        {
            FromAccount = "checking",
            ToAccount = "checking",
            Amount = 1000m
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Index(MoneyTransfer model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        ViewBag.SuccessMessage = $"✅ Transfer from '{model.FromAccount}' to '{model.ToAccount}' validated successfully!";
        return View(model);
    }
}
