using Microsoft.Extensions.Options;
using Options.Shared;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ════════════════════════════════════════════════════════════════════
// APPROACH A: DataAnnotations-based (full bypass pipeline)
// Uses the new ValidateDataAnnotationsAsync() + ValidateOnStartAsync()
// extension methods — the async equivalent of the existing sync pair.
// ════════════════════════════════════════════════════════════════════
builder.Services.AddOptions<CloudInfoOptions>()
    .BindConfiguration("CloudInfo")
    .ValidateDataAnnotationsAsync()   // registers IAsyncValidateOptions<CloudInfoOptions>
    .ValidateOnStartAsync();          // registers IAsyncStartupValidator → runs at Host.StartAsync()

// ════════════════════════════════════════════════════════════════════
// APPROACH B: Lambda-based (manual sync validation — baseline comparison)
// This is the existing sync pattern — does NOT support async attributes.
// Shown here for side-by-side comparison with Approach A.
// ════════════════════════════════════════════════════════════════════
// builder.Services.AddOptions<CloudInfoOptions>()
//     .BindConfiguration("CloudInfo")
//     .Validate(opts => !string.IsNullOrWhiteSpace(opts.Storage),
//               "Cloud Info Options Storage failed validation.")
//     .ValidateOnStart();

var app = builder.Build();

// Manually invoke async startup validation until the hosting layer
// natively integrates IAsyncStartupValidator (requires full Tier 2 merge).
var asyncValidator = app.Services.GetService<Microsoft.Extensions.Options.IAsyncStartupValidator>();
if (asyncValidator is not null)
{
    await asyncValidator.ValidateAsync();
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<Tier2.OptionsBlazor.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();
