// Blazor Web App — Field-Level Async Validation Demo
// Demonstrates the new DataAnnotationsValidator async field-level validation:
// OnFieldChanged → Validator.TryValidatePropertyAsync, IsValidationPending,
// IsValidationFaulted, ValidateAsync(), and automatic cancellation via AddValidationTask.

using SharedModels.ServiceClasses;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Register UserService in the DI container for DI-backed async validation attributes
builder.Services.AddSingleton<UserService>();

var app = builder.Build();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<FieldLevelValidationDemo.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();
