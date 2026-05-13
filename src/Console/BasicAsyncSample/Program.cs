// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using SharedModels.EntityClasses;
using SharedModels.ValidationClasses;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

Console.WriteLine("=== Basic Async Validation Sample ===");
Console.WriteLine("  Demonstrates the 4 API proposal scenarios from dotnet/runtime#128096.\n");

// ─── Scenario 1: No interface, mixed sync + async property-level attributes ───
Console.WriteLine("--- Scenario 1: User (sync [IsValidName] + async [UsernameAvailableAsync]) ---");
var user = new User
{
    Name = "Bob",
    Username = "admin"
};
await ValidateAndPrintAsync(user, new ValidationContext(user), "User");

// Two-phase optimization demo: sync failure skips async
Console.WriteLine("\n  Two-phase optimization: [Required] fails → [UsernameAvailableAsync] skipped");
var badUser = new User { Name = "", Username = "admin" };
await ValidateAndPrintAsync(badUser, new ValidationContext(badUser), "User (bad name)");

// ─── Scenario 2: IValidatableObject with sync cross-property Validate() ───
Console.WriteLine("\n--- Scenario 2: Order (IValidatableObject + async [AsyncProductExists]) ---");
var order = new Order
{
    ProductName = "Gadget",
    Quantity = 250,
    UnitPrice = 250.00m,
    Delay = 100
};
await ValidateAndPrintAsync(order, new ValidationContext(order), "Order");

// ─── Scenario 3: IAsyncValidatableObject with async cross-property validation ───
Console.WriteLine("\n--- Scenario 3: MoneyTransfer (IAsyncValidatableObject) ---");
var transfer = new MoneyTransfer
{
    FromAccount = "checking",
    ToAccount = "checking",
    Amount = 1000.00m
};
await ValidateAndPrintAsync(transfer, new ValidationContext(transfer), "MoneyTransfer");

// ─── Scenario 4: Async entity attribute + sync fallback timing comparison ───
Console.WriteLine("\n--- Scenario 4: Event ([AsyncDateRangeValid] — calendar service) ---");
var badEvent = new Event
{
    Title = "TBD Kickoff",
    StartDate = new DateTime(2026, 6, 1),
    EndDate = new DateTime(2026, 6, 2)
};
await ValidateAndPrintAsync(badEvent, new ValidationContext(badEvent), "Event");

// Demonstrate that sync callers get NotSupportedException for async-only attrs
Console.WriteLine("\n  Sync path with async-only [AsyncDateRangeValid] (no sync fallback):");
try
{
    Validator.TryValidateObject(badEvent, new ValidationContext(badEvent), null, true);
    Console.WriteLine("  (Should not reach here)");
}
catch (NotSupportedException ex)
{
    Console.WriteLine($"  Caught expected: {ex.Message}");
}

// Timing comparison using [AsyncDateRangeValidWithSyncFallback] — a separate model
// that overrides BOTH IsValidAsync AND IsValid for backward compat
Console.WriteLine("\n  Sync fallback timing comparison:");
Console.WriteLine("  Using a model with [AsyncDateRangeValidWithSyncFallback] — overrides both paths.\n");

var event1 = new SyncFallbackEvent { Title = "Meeting", StartDate = new DateTime(2026, 7, 1), EndDate = new DateTime(2026, 7, 2) };
var event2 = new SyncFallbackEvent { Title = "Planning", StartDate = new DateTime(2026, 8, 1), EndDate = new DateTime(2026, 8, 2) };

// Parallel async
var sw = Stopwatch.StartNew();
var r1 = new List<ValidationResult>();
var r2 = new List<ValidationResult>();
var t1 = Validator.TryValidateObjectAsync(event1, new ValidationContext(event1), r1, true).AsTask();
var t2 = Validator.TryValidateObjectAsync(event2, new ValidationContext(event2), r2, true).AsTask();
await Task.WhenAll(t1, t2);
sw.Stop();
Console.WriteLine($"  Parallel async:  {sw.ElapsedMilliseconds}ms  (both run concurrently)");

// Sync sequential — works because WithSyncFallback overrides IsValid
sw.Restart();
Validator.TryValidateObject(event1, new ValidationContext(event1), null, true);
Validator.TryValidateObject(event2, new ValidationContext(event2), null, true);
sw.Stop();
Console.WriteLine($"  Sync sequential: {sw.ElapsedMilliseconds}ms  (one after the other)");

// Local model for the sync fallback timing comparison
static async Task ValidateAndPrintAsync<T>(
    T model,
    ValidationContext context,
    string label) where T : notnull
{
    var results = new List<ValidationResult>();
    bool isValid = await Validator.TryValidateObjectAsync(
        model, context, results, validateAllProperties: true);

    Console.WriteLine($"  {label} valid: {isValid}");
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

// Local model for the Scenario 4 sync fallback timing comparison.
// Uses [AsyncDateRangeValidWithSyncFallback] which overrides BOTH IsValidAsync AND IsValid,
// so sync callers (Validator.TryValidateObject) work without throwing.
[AsyncDateRangeValidWithSyncFallback(nameof(StartDate), nameof(EndDate))]
partial class SyncFallbackEvent
{
    [Required] public string? Title { get; set; }
    [Required] public DateTime? StartDate { get; set; }
    [Required] public DateTime? EndDate { get; set; }
}
