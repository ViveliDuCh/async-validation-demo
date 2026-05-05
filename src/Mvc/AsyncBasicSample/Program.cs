// MVC Web App — Async port of SyncBasicSample
// Demonstrates manual async validation in controllers using Validator.TryValidateObjectAsync.
// Built-in MVC sync validators are disabled so AsyncValidationAttribute and
// IAsyncValidatableObject run without blocking the request thread.

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
// Remove built-in sync validation to avoid blocking — we use async validation in controllers
builder.Services.Configure<Microsoft.AspNetCore.Mvc.MvcOptions>(options =>
{
    options.ModelValidatorProviders.Clear();
});

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
