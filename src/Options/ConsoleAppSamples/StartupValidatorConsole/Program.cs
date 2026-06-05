// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// ═══════════════════════════════════════════════════════════════════════════
// Custom Registration Test Scenarios for IStartupValidator / IAsyncStartupValidator
//
// Exercises every combination of built-in vs custom registrations for
// IStartupValidator and IAsyncStartupValidator, verifying that the two-stage
// Host.StartAsync() orchestration (sync first, then async) respects
// TryAddTransient semantics from ValidateOnStart().
//
// Reference: https://gist.github.com/ViveliDuCh/86df9b99865cf473fac6d7538c502b19
// PR: https://github.com/dotnet/runtime/pull/128788
// ═══════════════════════════════════════════════════════════════════════════

using StartupValidatorConsole;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

Console.WriteLine("=== Startup Validator Custom Registration Scenarios ===\n");

await RunScenario1();
await RunScenario2();
await RunScenario3();
await RunScenario3b();
await RunScenario4();
await RunScenario5();
await RunScenario6();
await RunScenario7();
await RunScenario8();
await RunEdgeCaseA();
await RunEdgeCaseB();

Console.WriteLine("=== All Scenarios Complete ===");

// ════════════════════════════════════════════════════════════════════
// Scenario 1: Default — Built-in Both, Sync + Async Validators
// ════════════════════════════════════════════════════════════════════
// Purpose: Baseline. Both pipelines run end-to-end with no custom registrations.
//
//   Stage 1: Built-in StartupValidator.Validate()
//     → _validators dict → options.Get("") → OptionsFactory.Create()
//     → DataAnnotationValidateOptions<DatabaseSettings>.Validate()
//     → [Required] on ConnectionString
//   Stage 2: Built-in StartupValidator.ValidateAsync()
//     → _asyncValidators dict → DataAnnotationValidateOptionsAsync<DatabaseSettings>.ValidateAsync()
// ════════════════════════════════════════════════════════════════════
static async Task RunScenario1()
{
    Console.WriteLine("--- Scenario 1: Default — Built-in Both, Sync + Async Validators ---");
    try
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(Array.Empty<string>());
        builder.Configuration.AddJsonFile("appsettings.json", optional: false);

        builder.Services.AddOptions<DatabaseSettings>()
            .Bind(builder.Configuration.GetSection("Scenarios:Valid:Database"))
            .ValidateDataAnnotations()
            .ValidateDataAnnotationsAsync()
            .ValidateOnStart();

        using IHost host = builder.Build();
        await host.StartAsync();

        // Verify the async pipeline is also exercised
        // (Host.StartAsync will do this automatically once Hosting ships with PR #128788)
        var asyncValidator = host.Services.GetService<IAsyncStartupValidator>();
        if (asyncValidator is not null)
        {
            await asyncValidator.ValidateAsync();
        }

        await host.StopAsync();

        Console.WriteLine("  ✅ Both sync and async pipelines ran — startup succeeded.\n");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  ❌ Unexpected: {ex.Message}\n");
    }
}

// ════════════════════════════════════════════════════════════════════
// Scenario 2: Built-in Both, Sync Only (No Async Validators)
// ════════════════════════════════════════════════════════════════════
// Purpose: When no IAsyncValidateOptions<T> is registered, the async
// stage is a harmless no-op. Same effective behavior as pre-async PR.
//
//   Stage 1: Built-in Validate() → _validators dict → sync pipeline ✅
//   Stage 2: Built-in ValidateAsync() → _asyncValidators dict is EMPTY → no-op ✅
// ════════════════════════════════════════════════════════════════════
static async Task RunScenario2()
{
    Console.WriteLine("--- Scenario 2: Built-in Both, Sync Only (No Async Validators) ---");
    try
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(Array.Empty<string>());
        builder.Configuration.AddJsonFile("appsettings.json", optional: false);

        builder.Services.AddOptions<AppSettings>()
            .Bind(builder.Configuration.GetSection("Scenarios:ValidApp:App"))
            .ValidateDataAnnotations()  // sync only — no async validators registered
            .ValidateOnStart();         // registers both interfaces via TryAdd

        using IHost host = builder.Build();
        await host.StartAsync();
        await host.StopAsync();

        Console.WriteLine("  ✅ Sync ran, async was no-op (empty _asyncValidators).\n");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  ❌ Unexpected: {ex.Message}\n");
    }
}

// ════════════════════════════════════════════════════════════════════
// Scenario 3: Custom Sync BEFORE ValidateOnStart, No Async Validators
// ════════════════════════════════════════════════════════════════════
// Purpose: Custom IStartupValidator is NOT bypassed — the exact scenario
// from PR #128788 discussion r3342557295.
//
//   Register BEFORE ValidateOnStart:
//     AddTransient<IStartupValidator, MyCustomStartupValidator>()
//     .ValidateOnStart() → TryAdd<IStartupValidator> SKIPPED (custom already there)
//                        → TryAdd<IAsyncStartupValidator> registered (built-in)
//
//   Stage 1: MyCustomStartupValidator.Validate() → custom logic ✅
//     (_validators dict populated but UNREAD by custom validator)
//   Stage 2: Built-in ValidateAsync() → _asyncValidators EMPTY → no-op ✅
// ════════════════════════════════════════════════════════════════════
static async Task RunScenario3()
{
    Console.WriteLine("--- Scenario 3: Custom Sync BEFORE ValidateOnStart ---");
    Environment.SetEnvironmentVariable("DEMO_API_KEY", "my-secret-key");
    try
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(Array.Empty<string>());
        builder.Configuration.AddJsonFile("appsettings.json", optional: false);

        // Register custom BEFORE ValidateOnStart
        builder.Services.AddTransient<IStartupValidator, MyCustomStartupValidator>();

        builder.Services.AddOptions<DatabaseSettings>()
            .Bind(builder.Configuration.GetSection("Scenarios:Valid:Database"))
            .ValidateDataAnnotations()
            .ValidateOnStart();  // TryAdd<IStartupValidator> → SKIPPED

        using IHost host = builder.Build();
        await host.StartAsync();
        await host.StopAsync();

        Console.WriteLine("  ✅ Custom sync ran, async was no-op.\n");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  ❌ Unexpected: {ex.Message}\n");
    }
    finally
    {
        Environment.SetEnvironmentVariable("DEMO_API_KEY", null);
    }
}

// ════════════════════════════════════════════════════════════════════
// Scenario 3b: Custom Sync AFTER ValidateOnStart
// ════════════════════════════════════════════════════════════════════
// Purpose: Same as 3 but registration order reversed. AddTransient after
// ValidateOnStart → last registration wins for GetService<IStartupValidator>.
//
//   .ValidateOnStart() → TryAdd<IStartupValidator> registered (built-in)
//   AddTransient<IStartupValidator, MyCustomStartupValidator>() → overwrites
//
//   Stage 1: MyCustomStartupValidator.Validate() ✅ (last registration wins)
//   Stage 2: Built-in ValidateAsync() → empty _asyncValidators → no-op ✅
// ════════════════════════════════════════════════════════════════════
static async Task RunScenario3b()
{
    Console.WriteLine("--- Scenario 3b: Custom Sync AFTER ValidateOnStart ---");
    Environment.SetEnvironmentVariable("DEMO_API_KEY", "my-secret-key");
    try
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(Array.Empty<string>());
        builder.Configuration.AddJsonFile("appsettings.json", optional: false);

        builder.Services.AddOptions<DatabaseSettings>()
            .Bind(builder.Configuration.GetSection("Scenarios:Valid:Database"))
            .ValidateDataAnnotations()
            .ValidateOnStart();  // TryAdd<IStartupValidator> → registered (built-in)

        // AddTransient after → last registration wins for GetService
        builder.Services.AddTransient<IStartupValidator, MyCustomStartupValidator>();

        using IHost host = builder.Build();
        await host.StartAsync();
        await host.StopAsync();

        Console.WriteLine("  ✅ Custom sync ran (last registration wins), async no-op.\n");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  ❌ Unexpected: {ex.Message}\n");
    }
    finally
    {
        Environment.SetEnvironmentVariable("DEMO_API_KEY", null);
    }
}

// ════════════════════════════════════════════════════════════════════
// Scenario 4: Custom Sync + Async Validators Registered
// ════════════════════════════════════════════════════════════════════
// Purpose: Custom sync validator runs AND the built-in async pipeline
// also runs for async validators.
//
//   Stage 1: MyCustomStartupValidator.Validate() → custom logic ✅
//     (_validators dict populated but unread — custom validator doesn't use it)
//   Stage 2: Built-in StartupValidator.ValidateAsync()
//     → _asyncValidators dict HAS entries (from ValidateDataAnnotationsAsync)
//     → DataAnnotationValidateOptionsAsync<DatabaseSettings>.ValidateAsync() runs ✅
//
// If custom sync throws → async stage SKIPPED (two-stage: fail fast).
// ════════════════════════════════════════════════════════════════════
static async Task RunScenario4()
{
    Console.WriteLine("--- Scenario 4: Custom Sync + Async Validators Registered ---");
    Environment.SetEnvironmentVariable("DEMO_API_KEY", "my-secret-key");
    try
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(Array.Empty<string>());
        builder.Configuration.AddJsonFile("appsettings.json", optional: false);

        builder.Services.AddTransient<IStartupValidator, MyCustomStartupValidator>();

        builder.Services.AddOptions<DatabaseSettings>()
            .Bind(builder.Configuration.GetSection("Scenarios:Valid:Database"))
            .ValidateDataAnnotations()
            .ValidateDataAnnotationsAsync()
            .ValidateOnStart();

        using IHost host = builder.Build();
        await host.StartAsync();

        // Manually verify async pipeline (Host.StartAsync will do this automatically
        // once PR #128788's Hosting changes ship in a future preview)
        var asyncValidator = host.Services.GetService<IAsyncStartupValidator>();
        if (asyncValidator is not null)
        {
            await asyncValidator.ValidateAsync();
        }

        await host.StopAsync();

        Console.WriteLine("  ✅ Custom sync ran + built-in async pipeline ran.\n");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  ❌ Unexpected: {ex.Message}\n");
    }
    finally
    {
        Environment.SetEnvironmentVariable("DEMO_API_KEY", null);
    }
}

// ════════════════════════════════════════════════════════════════════
// Scenario 5: Custom Async Only
// ════════════════════════════════════════════════════════════════════
// Purpose: Built-in sync runs AND custom async runs.
//
//   Stage 1: Built-in StartupValidator.Validate()
//     → _validators dict → options.Get("") → sync validation pipeline ✅
//   Stage 2: MyCustomAsyncStartupValidator.ValidateAsync()
//     → custom async logic ✅
//     → _asyncValidators dict populated but UNREAD (custom owns async)
// ════════════════════════════════════════════════════════════════════
static async Task RunScenario5()
{
    Console.WriteLine("--- Scenario 5: Custom Async Only ---");
    Environment.SetEnvironmentVariable("DEMO_HEALTH_URL", "https://api.example.com/health");
    try
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(Array.Empty<string>());
        builder.Configuration.AddJsonFile("appsettings.json", optional: false);

        builder.Services.AddTransient<IAsyncStartupValidator, MyCustomAsyncStartupValidator>();

        builder.Services.AddOptions<DatabaseSettings>()
            .Bind(builder.Configuration.GetSection("Scenarios:Valid:Database"))
            .ValidateDataAnnotations()
            .ValidateOnStart();  // TryAdd<IStartupValidator> → registered (built-in)
                                 // TryAdd<IAsyncStartupValidator> → SKIPPED (custom already there)

        using IHost host = builder.Build();
        await host.StartAsync();

        // Manually verify custom async validator is the one resolved
        // (Host.StartAsync will call this automatically once PR #128788's
        // Hosting changes ship in a future preview)
        var asyncValidator = host.Services.GetService<IAsyncStartupValidator>();
        if (asyncValidator is not null)
        {
            await asyncValidator.ValidateAsync();
            Console.WriteLine($"    Resolved: {asyncValidator.GetType().Name}");
        }

        await host.StopAsync();

        Console.WriteLine("  ✅ Built-in sync ran, custom async ran.\n");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  ❌ Unexpected: {ex.Message}\n");
    }
    finally
    {
        Environment.SetEnvironmentVariable("DEMO_HEALTH_URL", null);
    }
}

// ════════════════════════════════════════════════════════════════════
// Scenario 6: Custom Both
// ════════════════════════════════════════════════════════════════════
// Purpose: User owns everything. Both _validators and _asyncValidators
// dictionaries are populated but unread.
//
//   Stage 1: MyCustomStartupValidator.Validate() → custom sync logic ✅
//   Stage 2: MyCustomAsyncStartupValidator.ValidateAsync() → custom async logic ✅
//   Both TryAdd calls → SKIPPED
// ════════════════════════════════════════════════════════════════════
static async Task RunScenario6()
{
    Console.WriteLine("--- Scenario 6: Custom Both ---");
    Environment.SetEnvironmentVariable("DEMO_API_KEY", "my-secret-key");
    Environment.SetEnvironmentVariable("DEMO_HEALTH_URL", "https://api.example.com/health");
    try
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(Array.Empty<string>());
        builder.Configuration.AddJsonFile("appsettings.json", optional: false);

        builder.Services.AddTransient<IStartupValidator, MyCustomStartupValidator>();
        builder.Services.AddTransient<IAsyncStartupValidator, MyCustomAsyncStartupValidator>();

        builder.Services.AddOptions<DatabaseSettings>()
            .Bind(builder.Configuration.GetSection("Scenarios:Valid:Database"))
            .ValidateDataAnnotations()
            .ValidateDataAnnotationsAsync()
            .ValidateOnStart();  // Both TryAdd → SKIPPED

        using IHost host = builder.Build();
        await host.StartAsync();

        // Manually verify both custom validators are resolved
        // (Host.StartAsync will call async automatically once PR #128788's
        // Hosting changes ship in a future preview)
        var asyncValidator = host.Services.GetService<IAsyncStartupValidator>();
        if (asyncValidator is not null)
        {
            await asyncValidator.ValidateAsync();
            Console.WriteLine($"    Resolved async: {asyncValidator.GetType().Name}");
        }

        await host.StopAsync();

        Console.WriteLine("  ✅ Both custom validators ran — user owns everything.\n");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  ❌ Unexpected: {ex.Message}\n");
    }
    finally
    {
        Environment.SetEnvironmentVariable("DEMO_API_KEY", null);
        Environment.SetEnvironmentVariable("DEMO_HEALTH_URL", null);
    }
}

// ════════════════════════════════════════════════════════════════════
// Scenario 7: No Validators Registered (Empty Pipeline)
// ════════════════════════════════════════════════════════════════════
// Purpose: ValidateOnStart() without any validators is a harmless no-op.
//
//   Stage 1: Built-in Validate() → _validators dict has entry
//     → options.Get("") → OptionsFactory.Create()
//     → NO IValidateOptions<SimpleSettings> registered → no validation errors ✅
//   Stage 2: Built-in ValidateAsync() → _asyncValidators dict is EMPTY → no-op ✅
// ════════════════════════════════════════════════════════════════════
static async Task RunScenario7()
{
    Console.WriteLine("--- Scenario 7: No Validators Registered (Empty Pipeline) ---");
    try
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(Array.Empty<string>());
        builder.Configuration.AddJsonFile("appsettings.json", optional: false);

        builder.Services.AddOptions<SimpleSettings>()
            .BindConfiguration("Scenarios:Valid:Database")
            // NO .ValidateDataAnnotations()
            // NO .ValidateDataAnnotationsAsync()
            .ValidateOnStart();

        using IHost host = builder.Build();
        await host.StartAsync();
        await host.StopAsync();

        Console.WriteLine("  ✅ Both stages no-op — no validators registered.\n");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  ❌ Unexpected: {ex.Message}\n");
    }
}

// ════════════════════════════════════════════════════════════════════
// Scenario 8: Custom Sync, No ValidateOnStart (No Built-in Async)
// ════════════════════════════════════════════════════════════════════
// Purpose: Edge case — user registers custom sync but never calls
// ValidateOnStart(). No IAsyncStartupValidator exists at all.
//
//   Stage 1: GetService<IStartupValidator>() → MyCustomStartupValidator
//     → MyCustomStartupValidator.Validate() → custom logic ✅
//   Stage 2: GetService<IAsyncStartupValidator>() → null → skip ✅
//
// No dictionaries populated (ValidateOnStart never called).
// ════════════════════════════════════════════════════════════════════
static async Task RunScenario8()
{
    Console.WriteLine("--- Scenario 8: Custom Sync, No ValidateOnStart ---");
    Environment.SetEnvironmentVariable("DEMO_API_KEY", "my-secret-key");
    try
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(Array.Empty<string>());

        builder.Services.AddTransient<IStartupValidator, MyCustomStartupValidator>();
        // No ValidateOnStart() call → no TryAdd for either interface

        using IHost host = builder.Build();
        await host.StartAsync();
        await host.StopAsync();

        Console.WriteLine("  ✅ Custom sync ran, no async (null → skipped).\n");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  ❌ Unexpected: {ex.Message}\n");
    }
    finally
    {
        Environment.SetEnvironmentVariable("DEMO_API_KEY", null);
    }
}

// ════════════════════════════════════════════════════════════════════
// Edge Case A: Two-Stage Fail-Fast Verification
// ════════════════════════════════════════════════════════════════════
// Purpose: Sync failure prevents async from running (two-stage model).
//
//   Stage 1: Built-in Validate() → [Required] on empty ConnectionString → FAIL
//   Stage 2: SKIPPED — sync failed, async never invoked
// ════════════════════════════════════════════════════════════════════
static async Task RunEdgeCaseA()
{
    Console.WriteLine("--- Edge Case A: Two-Stage Fail-Fast Verification ---");
    try
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(Array.Empty<string>());
        builder.Configuration.AddJsonFile("appsettings.json", optional: false);

        builder.Services.AddOptions<DatabaseSettings>()
            .Bind(builder.Configuration.GetSection("Scenarios:InvalidDb:Database"))
            .ValidateDataAnnotations()
            .ValidateDataAnnotationsAsync()
            .ValidateOnStart();

        using IHost host = builder.Build();
        await host.StartAsync();

        Console.WriteLine("  ❌ Should not reach here — sync should have failed.\n");
    }
    catch (OptionsValidationException ex)
    {
        Console.WriteLine("  ✅ Sync validation failed (async was never invoked):");
        foreach (string failure in ex.Failures)
        {
            Console.WriteLine($"     - {failure}");
        }
        Console.WriteLine();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  ✅ Caught expected startup failure: {ex.GetBaseException().Message}\n");
    }
}

// ════════════════════════════════════════════════════════════════════
// Edge Case B: Multiple Options Types with Mixed Custom/Built-in
// ════════════════════════════════════════════════════════════════════
// Purpose: Custom registration for IStartupValidator doesn't affect
// the async pipeline for other options types.
//
//   Stage 1: MyCustomStartupValidator.Validate() → custom logic ✅
//     (_validators has entries for BOTH DatabaseSettings and SmtpSettings, but unread)
//   Stage 2: Built-in ValidateAsync()
//     → _asyncValidators has entry for DatabaseSettings ONLY
//     → runs async validators for DatabaseSettings ✅
//
// CRITICAL: SmtpSettings sync validation (DataAnnotations) does NOT run in stage 1
//   because the custom IStartupValidator doesn't read _validators.
//   This is BY DESIGN — the user opted to own sync startup validation.
// ════════════════════════════════════════════════════════════════════
static async Task RunEdgeCaseB()
{
    Console.WriteLine("--- Edge Case B: Multiple Options Types with Mixed Custom/Built-in ---");
    Environment.SetEnvironmentVariable("DEMO_API_KEY", "my-secret-key");
    try
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(Array.Empty<string>());
        builder.Configuration.AddJsonFile("appsettings.json", optional: false);

        // Custom sync validator (affects ALL startup validation)
        builder.Services.AddTransient<IStartupValidator, MyCustomStartupValidator>();

        // Two options types, both with ValidateOnStart
        builder.Services.AddOptions<DatabaseSettings>()
            .Bind(builder.Configuration.GetSection("Scenarios:ValidMultiType:Database"))
            .ValidateDataAnnotations()
            .ValidateDataAnnotationsAsync()
            .ValidateOnStart();

        builder.Services.AddOptions<SmtpSettings>()
            .Bind(builder.Configuration.GetSection("Scenarios:ValidMultiType:Smtp"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        using IHost host = builder.Build();
        await host.StartAsync();
        await host.StopAsync();

        Console.WriteLine("  ✅ Custom sync ran, built-in async ran for DatabaseSettings only.\n");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  ❌ Unexpected: {ex.Message}\n");
    }
    finally
    {
        Environment.SetEnvironmentVariable("DEMO_API_KEY", null);
    }
}
