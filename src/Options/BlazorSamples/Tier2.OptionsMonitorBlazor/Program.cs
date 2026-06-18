// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;
using Options.Shared;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Bind with reloadOnChange: true (default for AddJsonFile in CreateBuilder).
// Use IOptionsMonitor<T> to pick up changes at runtime without restarting.
builder.Services.AddSingleton<ValidationLogService>();

// On .NET 11+, ValidateDataAnnotations() registers BOTH IValidateOptions<T>
// and IAsyncValidateOptions<T>. We deliberately omit ValidateOnStart() —
// it would wire the sync IStartupValidator into Host.StartAsync(), which
// crashes on the async-only [AsyncStorageExists] attribute before the
// async validator runs. We invoke the async validator manually below.
builder.Services.AddOptions<CloudInfoOptions>()
    .BindConfiguration("CloudInfo");

// Register DataAnnotationValidateOptions<T> only for the async path.
// On .NET 11+ it implements both interfaces, but exposing only the
// async one keeps IOptions<T>.Value / IOptionsMonitor<T>.CurrentValue
// from running the sync validator (which would crash on the async-only
// [AsyncStorageExists] attribute on CloudInfoOptions).
builder.Services.AddSingleton<IAsyncValidateOptions<CloudInfoOptions>>(_ => new Tier2.OptionsMonitorBlazor.DataAnnotationsAsyncValidateOptions<CloudInfoOptions>(Microsoft.Extensions.Options.Options.DefaultName));

var app = builder.Build();

// ════════════════════════════════════════════════════════════════════
// Async re-validation on config change (manual wire-up)
// ────────────────────────────────────────────────────────────────────
// The prototype's OptionsBuilder.RevalidateOnChangeAsync(onFailed) did not
// ship in the merged Options API. We replicate it here by subscribing to
// IOptionsMonitor<T>.OnChange and invoking the registered
// IAsyncValidateOptions<T> for each reload.
// ════════════════════════════════════════════════════════════════════
var monitor = app.Services.GetRequiredService<IOptionsMonitor<CloudInfoOptions>>();
var asyncValidators = app.Services.GetServices<IAsyncValidateOptions<CloudInfoOptions>>().ToArray();

async Task RunAsyncValidationAsync(CloudInfoOptions current)
{
    foreach (IAsyncValidateOptions<CloudInfoOptions> v in asyncValidators)
    {
        ValidateOptionsResult result = await v.ValidateAsync(Microsoft.Extensions.Options.Options.DefaultName, current);
        if (result.Failed)
        {
            throw new OptionsValidationException(
                Microsoft.Extensions.Options.Options.DefaultName, typeof(CloudInfoOptions), result.Failures);
        }
    }
}

monitor.OnChange(async (current, _) =>
{
    try
    {
        await RunAsyncValidationAsync(current);
        Console.WriteLine("[Revalidate] Config reloaded: async validation passed.");
    }
    catch (OptionsValidationException ex)
    {
        Console.WriteLine("[Revalidate] Config reload failed validation:");
        foreach (string failure in ex.Failures)
        {
            Console.WriteLine($"  - {failure}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Revalidate] Unexpected revalidation error: {ex.GetType().Name}: {ex.Message}");
    }
});

// Initial async validation — gate the app from starting with bad config.
// Bind manually so we don't go through IOptions<T>.Value (which would
// trigger the sync DataAnnotations validator and crash on async-only attrs).
var initial = new CloudInfoOptions();
app.Configuration.GetSection("CloudInfo").Bind(initial);
await RunAsyncValidationAsync(initial);

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<Tier2.OptionsMonitorBlazor.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();
