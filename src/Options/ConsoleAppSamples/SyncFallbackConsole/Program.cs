// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Options.Shared;
using SyncFallbackConsole;

Console.WriteLine("=== NotSupportedException Demo: Sync Path vs Async-Only Attributes ===\n");
Console.WriteLine("AsyncStorageExistsAttribute does NOT override IsValid().");
Console.WriteLine("Base AsyncValidationAttribute.IsValid() throws NotSupportedException.");
Console.WriteLine("This demo shows what happens when sync pipelines hit that attribute,");
Console.WriteLine("and how to fix it by using the async pipeline instead.\n");

// ────────────────────────────────────────────────────────────────────
// Scenario A: Reflection — ValidateDataAnnotations (sync) hits async-only attr
// ────────────────────────────────────────────────────────────────────
Console.WriteLine("--- Scenario A: Reflection sync path → NotSupportedException ---");
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
        Console.WriteLine("     → The sync Validator called IsValid() on AsyncStorageExistsAttribute,");
        Console.WriteLine("       which throws because it only supports IsValidAsync().\n");
    }
    catch (NotSupportedException nse)
    {
        Console.WriteLine($"  ✅ Caught NotSupportedException directly:");
        Console.WriteLine($"     \"{nse.Message}\"\n");
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
Console.WriteLine("--- Scenario B: Source-gen sync path → NotSupportedException ---");
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
        Console.WriteLine("     → The source-gen Validate() called IsValid() on AsyncStorageExistsAttribute.\n");
    }
    catch (NotSupportedException nse)
    {
        Console.WriteLine($"  ✅ Caught NotSupportedException directly:");
        Console.WriteLine($"     \"{nse.Message}\"\n");
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
Console.WriteLine("  C1: Reflection fix — ValidateDataAnnotationsAsync + ValidateOnStartAsync");
{
    try
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(Array.Empty<string>());
        builder.Configuration.AddJsonFile("appsettings.json", optional: false);

        builder.Services.AddOptions<CloudInfoOptions>()
            .BindConfiguration("CloudInfo")
            .ValidateDataAnnotationsAsync()  // async: calls TryValidateObjectAsync
            .ValidateOnStartAsync();         // triggers async validators at startup

        using IHost host = builder.Build();

        var asyncValidator = host.Services.GetService<IAsyncStartupValidator>();
        if (asyncValidator is not null)
        {
            await asyncValidator.ValidateAsync();
        }

        var options = host.Services.GetRequiredService<IOptions<CloudInfoOptions>>();
        CloudInfoOptions opts = options.Value;
        Console.WriteLine($"     ✅ Reflection async: {opts.Storage} / {opts.Region} / {opts.Endpoint}\n");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"     ❌ Unexpected failure: {ex.Message}\n");
    }
}

// C2: Source gen fix
Console.WriteLine("  C2: Source-gen fix — IAsyncValidateOptions<T> + ValidateOnStartAsync");
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
            .ValidateOnStartAsync();

        using IHost host = builder.Build();

        var asyncValidator = host.Services.GetService<IAsyncStartupValidator>();
        if (asyncValidator is not null)
        {
            await asyncValidator.ValidateAsync();
        }

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
