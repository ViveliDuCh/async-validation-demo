using Microsoft.Extensions.Options;
using Options.Shared;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ════════════════════════════════════════════════════════════════════
// APPROACH A: DataAnnotations-based async validation (manual wire-up)
// ────────────────────────────────────────────────────────────────────
// On .NET 11+, ValidateDataAnnotations() registers BOTH
// IValidateOptions<T> (sync) and IAsyncValidateOptions<T> (async) from
// the same DataAnnotationValidateOptions<T> instance.
//
// For models that include an AsyncValidationAttribute (e.g.
// [AsyncStorageExists] on CloudInfoOptions), the SYNC side will crash
// with InvalidOperationException any time IOptions<T>.Value is resolved
// or ValidateOnStart() runs the sync startup validator. To avoid that,
// we manually register ONLY the async DataAnnotation validator below.
//
// This is the recommended workaround for async-only attributes until
// the hosting layer natively routes sync vs async (full Tier 2 merge).
// ════════════════════════════════════════════════════════════════════
builder.Services.AddOptions<CloudInfoOptions>()
    .BindConfiguration("CloudInfo");

// Register DataAnnotationValidateOptions<T> only for the async path.
// It implements both interfaces, but we only expose the async one so
// IOptions<T>.Value never runs the sync validator.
builder.Services.AddSingleton<IAsyncValidateOptions<CloudInfoOptions>>(_ => new OptionsBlazor.ManualOnly.DataAnnotationsAsyncValidateOptions<CloudInfoOptions>(Microsoft.Extensions.Options.Options.DefaultName));

// ════════════════════════════════════════════════════════════════════
// APPROACH B: Lambda-based (manual sync validation — baseline comparison)
// Shown here for side-by-side comparison with Approach A.
// ════════════════════════════════════════════════════════════════════
// builder.Services.AddOptions<CloudInfoOptions>()
//     .BindConfiguration("CloudInfo")
//     .Validate(opts => !string.IsNullOrWhiteSpace(opts.Storage),
//               "Cloud Info Options Storage failed validation.")
//     .ValidateOnStart();

var app = builder.Build();

// ════════════════════════════════════════════════════════════════════
// Manual startup validation
// ────────────────────────────────────────────────────────────────────
// Cheap sync check first (fast-fail on missing required values) using
// the same DataAnnotationValidateOptions<T>, then the async gate. We
// resolve the validator directly rather than calling IStartupValidator
// (the host's built-in sync startup validator) because IStartupValidator
// is not registered when ValidateOnStart() is not called.
// ════════════════════════════════════════════════════════════════════
var cloudInfo = new CloudInfoOptions();
app.Configuration.GetSection("CloudInfo").Bind(cloudInfo);

// 1) Sync gate — runs sync DataAnnotations attributes. Catches the
//    expected preemption on the async-only [AsyncStorageExists] attr.
var syncCheck = new Microsoft.Extensions.Options.DataAnnotationValidateOptions<CloudInfoOptions>(
    Microsoft.Extensions.Options.Options.DefaultName);
try
{
    ValidateOptionsResult syncResult =
        syncCheck.Validate(Microsoft.Extensions.Options.Options.DefaultName, cloudInfo);
    if (syncResult.Failed)
    {
        throw new OptionsValidationException(
            Microsoft.Extensions.Options.Options.DefaultName,
            typeof(CloudInfoOptions),
            syncResult.Failures);
    }
    Console.WriteLine("[StartupValidation] Sync gate: passed.");
}
catch (InvalidOperationException ex)
{
    Console.WriteLine(
        "[StartupValidation] Sync gate: preempted on async-only attribute — " +
        "delegating to async gate. (" + ex.Message + ")");
}
catch (NotSupportedException ex)
{
    Console.WriteLine(
        "[StartupValidation] Sync gate: preempted on async-only attribute — " +
        "delegating to async gate. (" + ex.Message + ")");
}

// 2) Async gate — runs IAsyncValidateOptions<T> registrations.
foreach (IAsyncValidateOptions<CloudInfoOptions> v in
         app.Services.GetServices<IAsyncValidateOptions<CloudInfoOptions>>())
{
    ValidateOptionsResult result =
        await v.ValidateAsync(Microsoft.Extensions.Options.Options.DefaultName, cloudInfo);
    if (result.Failed)
    {
        throw new OptionsValidationException(
            Microsoft.Extensions.Options.Options.DefaultName,
            typeof(CloudInfoOptions),
            result.Failures);
    }
}
Console.WriteLine("[StartupValidation] Async gate: passed.");

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<OptionsBlazor.ManualOnly.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();
