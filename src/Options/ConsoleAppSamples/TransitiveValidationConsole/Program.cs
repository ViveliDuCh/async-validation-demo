// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using TransitiveValidationConsole;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

Console.WriteLine("=== Transitive Async Validation Demo ===\n");
Console.WriteLine("Demonstrates ValidateDataAnnotationsAsync() with recursive validation:");
Console.WriteLine("  • [ValidateObjectMembers]   → nested object validation");
Console.WriteLine("  • [ValidateEnumeratedItems]  → collection item validation");
Console.WriteLine("  • Multi-level nesting        → TenantSettings → FailoverSettings → DatabaseSettings");
Console.WriteLine("  • Circular references         → cycle detection prevents infinite loops\n");

// ────────────────────────────────────────────────────────────────────
// Scenario 1: All valid — recursive walk validates everything
// ────────────────────────────────────────────────────────────────────
Console.WriteLine("--- Scenario 1: All Valid (nested + collection all pass) ---");
{
    try
    {
        using IHost host = BuildTenantHost("Scenarios:AllValid");

        var sw = Stopwatch.StartNew();
        var asyncValidator = host.Services.GetService<IAsyncStartupValidator>();
        if (asyncValidator is not null)
        {
            await asyncValidator.ValidateAsync();
        }
        sw.Stop();

        var opts = host.Services.GetRequiredService<IOptionsMonitor<TenantSettings>>().CurrentValue;
        Console.WriteLine($"  Tenant:     {opts.TenantName}");
        Console.WriteLine($"  Database:   {opts.Database.ConnectionString}");
        Console.WriteLine($"  Failover:   {opts.Failover.Primary.ConnectionString}");
        Console.WriteLine($"  Endpoints:  {string.Join(", ", opts.Endpoints.Select(e => e.Name))}");
        Console.WriteLine($"  ⏱️  Async validation completed in {sw.ElapsedMilliseconds}ms");
        Console.WriteLine("  ✅ All nested objects and collection items validated.\n");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  ❌ Unexpected failure: {ex.Message}\n");
    }
}

// ────────────────────────────────────────────────────────────────────
// Scenario 2: Nested failure — [AsyncConnectionStringValid] on
// Database.ConnectionString fails (first-level [ValidateObjectMembers])
// ────────────────────────────────────────────────────────────────────
Console.WriteLine("--- Scenario 2: Nested Failure ([ValidateObjectMembers] → Database unreachable) ---");
{
    try
    {
        using IHost host = BuildTenantHost("Scenarios:NestedFailure");

        var asyncValidator = host.Services.GetService<IAsyncStartupValidator>();
        if (asyncValidator is not null)
        {
            await asyncValidator.ValidateAsync();
        }

        Console.WriteLine("  (Should not reach here)\n");
    }
    catch (OptionsValidationException ex)
    {
        Console.WriteLine($"  ✅ Async validation caught nested object failure:");
        foreach (string failure in ex.Failures)
        {
            Console.WriteLine($"     - {failure}");
        }
        Console.WriteLine();
    }
}

// ────────────────────────────────────────────────────────────────────
// Scenario 3: Multi-level nesting failure — failover DB unreachable
// TenantSettings → FailoverSettings → DatabaseSettings
// ────────────────────────────────────────────────────────────────────
Console.WriteLine("--- Scenario 3: Multi-Level Failure (Tenant → Failover → Database unreachable) ---");
{
    try
    {
        using IHost host = BuildTenantHost("Scenarios:MultiLevelFailure");

        var asyncValidator = host.Services.GetService<IAsyncStartupValidator>();
        if (asyncValidator is not null)
        {
            await asyncValidator.ValidateAsync();
        }

        Console.WriteLine("  (Should not reach here)\n");
    }
    catch (OptionsValidationException ex)
    {
        Console.WriteLine($"  ✅ Multi-level recursive walk caught deep-nested failure:");
        foreach (string failure in ex.Failures)
        {
            Console.WriteLine($"     - {failure}");
        }
        Console.WriteLine();
    }
}

// ────────────────────────────────────────────────────────────────────
// Scenario 4: Collection failure — one endpoint in the list is unhealthy
// [ValidateEnumeratedItems] causes per-item async validation
// ────────────────────────────────────────────────────────────────────
Console.WriteLine("--- Scenario 4: Collection Failure ([ValidateEnumeratedItems] → unhealthy endpoint) ---");
{
    try
    {
        using IHost host = BuildTenantHost("Scenarios:CollectionFailure");

        var asyncValidator = host.Services.GetService<IAsyncStartupValidator>();
        if (asyncValidator is not null)
        {
            await asyncValidator.ValidateAsync();
        }

        Console.WriteLine("  (Should not reach here)\n");
    }
    catch (OptionsValidationException ex)
    {
        Console.WriteLine($"  ✅ Collection item validation caught unhealthy endpoint:");
        foreach (string failure in ex.Failures)
        {
            Console.WriteLine($"     - {failure}");
        }
        Console.WriteLine();
    }
}

// ────────────────────────────────────────────────────────────────────
// Scenario 5: Mixed failures — sync [Required]/[Range] + async attrs
// fail at multiple levels simultaneously
// ────────────────────────────────────────────────────────────────────
Console.WriteLine("--- Scenario 5: Mixed Failures (sync + async at multiple levels) ---");
{
    try
    {
        using IHost host = BuildTenantHost("Scenarios:MixedFailures");

        var asyncValidator = host.Services.GetService<IAsyncStartupValidator>();
        if (asyncValidator is not null)
        {
            await asyncValidator.ValidateAsync();
        }

        Console.WriteLine("  (Should not reach here)\n");
    }
    catch (OptionsValidationException ex)
    {
        Console.WriteLine($"  ✅ Multiple failures across all levels:");
        foreach (string failure in ex.Failures)
        {
            Console.WriteLine($"     - {failure}");
        }
        Console.WriteLine();
    }
}

// ────────────────────────────────────────────────────────────────────
// Scenario 6: Circular reference — cycle detection prevents infinite loop
// ────────────────────────────────────────────────────────────────────
Console.WriteLine("--- Scenario 6: Circular Reference (cycle detection) ---");
{
    try
    {
        var services = new ServiceCollection();

        // Manually configure circular options (can't represent in JSON)
        services.AddOptions<CircularParent>()
            .Configure(opts =>
            {
                opts.Name = "Parent";
                opts.Child = new CircularChild
                {
                    Name = "Child",
                    BackRef = opts // ← circular reference!
                };
            })
            .ValidateDataAnnotations()
            .ValidateOnStart();

        using ServiceProvider sp = services.BuildServiceProvider();
        var asyncValidator = sp.GetService<IAsyncStartupValidator>();
        if (asyncValidator is not null)
        {
            await asyncValidator.ValidateAsync();
        }

        Console.WriteLine("  ✅ Circular reference handled — cycle detection prevented infinite loop.");
        Console.WriteLine("     Parent → Child → BackRef(Parent) → (already visited, skipped)\n");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  ❌ Unexpected: {ex.GetType().Name}: {ex.Message}\n");
    }
}

Console.WriteLine("=== Demo Complete ===");

// ════════════════════════════════════════════════════════════════════
// Host builder
// ════════════════════════════════════════════════════════════════════

static IHost BuildTenantHost(string configSection)
{
    HostApplicationBuilder builder = Host.CreateApplicationBuilder(Array.Empty<string>());
    builder.Configuration.AddJsonFile("appsettings.json", optional: false);

    // ════════════════════════════════════════════════════════════════
    // KEY: ValidateDataAnnotationsAsync() recursively validates:
    //   • [ValidateObjectMembers]  → nested DatabaseSettings, FailoverSettings
    //   • [ValidateEnumeratedItems] → each EndpointEntry in the list
    //   • Multi-level: TenantSettings → FailoverSettings → DatabaseSettings
    //   • Cycle detection via visited HashSet (for circular references)
    //
    // The recursive walk calls Validator.TryValidateObjectAsync on each
    // nested object, so async attrs like [AsyncConnectionStringValid]
    // on deeply nested properties are exercised.
    // ════════════════════════════════════════════════════════════════
    builder.Services.AddOptions<TenantSettings>()
        .Bind(builder.Configuration.GetSection($"{configSection}:Tenant"))
        .ValidateDataAnnotations()
        .ValidateOnStart();

    return builder.Build();
}
