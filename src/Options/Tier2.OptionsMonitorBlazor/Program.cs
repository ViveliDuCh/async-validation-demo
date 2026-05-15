using Microsoft.Extensions.Options;
using Options.Shared;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Bind with reloadOnChange: true (default for AddJsonFile in CreateBuilder).
// Use IOptionsMonitor<T> to pick up changes at runtime without restarting.
builder.Services.AddSingleton<Options.Shared.ValidationLogService>();

builder.Services.AddOptions<CloudInfoOptions>()
    .BindConfiguration("CloudInfo")
    .ValidateDataAnnotationsAsync()
    .ValidateOnStartAsync()
    .RevalidateOnChangeAsync(onRevalidationFailed: ex =>
    {
        Console.WriteLine($"[RevalidateOnChangeAsync] Config reload failed validation:");
        foreach (string failure in ex.Failures)
        {
            Console.WriteLine($"  - {failure}");
        }
    });

var app = builder.Build();

// Run async startup validation — gate the app from starting with bad config.
var asyncValidator = app.Services.GetService<IAsyncStartupValidator>();
if (asyncValidator is not null)
{
    await asyncValidator.ValidateAsync();
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<Tier2.OptionsMonitorBlazor.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();
