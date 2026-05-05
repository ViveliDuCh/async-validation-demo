// Blazor Web App — Async port of AsyncValidationConsoleDemo
// Demonstrates DI integration, two-phase validation, infrastructure failure handling,
// and CancellationToken propagation while awaiting async I/O without blocking the server thread.

using SharedModels.ServiceClasses;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Register UserService in the DI container (real app pattern)
builder.Services.AddSingleton<UserService>();

var app = builder.Build();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<AsyncValidationDemo.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();
