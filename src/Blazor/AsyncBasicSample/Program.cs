// Blazor Web App — Async port of BasicAsyncSample
// Uses EditContext + ValidationMessageStore to surface Validator.TryValidateObjectAsync
// results in Blazor while awaiting async I/O without blocking the server thread.

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<AsyncBasicSample.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();
