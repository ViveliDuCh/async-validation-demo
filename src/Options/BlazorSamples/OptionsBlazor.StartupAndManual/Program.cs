using Microsoft.Extensions.Options;
using Options.Shared;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Share a single ValidationLogService so Razor pages can see what each
// validation pass logged.
builder.Services.AddSingleton<ValidationLogService>();

// ════════════════════════════════════════════════════════════════════
// STAGE 1 — Standard startup gate (.ValidateDataAnnotations + .ValidateOnStart)
// ────────────────────────────────────────────────────────────────────
// CloudInfoFallbackOptions uses [AsyncStorageExistsWithSyncFallback],
// which overrides BOTH IsValid(value, ctx) (cheap sync well-formedness
// check, no I/O) AND IsValidAsync(...) (real reachability probe).
//
// Because IsValid does NOT throw, the host's StartupValidator can run
// the sync DataAnnotations validator during Host.StartAsync() without
// crashing. On .NET 11+, ValidateDataAnnotations() also registers
// IAsyncValidateOptions<T>, so the same DataAnnotationValidateOptions<T>
// instance is available for the manual async pass below.
// ════════════════════════════════════════════════════════════════════
builder.Services.AddOptions<CloudInfoFallbackOptions>()
    .BindConfiguration("CloudInfo")
    .ValidateDataAnnotations()    // registers IValidateOptions<T> + IAsyncValidateOptions<T> on .NET 11+
    .ValidateOnStart();           // host runs the sync StartupValidator at Host.StartAsync()

var app = builder.Build();

// ════════════════════════════════════════════════════════════════════
// STAGE 2 — Manual validation passes (sync, then async) AFTER Build()
// ────────────────────────────────────────────────────────────────────
// .ValidateOnStart() above already triggered the SYNC gate inside
// Host.StartAsync() (which app.Run() will invoke). To demonstrate the
// pattern of calling both validators directly — for diagnostics, for
// pre-flight config dumps, or for revalidation outside the startup
// flow — we resolve each interface and call it ourselves here too.
//
// This is the same shape of code the hosting layer runs internally,
// just made visible so users can see the two gates as separate calls.
// ════════════════════════════════════════════════════════════════════
var current = new CloudInfoFallbackOptions();
app.Configuration.GetSection("CloudInfo").Bind(current);

// 1) Manual SYNC pass — uses the cheap sync fallback in
//    AsyncStorageExistsWithSyncFallbackAttribute.IsValid (well-formedness).
Console.WriteLine("[Manual] SYNC pass (Validate)…");
foreach (IValidateOptions<CloudInfoFallbackOptions> sv in
         app.Services.GetServices<IValidateOptions<CloudInfoFallbackOptions>>())
{
    ValidateOptionsResult result =
        sv.Validate(Microsoft.Extensions.Options.Options.DefaultName, current);
    if (result.Failed)
    {
        Console.WriteLine("[Manual] SYNC pass FAILED:");
        foreach (string f in result.Failures) Console.WriteLine("  - " + f);
        throw new OptionsValidationException(
            Microsoft.Extensions.Options.Options.DefaultName,
            typeof(CloudInfoFallbackOptions),
            result.Failures);
    }
}
Console.WriteLine("[Manual] SYNC pass: PASSED (well-formedness checks).");

// 2) Manual ASYNC pass — runs AsyncStorageExistsWithSyncFallback.IsValidAsync
//    (the full reachability probe).
Console.WriteLine("[Manual] ASYNC pass (ValidateAsync)…");
foreach (IAsyncValidateOptions<CloudInfoFallbackOptions> av in
         app.Services.GetServices<IAsyncValidateOptions<CloudInfoFallbackOptions>>())
{
    ValidateOptionsResult result =
        await av.ValidateAsync(Microsoft.Extensions.Options.Options.DefaultName, current);
    if (result.Failed)
    {
        Console.WriteLine("[Manual] ASYNC pass FAILED:");
        foreach (string f in result.Failures) Console.WriteLine("  - " + f);
        throw new OptionsValidationException(
            Microsoft.Extensions.Options.Options.DefaultName,
            typeof(CloudInfoFallbackOptions),
            result.Failures);
    }
}
Console.WriteLine("[Manual] ASYNC pass: PASSED (reachability probe).");

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<OptionsBlazor.StartupAndManual.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();
