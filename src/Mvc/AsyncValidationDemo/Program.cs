// MVC Web App — Async port of SyncValidationDemo
// Demonstrates DI integration, IAsyncValidatableObject, and infrastructure failure handling
// using Validator.TryValidateObjectAsync inside controllers.
// Built-in MVC sync validators are disabled so async validation does not block request threads.

using SharedModels.ServiceClasses;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
// Remove built-in sync validation to avoid blocking — we use async validation in controllers
builder.Services.Configure<Microsoft.AspNetCore.Mvc.MvcOptions>(options =>
{
    options.ModelValidatorProviders.Clear();
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
