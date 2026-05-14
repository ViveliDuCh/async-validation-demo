// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using MixedSyncAsyncConsole;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

Console.WriteLine("=== Mixed Sync + Async Options Validation Demo ===\n");
Console.WriteLine("This demo chains BOTH sync and async validation on the same OptionsBuilder:");
Console.WriteLine("  .ValidateDataAnnotations()      → sync [Required], [Range], [AsyncSmtpReachable] sync fallback");
Console.WriteLine("  .ValidateDataAnnotationsAsync()  → async [AsyncSmtpReachable] non-blocking path");
Console.WriteLine("  .Validate(...)                   → sync lambda");
Console.WriteLine("  .ValidateAsync(...)              → async lambda");
Console.WriteLine("  .ValidateOnStart()               → triggers sync validators");
Console.WriteLine("  .ValidateOnStartAsync()          → triggers async validators");
Console.WriteLine();
Console.WriteLine("Also demonstrates [ValidateObjectMembers] for nested property");
Console.WriteLine("parallelism on the SmtpSettings.Credentials sub-object.\n");

// ────────────────────────────────────────────────────────────────────
// Scenario 1: Valid configuration — both sync and async pass
// ────────────────────────────────────────────────────────────────────
Console.WriteLine("--- Scenario 1: Valid Configuration (sync + async both pass) ---");
{
    try
    {
        using IHost host = BuildHost("Scenarios:Valid");

        // Sync validators already ran inside OptionsFactory.Create().
        // Now run async validators explicitly.
        var asyncValidator = host.Services.GetService<IAsyncStartupValidator>();
        if (asyncValidator is not null)
        {
            await asyncValidator.ValidateAsync();
        }

        var options = host.Services.GetRequiredService<IOptionsMonitor<SmtpSettings>>();
        SmtpSettings opts = options.CurrentValue;
        Console.WriteLine($"  Host:        {opts.Host}");
        Console.WriteLine($"  Port:        {opts.Port}");
        Console.WriteLine($"  TLS:         {opts.UseTls}");
        Console.WriteLine($"  Credentials: {opts.Credentials.Username} / ****");
        Console.WriteLine("  ✅ Both sync and async validation passed.\n");
    }
    catch (OptionsValidationException ex)
    {
        Console.WriteLine($"  ❌ Unexpected failure: {ex.Message}\n");
    }
}

// ────────────────────────────────────────────────────────────────────
// Scenario 2: Sync failure — [Range] and [MinLength] catch bad values
// ────────────────────────────────────────────────────────────────────
Console.WriteLine("--- Scenario 2: Sync Failure ([Range] port=0, [MinLength] short password) ---");
{
    try
    {
        using IHost host = BuildHost("Scenarios:SyncFailure");

        // Trigger sync validation by resolving options
        var options = host.Services.GetRequiredService<IOptionsMonitor<SmtpSettings>>();
        _ = options.CurrentValue;

        Console.WriteLine("  (Should not reach here)\n");
    }
    catch (OptionsValidationException ex)
    {
        Console.WriteLine($"  ✅ Sync validation caught errors (async never needed to run):");
        foreach (string failure in ex.Failures)
        {
            Console.WriteLine($"     - {failure}");
        }
        Console.WriteLine();
    }
}

// ────────────────────────────────────────────────────────────────────
// Scenario 3: Async failure — sync passes, [AsyncSmtpReachable] fails
// ────────────────────────────────────────────────────────────────────
Console.WriteLine("--- Scenario 3: Async Failure (sync OK, [AsyncSmtpReachable] fails) ---");
{
    try
    {
        using IHost host = BuildHost("Scenarios:AsyncFailure");

        // Sync validators pass (host is required ✓, port is valid ✓)
        var options = host.Services.GetRequiredService<IOptionsMonitor<SmtpSettings>>();
        _ = options.CurrentValue;
        Console.WriteLine("  Sync validation passed ✓");

        // Async validator catches unreachable host
        var asyncValidator = host.Services.GetService<IAsyncStartupValidator>();
        if (asyncValidator is not null)
        {
            await asyncValidator.ValidateAsync();
        }

        Console.WriteLine("  (Should not reach here)\n");
    }
    catch (OptionsValidationException ex)
    {
        Console.WriteLine($"  ✅ Async validation caught the unreachable host:");
        foreach (string failure in ex.Failures)
        {
            Console.WriteLine($"     - {failure}");
        }
        Console.WriteLine();
    }
}

Console.WriteLine("=== Demo Complete ===");

// ────────────────────────────────────────────────────────────────────
// Host builder: chains BOTH sync and async validation
// ────────────────────────────────────────────────────────────────────

static IHost BuildHost(string configSection)
{
    HostApplicationBuilder builder = Host.CreateApplicationBuilder(Array.Empty<string>());
    builder.Configuration.AddJsonFile("appsettings.json", optional: false);

    // ════════════════════════════════════════════════════════════════
    // KEY: Mixed sync + async validation on the SAME OptionsBuilder
    //
    // This is the exact pattern from Issue Scenario 4:
    //   .ValidateDataAnnotations()        → sync path: [Required], [Range],
    //                                       [AsyncSmtpReachable] sync fallback
    //   .ValidateDataAnnotationsAsync()   → async path: [AsyncSmtpReachable]
    //                                       non-blocking, [ValidateObjectMembers]
    //   .Validate(...)                    → sync lambda
    //   .ValidateAsync(...)               → async lambda
    //   .ValidateOnStart()                → triggers sync validators
    //   .ValidateOnStartAsync()           → triggers async validators
    //
    // The [AsyncSmtpReachable] attribute provides BOTH paths:
    //   - IsValidAsync() for the async pipeline (non-blocking)
    //   - IsValid() sync fallback for the sync pipeline (blocking)
    // ════════════════════════════════════════════════════════════════
    builder.Services.AddOptions<SmtpSettings>()
        .Bind(builder.Configuration.GetSection($"{configSection}:Smtp"))
        .ValidateDataAnnotations()
        .ValidateDataAnnotationsAsync()
        .Validate(opts => opts.Port > 0,
            "Port must be positive.")
        .ValidateAsync(async (opts, ct) =>
        {
            await Task.CompletedTask;
            return opts.Host != "localhost" || opts.Port != 25;
        }, "Default SMTP config (localhost:25) is not allowed in production.")
        .ValidateOnStart()
        .ValidateOnStartAsync();

    return builder.Build();
}
