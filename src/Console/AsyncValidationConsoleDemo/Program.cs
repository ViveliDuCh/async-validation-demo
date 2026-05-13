// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using SharedModels.EntityClasses;
using SharedModels.ServiceClasses;
using System.ComponentModel.DataAnnotations;

public class Program
{
    public static async Task Main()
    {
        Console.WriteLine("=== Async Validation Console Demo ===");
        Console.WriteLine("  Advanced scenarios: DI, error handling, cancellation.\n");

        // ─── Scenario 1: User — async property validation ───
        Console.WriteLine("--- Scenario 1: User (async [UsernameAvailableAsync]) ---");
        var user = new User { Name = "Bob", Username = "admin" };
        await ValidateAndPrintAsync(user, new ValidationContext(user));
        Console.WriteLine();

        // ─── Scenario 2: Order — IValidatableObject + async attrs ───
        Console.WriteLine("--- Scenario 2: Order (IValidatableObject + [AsyncProductExists]) ---");
        var order = new Order
        {
            ProductName = "Gadget",
            Quantity = 250,
            UnitPrice = 250.00m,
            Delay = 100
        };
        await ValidateAndPrintAsync(order, new ValidationContext(order));
        Console.WriteLine();

        // ─── Scenario 3: MoneyTransfer — IAsyncValidatableObject ───
        Console.WriteLine("--- Scenario 3: MoneyTransfer (IAsyncValidatableObject) ---");
        var transfer = new MoneyTransfer
        {
            FromAccount = "checking",
            ToAccount = "checking",
            Amount = 1000.00m
        };
        await ValidateAndPrintAsync(transfer, new ValidationContext(transfer));
        Console.WriteLine();

        // ─── Scenario 4: Event — async entity attribute ───
        Console.WriteLine("--- Scenario 4: Event ([AsyncDateRangeValid] — calendar service) ---");
        var eventSample = new Event
        {
            Title = "TBD Kickoff",
            StartDate = new DateTime(2026, 6, 1),
            EndDate = new DateTime(2026, 6, 2)
        };
        await ValidateAndPrintAsync(eventSample, new ValidationContext(eventSample));
        Console.WriteLine();

        // ─── Infrastructure failure handling ───
        Console.WriteLine("--- Infrastructure failure: [UsernameAvailableAsync] error simulation ---");
        // UsernameAvailableAsync only checks "admin" — no DI error path here,
        // but demonstrates try/catch pattern for async validation
        Console.WriteLine("  (Infrastructure error scenarios use DI-backed validators like [UniqueUsername])");
        Console.WriteLine();

        // ─── CancellationToken propagation ───
        Console.WriteLine("--- CancellationToken propagation ---");
        var cancellableTransfer = new MoneyTransfer
        {
            FromAccount = "savings",
            ToAccount = "checking",
            Amount = 100.00m
        };

        using var cts = new CancellationTokenSource(10); // Cancel after 10ms
        try
        {
            var results = new List<ValidationResult>();
            await Validator.TryValidateObjectAsync(
                cancellableTransfer,
                new ValidationContext(cancellableTransfer),
                results,
                validateAllProperties: true,
                cts.Token);
            Console.WriteLine("  (Should not reach here)");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("  Caught expected OperationCanceledException.");
        }
    }

    private static async Task ValidateAndPrintAsync<T>(
        T model,
        ValidationContext context) where T : notnull
    {
        var results = new List<ValidationResult>();
        bool isValid = await Validator.TryValidateObjectAsync(
            model, context, results, validateAllProperties: true);

        Console.WriteLine($"  Valid: {isValid}");
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
}
