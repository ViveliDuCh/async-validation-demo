// MVC Web App — Async validation via the built-in MVC pipeline
// The MVC DataAnnotationsModelValidator now implements IAsyncModelValidator,
// so ValidationVisitor.ValidateNodeAsync() calls ValidateAsync() during model binding.
// AsyncValidationAttribute and IAsyncValidatableObject run natively without blocking.

using SharedModels.ServiceClasses;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
// Built-in DataAnnotationsModelValidator now supports IAsyncModelValidator —
// no need to clear ModelValidatorProviders or call Validator.TryValidateObjectAsync manually.
builder.Services.Configure<Microsoft.AspNetCore.Mvc.MvcOptions>(options =>
{
    // Show entity-level validation errors (class attrs, IValidatableObject, IAsyncValidatableObject)
    // even when property-level validation fails. Without this, MVC skips entity validators
    // when any property fails — hiding [DateRange], [AsyncScheduleCheck], etc. until all
    // property errors are fixed first.
    options.ValidateComplexTypesIfChildValidationFails = true;
});
builder.Services.AddSingleton<UserService>();

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
