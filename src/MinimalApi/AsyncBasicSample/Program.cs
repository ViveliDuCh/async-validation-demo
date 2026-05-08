// Minimal API — Async port of BasicAsyncSample
// Demonstrates how async validation can await costly I/O without blocking the request thread.
// Each POST endpoint awaits Validator.TryValidateObjectAsync and returns errors or success.

using SharedModels.EntityClasses;
using SharedModels.ServiceClasses;
using System.ComponentModel.DataAnnotations;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<UserService>();
var app = builder.Build();

app.MapGet("/", () => Results.Ok(new
{
    Name = "AsyncBasicSample — Minimal API",
    Description = "Async validation for attribute-only, IValidatableObject, and IAsyncValidatableObject models without blocking server threads",
    Endpoints = new[]
    {
        new { Method = "POST", Path = "/api/register/invalid", Scenario = "1 — UserRegistration with DI-backed async validation" },
        new { Method = "POST", Path = "/api/event/invalid", Scenario = "2 — Event with IValidatableObject + async attributes" },
        new { Method = "POST", Path = "/api/order/invalid", Scenario = "3 — Order with IAsyncValidatableObject + async attributes" }
    }
}));

static async Task<IResult> ValidateAndRespondAsync<T>(
    T instance,
    string scenarioName,
    IServiceProvider? serviceProvider = null) where T : notnull
{
    var results = new List<ValidationResult>();
    var context = new ValidationContext(instance, serviceProvider, null);
    bool isValid = await Validator.TryValidateObjectAsync(instance, context, results, validateAllProperties: true);

    if (isValid)
    {
        return Results.Ok(new { Scenario = scenarioName, Valid = true });
    }

    var errors = results
        .SelectMany(r => r.MemberNames.DefaultIfEmpty(string.Empty),
            (r, member) => new { Member = member, r.ErrorMessage })
        .GroupBy(e => e.Member)
        .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage ?? "Validation failed.").ToArray());

    return Results.ValidationProblem(errors, title: scenarioName);
}

// ───────────────────────────────────────────
// UserRegistration combines DI-backed async uniqueness checks, sync password policy,
// and async registration screening without blocking the request thread.
// POST /api/register/invalid  { "username": "admin", "email": "admin@example.com", "password": "adminPass" }
// ───────────────────────────────────────────
app.MapPost("/api/register/invalid", async (UserRegistration registration, IServiceProvider sp) =>
    await ValidateAndRespondAsync(registration, "UserRegistration with DI-backed async validation", sp));

// ───────────────────────────────────────────
// Event uses IValidatableObject plus sync/async entity rules:
// reserved titles, date ordering, and schedule conflicts.
// POST /api/event/invalid  { "title": "Test Event", "startDate": "2027-01-01", "endDate": "2026-12-31", "delay": 3000 }
// ───────────────────────────────────────────
app.MapPost("/api/event/invalid", async (Event ev) =>
    await ValidateAndRespondAsync(ev, "Event with IValidatableObject + async attributes"));

// ───────────────────────────────────────────
// Order uses async catalog/inventory checks plus sync/async order-total limits.
// POST /api/order/invalid  { "productName": "Unknown", "quantity": 1000, "unitPrice": 120, "delay": 3000 }
// ───────────────────────────────────────────
app.MapPost("/api/order/invalid", async (Order order) =>
    await ValidateAndRespondAsync(order, "Order with IAsyncValidatableObject + async attributes"));

app.Run();
