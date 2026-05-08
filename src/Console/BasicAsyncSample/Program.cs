// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using SharedModels.EntityClasses;
using SharedModels.ServiceClasses;
using SharedModels.ValidationClasses;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

const int DelayMs = 3000;

var serviceProvider = new SimpleServiceProvider()
    .Register(new UserService());

Console.WriteLine("=== Basic Async Validation Sample ===");

Console.WriteLine("\n--- Scenario 1: UserRegistration (DI-backed async validation) ---");
var registration = new UserRegistration
{
    Username = "admin",
    Email = "admin@example.com",
    Password = "AdminPass123"
};
await ValidateAndPrintAsync(
    registration,
    new ValidationContext(registration, serviceProvider, null),
    "UserRegistration");

Console.WriteLine("\n--- Scenario 2: Event (IValidatableObject + async attributes) ---");
var badEvent = new Event
{
    Title = "TBD Kickoff",
    StartDate = new DateTime(2026, 6, 1),
    EndDate = new DateTime(2026, 6, 2),
    Delay = DelayMs
};
await ValidateAndPrintAsync(
    badEvent,
    new ValidationContext(badEvent),
    "Event");

Console.WriteLine("\nReusable entity-level attribute (sync fallback) — timing comparison");
Console.WriteLine("  [ReservedTitleCheck] overrides both IsValidAsync and IsValid (via Task.Result).");
Console.WriteLine("  Two valid Event titles validated in parallel (async) vs sequential (sync).\n");
var event1 = new Event
{
    Title = "Team Meeting",
    StartDate = new DateTime(2026, 6, 1),
    EndDate = new DateTime(2026, 6, 2),
    Delay = DelayMs
};
var event2 = new Event
{
    Title = "Planning Session",
    StartDate = new DateTime(2026, 6, 10),
    EndDate = new DateTime(2026, 6, 11),
    Delay = DelayMs
};

// Parallel async — both property validations run concurrently
var asyncResults1 = new List<ValidationResult>();
var asyncResults2 = new List<ValidationResult>();
var sw = Stopwatch.StartNew();
var ctx1 = new ValidationContext(event1) { MemberName = nameof(Event.Title) };
var ctx2 = new ValidationContext(event2) { MemberName = nameof(Event.Title) };
var task1 = Validator.TryValidatePropertyAsync(event1.Title, ctx1, asyncResults1).AsTask();
var task2 = Validator.TryValidatePropertyAsync(event2.Title, ctx2, asyncResults2).AsTask();
await Task.WhenAll(task1, task2);
sw.Stop();
Console.WriteLine($"Parallel async:   {sw.ElapsedMilliseconds}ms  (expected ~{DelayMs}ms)");

// Sync (blocking, sequential) — uses IsValid which calls IsValidAsync via Task.Result
var syncResults1 = new List<ValidationResult>();
var syncResults2 = new List<ValidationResult>();
sw.Restart();
Validator.TryValidateProperty(event1.Title, ctx1, syncResults1);
Validator.TryValidateProperty(event2.Title, ctx2, syncResults2);
sw.Stop();
Console.WriteLine($"Sync (blocking):  {sw.ElapsedMilliseconds}ms  (expected ~{DelayMs * 2}ms)");

Console.WriteLine("\n--- Scenario 3: Order (IAsyncValidatableObject + async attributes) ---");
var badOrder = new Order
{
    ProductName = "Gadget",
    Quantity = 250,
    UnitPrice = 250.00m,
    Delay = DelayMs
};
await ValidateAndPrintAsync(
    badOrder,
    new ValidationContext(badOrder),
    "Order");

static async Task ValidateAndPrintAsync<T>(
    T model,
    ValidationContext context,
    string label) where T : notnull
{
    var results = new List<ValidationResult>();
    bool isValid = await Validator.TryValidateObjectAsync(
        model,
        context,
        results,
        validateAllProperties: true);

    Console.WriteLine($"  {label} valid (async): {isValid}");
    PrintResults(results);
}

static void PrintResults(List<ValidationResult> results)
{
    if (results.Count == 0)
    {
        Console.WriteLine("  No validation errors.");
        return;
    }

    foreach (var result in results)
    {
        var members = result.MemberNames.Any()
            ? $" [Members: {string.Join(", ", result.MemberNames)}]"
            : string.Empty;
        Console.WriteLine($"  Error: {result.ErrorMessage}{members}");
    }
}
