// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using AsyncLambdaConsole;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Options.Shared;

Console.WriteLine("=== Async Lambda-Based Options Validation Demo ===\n");
Console.WriteLine("This demo uses .ValidateAsync<TDep>() with inline async lambdas");
Console.WriteLine("to validate options at startup, instead of implementing");
Console.WriteLine("IAsyncValidateOptions<T> as a separate class.\n");

// ────────────────────────────────────────────────────────────────────
// Scenario 1: Valid configuration — all async lambda checks pass
// ────────────────────────────────────────────────────────────────────
Console.WriteLine("--- Scenario 1: Valid Configuration ---");
{
    try
    {
        using IHost host = BuildHost("Scenarios:Valid");

        var asyncValidator = host.Services.GetService<IAsyncStartupValidator>();
        if (asyncValidator is not null)
        {
            await asyncValidator.ValidateAsync();
        }

        var options = host.Services.GetRequiredService<IOptionsMonitor<CloudInfoOptions>>();
        CloudInfoOptions opts = options.CurrentValue;
        Console.WriteLine($"  Storage:  {opts.Storage}");
        Console.WriteLine($"  Region:   {opts.Region}");
        Console.WriteLine($"  Endpoint: {opts.Endpoint}");
        Console.WriteLine("  ✅ Validation passed — app would start normally.\n");
    }
    catch (OptionsValidationException ex)
    {
        Console.WriteLine($"  ❌ Unexpected failure: {ex.Message}\n");
    }
}

// ────────────────────────────────────────────────────────────────────
// Scenario 2: Invalid endpoint — async lambda detects unreachable storage
// ────────────────────────────────────────────────────────────────────
Console.WriteLine("--- Scenario 2: Invalid Endpoint (async lambda catches it) ---");
{
    try
    {
        using IHost host = BuildHost("Scenarios:InvalidEndpoint");

        var asyncValidator = host.Services.GetService<IAsyncStartupValidator>();
        if (asyncValidator is not null)
        {
            await asyncValidator.ValidateAsync();
        }

        Console.WriteLine("  (Should not reach here)\n");
    }
    catch (OptionsValidationException ex)
    {
        Console.WriteLine($"  ✅ Caught expected OptionsValidationException:");
        foreach (string failure in ex.Failures)
        {
            Console.WriteLine($"     - {failure}");
        }
        Console.WriteLine();
    }
}

// ────────────────────────────────────────────────────────────────────
// Scenario 3: Multiple chained async lambdas — both sync and async checks
// ────────────────────────────────────────────────────────────────────
Console.WriteLine("--- Scenario 3: Multiple Chained Async Lambdas ---");
{
    try
    {
        using IHost host = BuildHostWithMultipleLambdas("Scenarios:MissingStorage");

        var asyncValidator = host.Services.GetService<IAsyncStartupValidator>();
        if (asyncValidator is not null)
        {
            await asyncValidator.ValidateAsync();
        }

        Console.WriteLine("  (Should not reach here)\n");
    }
    catch (OptionsValidationException ex)
    {
        Console.WriteLine($"  ✅ Caught expected OptionsValidationException:");
        foreach (string failure in ex.Failures)
        {
            Console.WriteLine($"     - {failure}");
        }
        Console.WriteLine();
    }
    catch (AggregateException ex)
    {
        Console.WriteLine($"  ✅ Caught expected AggregateException with {ex.InnerExceptions.Count} failure(s):");
        foreach (Exception inner in ex.InnerExceptions)
        {
            if (inner is OptionsValidationException ove)
            {
                foreach (string failure in ove.Failures)
                {
                    Console.WriteLine($"     - {failure}");
                }
            }
        }
        Console.WriteLine();
    }
}

Console.WriteLine("=== Demo Complete ===");

// ────────────────────────────────────────────────────────────────────
// Host builders
// ────────────────────────────────────────────────────────────────────

static IHost BuildHost(string configSection)
{
    HostApplicationBuilder builder = Host.CreateApplicationBuilder(Array.Empty<string>());
    builder.Configuration.AddJsonFile("appsettings.json", optional: false);

    builder.Services.AddSingleton<IStorageService, FakeStorageService>();

    // ════════════════════════════════════════════════════════════════
    // KEY API: .ValidateAsync<TDep>(async lambda) + .ValidateOnStartAsync()
    //
    // This is the async counterpart to:
    //   .Validate<IStorageService>((opts, svc) => svc.Exists(opts.Endpoint), "msg")
    //   .ValidateOnStart()
    // ════════════════════════════════════════════════════════════════
    builder.Services.AddOptions<CloudInfoOptions>()
        .BindConfiguration($"{configSection}:CloudInfo")
        .ValidateAsync<IStorageService>(async (opts, storageService, ct) =>
            await storageService.ExistsAsync(opts.Endpoint, ct),
            "Storage endpoint does not exist or is not reachable.")
        .ValidateOnStartAsync();

    return builder.Build();
}

static IHost BuildHostWithMultipleLambdas(string configSection)
{
    HostApplicationBuilder builder = Host.CreateApplicationBuilder(Array.Empty<string>());
    builder.Configuration.AddJsonFile("appsettings.json", optional: false);

    builder.Services.AddSingleton<IStorageService, FakeStorageService>();

    // Chain multiple async lambdas — each registers a separate
    // IAsyncValidateOptions<T> that runs during ValidateOnStartAsync().
    builder.Services.AddOptions<CloudInfoOptions>()
        .BindConfiguration($"{configSection}:CloudInfo")
        .ValidateAsync(async (opts, ct) =>
        {
            await Task.CompletedTask;
            return !string.IsNullOrWhiteSpace(opts.Storage);
        },
        "Cloud Info Options Storage must not be empty.")
        .ValidateAsync(async (opts, ct) =>
        {
            await Task.CompletedTask;
            return !string.IsNullOrWhiteSpace(opts.Region);
        },
        "Cloud Info Options Region must not be empty.")
        .ValidateAsync<IStorageService>(async (opts, storageService, ct) =>
            await storageService.ExistsAsync(opts.Endpoint, ct),
            "Storage endpoint does not exist or is not reachable.")
        .ValidateOnStartAsync();

    return builder.Build();
}
