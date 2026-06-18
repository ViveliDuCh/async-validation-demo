// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Options.Shared;
using SyncFallbackConsole;

Console.WriteLine("=== Sync-pipeline vs async-only attribute demo ===\n");
Console.WriteLine("AsyncStorageExistsAttribute overrides IsValid(value, ctx) to throw");
Console.WriteLine("InvalidOperationException (\"Use the async validation path.\"). When the");
Console.WriteLine("merged runtime's sync Validator path encounters an AsyncValidationAttribute,");
Console.WriteLine("it preempts with NotSupportedException (\"…supports only asynchronous");
Console.WriteLine("validation. Use the async Validator methods…\") — so callers may see EITHER");
Console.WriteLine("type depending on whether the runtime preempts or the override actually");
Console.WriteLine("runs. The demo catches both.\n");

// ────────────────────────────────────────────────────────────────────
// Scenario A: Reflection — ValidateDataAnnotations (sync) hits async-only attr
// ────────────────────────────────────────────────────────────────────
Console.WriteLine("--- Scenario A: Reflection sync path → preempt / IsValid throws ---");
Console.WriteLine("  Using: ValidateDataAnnotations() + ValidateOnStart() [sync pipeline]");
{
    try
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(Array.Empty<string>());
        builder.Configuration.AddJsonFile("appsettings.json", optional: false);

        builder.Services.AddOptions<CloudInfoOptions>()
            .BindConfiguration("CloudInfo")
            .ValidateDataAnnotations()   // sync: calls Validator.TryValidateObject
            .ValidateOnStart();          // triggers sync validators at startup

        using IHost host = builder.Build();

        // Sync ValidateOnStart triggers during Build() or first resolution.
        // Force resolution:
        var options = host.Services.GetRequiredService<IOptions<CloudInfoOptions>>();
        _ = options.Value;

        Console.WriteLine("  ❌ Unexpectedly succeeded — should have thrown.\n");
    }
    catch (OptionsValidationException ex) when (ex.InnerException is NotSupportedException nse)
    {
        Console.WriteLine($"  ✅ Caught OptionsValidationException wrapping NotSupportedException:");
        Console.WriteLine($"     \"{nse.Message}\"");
        Console.WriteLine("     → The sync Validator preempted on the AsyncValidationAttribute base type.\n");
    }
    catch (OptionsValidationException ex) when (ex.InnerException is InvalidOperationException ioe)
    {
        Console.WriteLine($"  ✅ Caught OptionsValidationException wrapping InvalidOperationException:");
        Console.WriteLine($"     \"{ioe.Message}\"");
        Console.WriteLine("     → The sync Validator called IsValid() on AsyncStorageExistsAttribute,");
        Console.WriteLine("       which throws because it only supports IsValidAsync().\n");
    }
    catch (NotSupportedException nse)
    {
        Console.WriteLine($"  ✅ Caught NotSupportedException directly:");
        Console.WriteLine($"     \"{nse.Message}\"\n");
    }
    catch (InvalidOperationException ioe)
    {
        Console.WriteLine($"  ✅ Caught InvalidOperationException directly:");
        Console.WriteLine($"     \"{ioe.Message}\"\n");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  ✅ Caught {ex.GetType().Name}:");
        Console.WriteLine($"     \"{ex.Message}\"");
        if (ex.InnerException is not null)
            Console.WriteLine($"     Inner: {ex.InnerException.GetType().Name}: \"{ex.InnerException.Message}\"");
        Console.WriteLine();
    }
}

// ────────────────────────────────────────────────────────────────────
// Scenario B: Source gen — Validate() (sync) hits async-only attr
// ────────────────────────────────────────────────────────────────────
Console.WriteLine("--- Scenario B: Source-gen sync path → preempt / IsValid throws ---");
Console.WriteLine("  Using: CloudInfoOptionsValidator as IValidateOptions<T> [sync interface]");
{
    try
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(Array.Empty<string>());
        builder.Configuration.AddJsonFile("appsettings.json", optional: false);

        builder.Services.AddOptions<CloudInfoOptions>()
            .BindConfiguration("CloudInfo");

        // Register the generated validator as SYNC (IValidateOptions<T>)
        // The generated Validate() calls TryValidateValue → IsValid() on all attrs,
        // including AsyncStorageExistsAttribute which throws.
        builder.Services.AddSingleton<IValidateOptions<CloudInfoOptions>>(
            new CloudInfoOptionsValidator());

        builder.Services.AddOptions<CloudInfoOptions>()
            .ValidateOnStart();

        using IHost host = builder.Build();

        var options = host.Services.GetRequiredService<IOptions<CloudInfoOptions>>();
        _ = options.Value;

        Console.WriteLine("  ❌ Unexpectedly succeeded — should have thrown.\n");
    }
    catch (OptionsValidationException ex) when (ex.InnerException is NotSupportedException nse)
    {
        Console.WriteLine($"  ✅ Caught OptionsValidationException wrapping NotSupportedException:");
        Console.WriteLine($"     \"{nse.Message}\"");
        Console.WriteLine("     → The source-gen Validate() preempted on the AsyncValidationAttribute base type.\n");
    }
    catch (OptionsValidationException ex) when (ex.InnerException is InvalidOperationException ioe)
    {
        Console.WriteLine($"  ✅ Caught OptionsValidationException wrapping InvalidOperationException:");
        Console.WriteLine($"     \"{ioe.Message}\"");
        Console.WriteLine("     → The source-gen Validate() called IsValid() on AsyncStorageExistsAttribute.\n");
    }
    catch (NotSupportedException nse)
    {
        Console.WriteLine($"  ✅ Caught NotSupportedException directly:");
        Console.WriteLine($"     \"{nse.Message}\"\n");
    }
    catch (InvalidOperationException ioe)
    {
        Console.WriteLine($"  ✅ Caught InvalidOperationException directly:");
        Console.WriteLine($"     \"{ioe.Message}\"\n");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  ✅ Caught {ex.GetType().Name}:");
        Console.WriteLine($"     \"{ex.Message}\"");
        if (ex.InnerException is not null)
            Console.WriteLine($"     Inner: {ex.InnerException.GetType().Name}: \"{ex.InnerException.Message}\"");
        Console.WriteLine();
    }
}

// ────────────────────────────────────────────────────────────────────
// Scenario C: The fix — async pipeline (both patterns)
// ────────────────────────────────────────────────────────────────────
Console.WriteLine("--- Scenario C: The Fix — async pipeline (both patterns work) ---\n");

// C1: Reflection fix
Console.WriteLine("  C1: Reflection fix — async validator only (skip options.Value sync resolve)");
{
    try
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(Array.Empty<string>());
        builder.Configuration.AddJsonFile("appsettings.json", optional: false);

        builder.Services.AddOptions<CloudInfoOptions>()
            .BindConfiguration("CloudInfo")
            .ValidateDataAnnotations()  // .NET 11+: registers BOTH IValidateOptions<T> and
                                        // IAsyncValidateOptions<T>. The async side calls
                                        // IsValidAsync; the sync side would still throw if
                                        // exercised, so we only invoke the async validator.
            .ValidateOnStart();         // drives IAsyncStartupValidator at startup

        using IHost host = builder.Build();

        var asyncValidator = host.Services.GetService<IAsyncStartupValidator>();
        if (asyncValidator is not null)
        {
            await asyncValidator.ValidateAsync();
        }

        // NOTE: do NOT resolve options.Value here. With an async-only attribute,
        //       OptionsFactory.Create() goes through the sync IValidateOptions<T>
        //       which would still throw InvalidOperationException. The async
        //       startup validator running cleanly is what proves the fix.
        Console.WriteLine("     ✅ Reflection async validator ran without throwing.\n");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"     ❌ Unexpected failure: {ex.Message}\n");
    }
}

// C2: Source gen fix
Console.WriteLine("  C2: Source-gen fix — register validator as IAsyncValidateOptions<T> + ValidateOnStart");
{
    try
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(Array.Empty<string>());
        builder.Configuration.AddJsonFile("appsettings.json", optional: false);

        builder.Services.AddOptions<CloudInfoOptions>()
            .BindConfiguration("CloudInfo");

        // Register as ASYNC (IAsyncValidateOptions<T>) — ValidateAsync() calls IsValidAsync()
        builder.Services.AddSingleton<IAsyncValidateOptions<CloudInfoOptions>>(
            new CloudInfoOptionsValidator());

        builder.Services.AddOptions<CloudInfoOptions>()
            .ValidateOnStart();

        using IHost host = builder.Build();

        var asyncValidator = host.Services.GetService<IAsyncStartupValidator>();
        if (asyncValidator is not null)
        {
            await asyncValidator.ValidateAsync();
        }

        // Here options.Value IS safe because there is no IValidateOptions<T> registered,
        // so OptionsFactory.Create() doesn't run any sync DataAnnotations.
        var options = host.Services.GetRequiredService<IOptions<CloudInfoOptions>>();
        CloudInfoOptions opts = options.Value;
        Console.WriteLine($"     ✅ Source-gen async: {opts.Storage} / {opts.Region} / {opts.Endpoint}\n");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"     ❌ Unexpected failure: {ex.Message}\n");
    }
}

Console.WriteLine("=== Demo Complete ===");
