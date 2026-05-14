// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using CrossTypeParallelConsole;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

Console.WriteLine("=== Cross-Options-Type Parallel Validation Demo ===\n");
Console.WriteLine("This demo registers TWO independent options types, each with");
Console.WriteLine("ValidateOnStartAsync(). At startup, IAsyncStartupValidator");
Console.WriteLine("validates both types concurrently via Task.WhenAll.\n");
Console.WriteLine("Each async attribute simulates a 200ms I/O probe. If parallel,");
Console.WriteLine("total time ≈ 200ms (max), not 400ms (sum).\n");

// ────────────────────────────────────────────────────────────────────
// Scenario 1: Both valid — parallel execution, ~200ms total
// ────────────────────────────────────────────────────────────────────
Console.WriteLine("--- Scenario 1: Both Valid (parallel ≈ 200ms, not 400ms) ---");
{
    try
    {
        using IHost host = BuildHost("Scenarios:BothValid");

        var sw = Stopwatch.StartNew();
        var asyncValidator = host.Services.GetService<IAsyncStartupValidator>();
        if (asyncValidator is not null)
        {
            await asyncValidator.ValidateAsync();
        }
        sw.Stop();

        var dbOpts = host.Services.GetRequiredService<IOptionsMonitor<DatabaseSettings>>().CurrentValue;
        var cacheOpts = host.Services.GetRequiredService<IOptionsMonitor<CacheSettings>>().CurrentValue;

        Console.WriteLine($"  Database:  {dbOpts.ConnectionString}");
        Console.WriteLine($"  Cache:     {cacheOpts.Endpoint}");
        Console.WriteLine($"  ⏱️  Async validation completed in {sw.ElapsedMilliseconds}ms");
        Console.WriteLine("  ✅ Both options types validated concurrently — app starts.\n");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  ❌ Unexpected failure: {ex.Message}\n");
    }
}

// ────────────────────────────────────────────────────────────────────
// Scenario 2: Cache fails — database still validated concurrently
// ────────────────────────────────────────────────────────────────────
Console.WriteLine("--- Scenario 2: Cache Fails (database OK, cache unreachable) ---");
{
    try
    {
        using IHost host = BuildHost("Scenarios:CacheFails");

        var asyncValidator = host.Services.GetService<IAsyncStartupValidator>();
        if (asyncValidator is not null)
        {
            await asyncValidator.ValidateAsync();
        }

        Console.WriteLine("  (Should not reach here)\n");
    }
    catch (OptionsValidationException ex)
    {
        Console.WriteLine($"  ✅ Caught OptionsValidationException (single type failed):");
        foreach (string failure in ex.Failures)
        {
            Console.WriteLine($"     - {failure}");
        }
        Console.WriteLine();
    }
    catch (AggregateException ex)
    {
        Console.WriteLine($"  ✅ Caught AggregateException with {ex.InnerExceptions.Count} failure(s):");
        foreach (Exception inner in ex.InnerExceptions)
        {
            if (inner is OptionsValidationException ove)
            {
                Console.WriteLine($"     [{ove.OptionsType}]");
                foreach (string failure in ove.Failures)
                {
                    Console.WriteLine($"       - {failure}");
                }
            }
        }
        Console.WriteLine();
    }
}

// ────────────────────────────────────────────────────────────────────
// Scenario 3: Both fail — AggregateException with both failures
// ────────────────────────────────────────────────────────────────────
Console.WriteLine("--- Scenario 3: Both Fail (AggregateException wraps both) ---");
{
    try
    {
        using IHost host = BuildHost("Scenarios:BothFail");

        var sw = Stopwatch.StartNew();
        var asyncValidator = host.Services.GetService<IAsyncStartupValidator>();
        if (asyncValidator is not null)
        {
            await asyncValidator.ValidateAsync();
        }
        sw.Stop();

        Console.WriteLine("  (Should not reach here)\n");
    }
    catch (OptionsValidationException ex)
    {
        // Single failure if runtime merges into one exception
        Console.WriteLine($"  ✅ Caught OptionsValidationException:");
        foreach (string failure in ex.Failures)
        {
            Console.WriteLine($"     - {failure}");
        }
        Console.WriteLine();
    }
    catch (AggregateException ex)
    {
        Console.WriteLine($"  ✅ Caught AggregateException with {ex.InnerExceptions.Count} failure(s):");
        foreach (Exception inner in ex.InnerExceptions)
        {
            if (inner is OptionsValidationException ove)
            {
                Console.WriteLine($"     [{ove.OptionsType}]");
                foreach (string failure in ove.Failures)
                {
                    Console.WriteLine($"       - {failure}");
                }
            }
        }
        Console.WriteLine();
    }
}

Console.WriteLine("=== Demo Complete ===");

// ────────────────────────────────────────────────────────────────────
// Host builder: two options types, each with ValidateOnStartAsync()
// ────────────────────────────────────────────────────────────────────

static IHost BuildHost(string configSection)
{
    HostApplicationBuilder builder = Host.CreateApplicationBuilder(Array.Empty<string>());
    builder.Configuration.AddJsonFile("appsettings.json", optional: false);

    // ════════════════════════════════════════════════════════════════
    // KEY: Two independent options types, each registered with
    // ValidateDataAnnotationsAsync() + ValidateOnStartAsync().
    //
    // At Host.StartAsync() → IAsyncStartupValidator.ValidateAsync()
    // validates BOTH types concurrently via Task.WhenAll.
    // Total time ≈ max(DatabaseSettings delay, CacheSettings delay),
    // NOT the sum.
    // ════════════════════════════════════════════════════════════════
    builder.Services.AddOptions<DatabaseSettings>()
        .Bind(builder.Configuration.GetSection($"{configSection}:Database"))
        .ValidateDataAnnotationsAsync()
        .ValidateOnStartAsync();

    builder.Services.AddOptions<CacheSettings>()
        .Bind(builder.Configuration.GetSection($"{configSection}:Cache"))
        .ValidateDataAnnotationsAsync()
        .ValidateOnStartAsync();

    return builder.Build();
}
