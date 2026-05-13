// Minimal API — Async port of BasicAsyncSample
// Demonstrates the 4 async validation proposal scenarios without blocking request threads.

using SharedModels.EntityClasses;
using System.ComponentModel.DataAnnotations;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => Results.Ok(new
{
    Name = "AsyncBasicSample — Minimal API",
    Description = "Minimal API endpoints aligned to the 4 async validation proposal scenarios",
    Endpoints = new[]
    {
        new { Method = "POST", Path = "/api/scenario1/user", Scenario = "1 — User (mixed sync + async property attributes)" },
        new { Method = "POST", Path = "/api/scenario2/order", Scenario = "2 — Order (IValidatableObject + async attributes)" },
        new { Method = "POST", Path = "/api/scenario3/transfer", Scenario = "3 — MoneyTransfer (IAsyncValidatableObject)" },
        new { Method = "POST", Path = "/api/scenario4/event", Scenario = "4 — Event (IValidatableObject + async entity attribute)" }
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
// Scenario 1: User uses mixed sync + async property validation.
// POST /api/scenario1/user  { "name": "Bob", "username": "admin" }
// ───────────────────────────────────────────
app.MapPost("/api/scenario1/user", async (User user) =>
    await ValidateAndRespondAsync(user, "Scenario 1 — User (mixed sync + async property attributes)"));

// ───────────────────────────────────────────
// Scenario 2: Order uses IValidatableObject plus async product/inventory checks.
// POST /api/scenario2/order  { "productName": "Unknown", "quantity": 1000, "unitPrice": 120, "delay": 100 }
// ───────────────────────────────────────────
app.MapPost("/api/scenario2/order", async (Order order) =>
    await ValidateAndRespondAsync(order, "Scenario 2 — Order (IValidatableObject + async attributes)"));

// ───────────────────────────────────────────
// Scenario 3: MoneyTransfer uses IAsyncValidatableObject for async cross-property validation.
// POST /api/scenario3/transfer  { "fromAccount": "checking", "toAccount": "checking", "amount": 1000 }
// ───────────────────────────────────────────
app.MapPost("/api/scenario3/transfer", async (MoneyTransfer transfer) =>
    await ValidateAndRespondAsync(transfer, "Scenario 3 — MoneyTransfer (IAsyncValidatableObject)"));

// ───────────────────────────────────────────
// Scenario 4: Event uses IValidatableObject plus an async entity-level attribute.
// POST /api/scenario4/event  { "title": "TBD Kickoff", "startDate": "2099-01-01", "endDate": "2099-01-02" }
// ───────────────────────────────────────────
app.MapPost("/api/scenario4/event", async (Event ev) =>
    await ValidateAndRespondAsync(ev, "Scenario 4 — Event (IValidatableObject + async entity attribute)"));

app.Run();
