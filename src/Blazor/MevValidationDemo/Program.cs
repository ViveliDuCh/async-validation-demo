// Blazor Web App — MEV (Microsoft.Extensions.Validation) Path Demo
// Demonstrates AddValidation() source-gen path vs fallback Validator.TryValidateObjectAsync.
// [ValidatableType] local models light up the MEV path for the scenario-based pages,
// while SharedModels entities continue to exercise the runtime fallback path.

using SharedModels.ServiceClasses;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Register AddValidation() — this activates the MEV source-gen code path
// in DataAnnotationsValidator for models that have [ValidatableType].
builder.Services.AddValidation();

// Register UserService for DI-backed async validation attributes.
builder.Services.AddSingleton<UserService>();

var app = builder.Build();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<MevValidationDemo.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();
