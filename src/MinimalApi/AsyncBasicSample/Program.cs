// Minimal API — Async port of BasicAsyncSample
// Demonstrates how async validation can await costly I/O without blocking the request thread.
// Each POST endpoint awaits Validator.TryValidateObjectAsync and returns errors or success.

using SharedModels.EntityClasses;
using System.ComponentModel.DataAnnotations;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => Results.Ok(new
{
    Name = "AsyncBasicSample — Minimal API",
    Description = "Async validation using AsyncValidationAttribute + IAsyncValidatableObject without blocking server threads",
    Endpoints = new[]
    {
        new { Method = "POST", Path = "/api/user/valid", Scenario = "1 — Reusable property attribute (valid user)" },
        new { Method = "POST", Path = "/api/user/invalid-email", Scenario = "2 — Invalid email domain" },
        new { Method = "POST", Path = "/api/event/invalid", Scenario = "3 — Entity-level attribute (cross-field)" },
        new { Method = "POST", Path = "/api/order/over-limit", Scenario = "4 — IAsyncValidatableObject (cross-property)" },
        new { Method = "POST", Path = "/api/profile/invalid", Scenario = "5 — IAsyncValidatableObject (property-scoped)" }
    }
}));

static async Task<IResult> ValidateAndRespondAsync<T>(T instance, string scenarioName) where T : notnull
{
    var results = new List<ValidationResult>();
    var context = new ValidationContext(instance);
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
// Reusable async property attribute with I/O simulation.
// The server thread is released while validation awaits the simulated external call.
// POST /api/user/valid  { "name": "Alice", "email": "alice@contoso.com", "delay": 3000 }
// ───────────────────────────────────────────
app.MapPost("/api/user/valid", async (User user) =>
    await ValidateAndRespondAsync(user, "Reusable property attribute (Valid user)"));

// ───────────────────────────────────────────
// Reusable async-only property attribute, parameterized.
// Invalid email domains fail without blocking the request thread during the I/O wait.
// POST /api/user/invalid-email  { "name": "Bob", "email": "bob@gmail.com", "delay": 3000 }
// ───────────────────────────────────────────
app.MapPost("/api/user/invalid-email", async (User user) =>
    await ValidateAndRespondAsync(user, "Async-only property attribute (invalid email domain)"));

// ───────────────────────────────────────────
// [Event] Reusable async entity-level attribute (cross-field).
// Async validation keeps the server responsive while the simulated delay runs.
// POST /api/event/invalid  { "title": "Launch Party", "startDate": "2026-12-25", "endDate": "2026-12-20", "delay": 3000 }
// ───────────────────────────────────────────
app.MapPost("/api/event/invalid", async (Event ev) =>
    await ValidateAndRespondAsync(ev, "Reusable entity-level attribute (cross-field)"));

// ───────────────────────────────────────────
// [Order] Self-validating entity via IAsyncValidatableObject (cross-property).
// Because validation is async, expensive checks do not block the thread pool.
// POST /api/order/over-limit  { "productName": "Widget", "quantity": 10000, "unitPrice": 10, "delay": 3000 }
// ───────────────────────────────────────────
app.MapPost("/api/order/over-limit", async (Order order) =>
    await ValidateAndRespondAsync(order, "IAsyncValidatableObject (cross-property)"));

// ───────────────────────────────────────────
// [Profile] Self-validating entity via IAsyncValidatableObject (property-scoped).
// Multiple async checks can run without tying up a server thread.
// POST /api/profile/invalid  { "username": "admin", "bio": "<201 chars>", "delay": 3000 }
// ───────────────────────────────────────────
app.MapPost("/api/profile/invalid", async (Profile profile) =>
    await ValidateAndRespondAsync(profile, "IAsyncValidatableObject (property-scoped)"));

app.Run();
