// Minimal API — Async port of AsyncValidationConsoleDemo
// Demonstrates DI integration, two-phase validation, infrastructure failure handling,
// and cancellation propagation using async validation without blocking the request thread.

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
    Description = "DI integration, two-phase validation, and cancellation using async validation",
    Endpoints = new[]
    {
        new { Method = "POST", Path = "/api/register/duplicate", Scenario = "1 — DI + duplicate detection" },
        new { Method = "POST", Path = "/api/register/bad-email", Scenario = "2 — Two-phase validation" },
        new { Method = "POST", Path = "/api/transfer/invalid", Scenario = "3 — IAsyncValidatableObject (MoneyTransfer)" },
        new { Method = "POST", Path = "/api/register/error", Scenario = "4 — Infrastructure failure handling" },
        new { Method = "POST", Path = "/api/register/cancellation", Scenario = "5 — CancellationToken propagation" }
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
// Scenario 1: DI Service Resolution + Multiple Async Attributes.
// Async validators resolve UserService from DI and await uniqueness checks without blocking threads.
// POST /api/register/duplicate  { "username": "admin", "email": "admin@example.com", "password": "SecureP@ss123" }
// ───────────────────────────────────────────
app.MapPost("/api/register/duplicate", async (UserRegistration registration, IServiceProvider sp) =>
    await ValidateAndRespondAsync(registration, sp, "DI Service Resolution + Duplicate Detection"));

// ───────────────────────────────────────────
// Scenario 2: Two-Phase Validation.
// Sync validators run first; if EmailAddress fails, the async UniqueEmail check is skipped.
// The request thread still stays free while any async validators await I/O.
// POST /api/register/bad-email  { "username": "newuser", "email": "not-an-email", "password": "SecureP@ss123" }
// ───────────────────────────────────────────
app.MapPost("/api/register/bad-email", async (UserRegistration registration, IServiceProvider sp) =>
    await ValidateAndRespondAsync(registration, sp, "Two-Phase Validation"));

// ───────────────────────────────────────────
// Scenario 3: IAsyncValidatableObject (cross-property validation).
// MoneyTransfer can perform async balance checks without tying up the server thread.
// POST /api/transfer/invalid  { "fromAccount": "checking", "toAccount": "checking", "amount": 1000.00 }
// ───────────────────────────────────────────
app.MapPost("/api/transfer/invalid", async (MoneyTransfer transfer, IServiceProvider sp) =>
    await ValidateAndRespondAsync(transfer, sp, "IAsyncValidatableObject (MoneyTransfer)"));

// ───────────────────────────────────────────
// Scenario 4: Infrastructure Failure Handling.
// Async validation surfaces service failures while keeping the request pipeline non-blocking.
// POST /api/register/error  { "username": "error-trigger", "email": "new@example.com", "password": "SecureP@ss123" }
// ───────────────────────────────────────────
app.MapPost("/api/register/error", async (UserRegistration registration, IServiceProvider sp) =>
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
// Scenario 5: CancellationToken Propagation.
// Async validation can observe disconnects/timeouts so the server stops waiting instead of blocking.
// POST /api/register/cancellation  { "username": "newuser", "email": "new@example.com", "password": "SecureP@ss123" }
// ───────────────────────────────────────────
app.MapPost("/api/register/cancellation", async (UserRegistration registration, IServiceProvider sp, HttpContext httpContext) =>
{
    using var cts = CancellationTokenSource.CreateLinkedTokenSource(httpContext.RequestAborted);
    cts.CancelAfter(TimeSpan.FromSeconds(2)); // timeout
    try
    {
        return await ValidateAndRespondAsync(registration, sp, "CancellationToken Propagation", cts.Token);
    }
    catch (OperationCanceledException)
    {
        return Results.Problem(detail: "Validation was cancelled (client disconnected or timeout).", title: "Cancelled", statusCode: 499);
    }
});

app.Run();
