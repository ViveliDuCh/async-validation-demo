// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using SharedModels.EntityClasses;
using SharedModels.ServiceClasses;
using SharedModels.ValidationClasses;
using System.ComponentModel.DataAnnotations;
using System.Threading;

public class Program
{
    private const int DelayMs = 100;

    public static async Task Main()
    {
        Console.WriteLine("=== Async Validation Console Demo ===\n");

        var serviceProvider = new SimpleServiceProvider()
            .Register(new UserService());

        Console.WriteLine("--- Scenario 1: DI duplicate detection (UserRegistration) ---");
        var duplicateRegistration = new UserRegistration
        {
            Username = "admin",
            Email = "admin@example.com",
            Password = "SecurePass123"
        };
        await ValidateAndPrintAsync(
            duplicateRegistration,
            new ValidationContext(duplicateRegistration, serviceProvider, null));
        Console.WriteLine();

        Console.WriteLine("--- Scenario 2: IValidatableObject behavior (Event) ---");
        var eventSample = new Event
        {
            Title = "TBD Kickoff",
            StartDate = new DateTime(2026, 6, 1),
            EndDate = new DateTime(2026, 6, 2),
            Delay = DelayMs
        };
        await ValidateAndPrintAsync(
            eventSample,
            new ValidationContext(eventSample));
        Console.WriteLine();

        Console.WriteLine("--- Scenario 3: IAsyncValidatableObject behavior (Order) ---");
        var orderSample = new Order
        {
            ProductName = "Gadget",
            Quantity = 250,
            UnitPrice = 250.00m,
            Delay = DelayMs
        };
        await ValidateAndPrintAsync(
            orderSample,
            new ValidationContext(orderSample));
        Console.WriteLine();

        Console.WriteLine("--- Scenario 4: Infrastructure failure handling ---");
        var failingRegistration = new UserRegistration
        {
            Username = "error-trigger",
            Email = "new@example.com",
            Password = "SecurePass123"
        };
        try
        {
            await ValidateAndPrintAsync(
                failingRegistration,
                new ValidationContext(failingRegistration, serviceProvider, null));
            Console.WriteLine("  (Should not reach here)");
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"  Caught expected error: {ex.Message}");
        }
        Console.WriteLine();

        Console.WriteLine("--- Scenario 5: CancellationToken propagation ---");
        var cancellableOrder = new Order
        {
            ProductName = "Widget",
            Quantity = 10,
            UnitPrice = 25.00m,
            Delay = 1_000
        };

        using var cts = new CancellationTokenSource(50);
        try
        {
            var results = new List<ValidationResult>();
            await Validator.TryValidateObjectAsync(
                cancellableOrder,
                new ValidationContext(cancellableOrder),
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
            model,
            context,
            results,
            validateAllProperties: true);

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
