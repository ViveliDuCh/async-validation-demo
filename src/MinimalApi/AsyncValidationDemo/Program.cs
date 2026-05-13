// Minimal API — Async port of AsyncValidationConsoleDemo
// Demonstrates the 4 proposal scenarios plus infrastructure error handling and cancellation.

using SharedModels.EntityClasses;
using SharedModels.ServiceClasses;
using System.ComponentModel.DataAnnotations;

var builder = WebApplication.CreateBuilder(args);

// Register UserService in the DI container (real app pattern).
// Async validators can resolve services and await I/O instead of blocking the server thread.
builder.Services.AddSingleton<UserService>();

var app = builder.Build();

app.MapGet("/", () => Results.Ok(new
{
    Name = "AsyncValidationDemo — Minimal API",
    Description = "4 async validation proposal scenarios plus error handling and cancellation",
    Endpoints = new[]
    {
        new { Method = "POST", Path = "/api/scenario1/user", Scenario = "1 — User (mixed sync + async property attributes)" },
        new { Method = "POST", Path = "/api/scenario2/order", Scenario = "2 — Order (IValidatableObject + async attributes)" },
        new { Method = "POST", Path = "/api/scenario3/transfer", Scenario = "3 — MoneyTransfer (IAsyncValidatableObject)" },
        new { Method = "POST", Path = "/api/scenario4/event", Scenario = "4 — Event (IValidatableObject + async entity attribute)" },
        new { Method = "POST", Path = "/api/error", Scenario = "Error handling with DI-backed UserRegistration validation" },
        new { Method = "POST", Path = "/api/cancellation", Scenario = "CancellationToken propagation with MoneyTransfer" }
    }
}));

static async Task<IResult> ValidateAndRespondAsync<T>(
    T instance,
    IServiceProvider serviceProvider,
    string scenarioName,
    CancellationToken cancellationToken = default) where T : notnull
{
    var results = new List<ValidationResult>();
    var context = new ValidationContext(instance, serviceProvider, null);
    bool isValid = await Validator.TryValidateObjectAsync(instance, context, results, validateAllProperties: true, cancellationToken);

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
app.MapPost("/api/scenario1/user", async (User user, IServiceProvider sp) =>
    await ValidateAndRespondAsync(user, sp, "Scenario 1 — User (mixed sync + async property attributes)"));

// ───────────────────────────────────────────
// Scenario 2: Order uses IValidatableObject plus async product/inventory checks.
// POST /api/scenario2/order  { "productName": "Unknown", "quantity": 1000, "unitPrice": 120, "delay": 100 }
// ───────────────────────────────────────────
app.MapPost("/api/scenario2/order", async (Order order, IServiceProvider sp) =>
    await ValidateAndRespondAsync(order, sp, "Scenario 2 — Order (IValidatableObject + async attributes)"));

// ───────────────────────────────────────────
// Scenario 3: MoneyTransfer uses IAsyncValidatableObject for async cross-property validation.
// POST /api/scenario3/transfer  { "fromAccount": "checking", "toAccount": "checking", "amount": 1000 }
// ───────────────────────────────────────────
app.MapPost("/api/scenario3/transfer", async (MoneyTransfer transfer, IServiceProvider sp) =>
    await ValidateAndRespondAsync(transfer, sp, "Scenario 3 — MoneyTransfer (IAsyncValidatableObject)"));

// ───────────────────────────────────────────
// Scenario 4: Event uses IValidatableObject plus an async entity-level attribute.
// POST /api/scenario4/event  { "title": "TBD Kickoff", "startDate": "2099-01-01", "endDate": "2099-01-02" }
// ───────────────────────────────────────────
app.MapPost("/api/scenario4/event", async (Event ev, IServiceProvider sp) =>
    await ValidateAndRespondAsync(ev, sp, "Scenario 4 — Event (IValidatableObject + async entity attribute)"));

// ───────────────────────────────────────────
// Error handling: DI-backed UserRegistration validation can surface infrastructure failures.
// POST /api/error  { "username": "error-trigger", "email": "new@example.com", "password": "SecureP@ss123" }
// ───────────────────────────────────────────
app.MapPost("/api/error", async (UserRegistration registration, IServiceProvider sp) =>
{
    try
    {
        return await ValidateAndRespondAsync(registration, sp, "Infrastructure Failure Handling");
    }
    catch (InvalidOperationException ex)
    {
        return Results.Problem(
            detail: ex.Message,
            title: "Infrastructure Failure",
            statusCode: 500);
    }
});

// ───────────────────────────────────────────
// CancellationToken propagation with async MoneyTransfer validation.
// POST /api/cancellation  { "fromAccount": "savings", "toAccount": "checking", "amount": 100 }
// ───────────────────────────────────────────
app.MapPost("/api/cancellation", async (MoneyTransfer transfer, IServiceProvider sp, HttpContext httpContext) =>
{
    using var cts = CancellationTokenSource.CreateLinkedTokenSource(httpContext.RequestAborted);
    cts.CancelAfter(TimeSpan.FromMilliseconds(10));
    try
    {
        return await ValidateAndRespondAsync(transfer, sp, "CancellationToken Propagation", cts.Token);
    }
    catch (OperationCanceledException)
    {
        return Results.Problem(detail: "Validation was cancelled (client disconnected or timeout).", title: "Cancelled", statusCode: 499);
    }
});

app.Run();
