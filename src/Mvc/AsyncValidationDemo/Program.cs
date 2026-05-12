// MVC Web App — Async validation via the built-in MVC pipeline
// The MVC DataAnnotationsModelValidator now implements IAsyncModelValidator,
// so ValidationVisitor.ValidateNodeAsync() calls ValidateAsync() during model binding.
// DI integration, IAsyncValidatableObject, and infrastructure failure handling
// all work natively through the async-aware model binding pipeline.

using SharedModels.ServiceClasses;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
// Built-in DataAnnotationsModelValidator now supports IAsyncModelValidator —
// no need to clear ModelValidatorProviders or call Validator.TryValidateObjectAsync manually.
builder.Services.Configure<Microsoft.AspNetCore.Mvc.MvcOptions>(options =>
{
    // Show entity-level validation errors (class attrs, IAsyncValidatableObject)
    // even when property-level validation fails. Without this, MVC skips entity validators
    // when any property fails — hiding [PasswordPolicy], [AsyncRegistrationScreen], etc.
    options.ValidateComplexTypesIfChildValidationFails = true;
});
builder.Services.AddSingleton<UserService>();

var app = builder.Build();

app.UseExceptionHandler("/ErrorHandling/Error");
app.UseStaticFiles();
app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
