// Minimal API — Hybrid validation sample using AddValidation() plus manual async validation.
// Local models use automatic validation for convenience, while SharedModels endpoints opt out
// and call Validator.TryValidateObjectAsync so simulated I/O does not block server threads.
// This demonstrates that automatic validation ≠ async validation.

using AutoValidationSample.Models;
using SharedModels.EntityClasses;
using System.ComponentModel.DataAnnotations;

var builder = WebApplication.CreateBuilder(args);

// Keep automatic validation for local models that do not need async I/O.
// It is convenient, but it does not make blocking validation attributes asynchronous.
builder.Services.AddValidation();

var app = builder.Build();

app.MapGet("/", () => Results.Ok(new
{
    Name = "AutoValidationSample — Minimal API (.NET 11 AddValidation + async hybrid)",
    Description = "Local models use automatic validation; SharedModels endpoints disable it and use async validation manually",
    Endpoints = new[]
    {
        new { Method = "GET", Path = "/customers/{id}", Scenario = "Route parameter validation" },
        new { Method = "POST", Path = "/customers", Scenario = "[ValidatableType] + nested object validation" },
        new { Method = "POST", Path = "/orders", Scenario = "IValidatableObject custom validation" },
        new { Method = "POST", Path = "/products", Scenario = "DisableValidation() — bypasses validation" },
        new { Method = "POST", Path = "/contact", Scenario = "Localization-ready error message keys" },
        new { Method = "POST", Path = "/api/scenario1/user", Scenario = "SharedModels Scenario 1 — User (manual async validation)" },
        new { Method = "POST", Path = "/api/scenario2/order", Scenario = "SharedModels Scenario 2 — Order (manual async validation)" },
        new { Method = "POST", Path = "/api/scenario3/transfer", Scenario = "SharedModels Scenario 3 — MoneyTransfer (manual async validation)" },
        new { Method = "POST", Path = "/api/scenario4/event", Scenario = "SharedModels Scenario 4 — Event (manual async validation)" }
    }
}));

// ───────────────────────────────────────────
// Route parameter validation
// [Range] on the route parameter is validated automatically.
// GET /customers/0 → 400 (id must be >= 1)
// GET /customers/42 → 200
// ───────────────────────────────────────────
app.MapGet("/customers/{id}", ([Range(1, int.MaxValue)] int id) =>
    $"Getting customer with ID: {id}");

// ───────────────────────────────────────────
// [ValidatableType] with nested object validation
// The source generator recursively validates Customer + Address.
// POST /customers { invalid nested address } → 400
// ───────────────────────────────────────────
app.MapPost("/customers", (Customer customer) =>
    TypedResults.Created($"/customers/{customer.Name}", customer));

// ───────────────────────────────────────────
// IValidatableObject with custom Validate() logic
// Order.Validate() checks Quantity > 0.
// POST /orders { quantity: 0 } → 400
// ───────────────────────────────────────────
app.MapPost("/orders", (DemoOrder order) =>
    TypedResults.Created($"/orders/{order.OrderId}", order));

// ───────────────────────────────────────────
// DisableValidation() — bypasses the validation endpoint filter
// Even invalid data (odd productId) is accepted.
// ───────────────────────────────────────────
app.MapPost("/products",
    ([EvenNumber(ErrorMessage = "Product ID must be even")] int productId, [Required] string name) =>
        TypedResults.Ok(new { productId, name }))
    .DisableValidation();

// ───────────────────────────────────────────
// ContactFormModel with localization-ready ErrorMessage keys
// Without AddValidationLocalization(), keys appear as raw strings.
// POST /contact { invalid } → 400 with ErrorMessage keys
// ───────────────────────────────────────────
app.MapPost("/contact", (ContactFormModel model) =>
    TypedResults.Ok(new { message = "Contact form submitted.", model.Email }));

static async Task<IResult> ValidateSharedModelAsync<T>(
    T instance,
    string scenarioName,
    Func<T, object> successFactory,
    IServiceProvider? serviceProvider = null) where T : notnull
{
    var results = new List<ValidationResult>();
    bool isValid = await Validator.TryValidateObjectAsync(
        instance,
        new ValidationContext(instance, serviceProvider, null),
        results,
        validateAllProperties: true);

    if (!isValid)
    {
        var errors = results
            .SelectMany(r => r.MemberNames.DefaultIfEmpty(string.Empty),
                (r, member) => new { Member = member, r.ErrorMessage })
            .GroupBy(e => e.Member)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage ?? "Validation failed.").ToArray());

        return Results.ValidationProblem(errors, title: scenarioName);
    }

    return TypedResults.Ok(successFactory(instance));
}

// ═══════════════════════════════════════════
// SharedModels endpoints — automatic validation DISABLED, manual ASYNC validation used.
// AddValidation() is still great for local models, but SharedModels uses async validators.
// Disabling the automatic filter here proves that automatic validation is not the same thing
// as async validation: we opt out, await Validator.TryValidateObjectAsync, and avoid blocking.
// ═══════════════════════════════════════════

app.MapPost("/api/scenario1/user", async (User user) =>
    await ValidateSharedModelAsync(user, "SharedModels Scenario 1 — User (manual async validation)", validatedUser => new
    {
        Scenario = "SharedModels Scenario 1 — User (manual async validation)",
        Valid = true,
        validatedUser.Name,
        validatedUser.Username
    }))
    .DisableValidation();

app.MapPost("/api/scenario2/order", async (Order order) =>
    await ValidateSharedModelAsync(order, "SharedModels Scenario 2 — Order (manual async validation)", validatedOrder => new
    {
        Scenario = "SharedModels Scenario 2 — Order (manual async validation)",
        Valid = true,
        validatedOrder.ProductName,
        validatedOrder.Quantity,
        validatedOrder.UnitPrice
    }))
    .DisableValidation();

app.MapPost("/api/scenario3/transfer", async (MoneyTransfer transfer) =>
    await ValidateSharedModelAsync(transfer, "SharedModels Scenario 3 — MoneyTransfer (manual async validation)", validatedTransfer => new
    {
        Scenario = "SharedModels Scenario 3 — MoneyTransfer (manual async validation)",
        Valid = true,
        validatedTransfer.FromAccount,
        validatedTransfer.ToAccount,
        validatedTransfer.Amount
    }))
    .DisableValidation();

app.MapPost("/api/scenario4/event", async (Event ev) =>
    await ValidateSharedModelAsync(ev, "SharedModels Scenario 4 — Event (manual async validation)", validatedEvent => new
    {
        Scenario = "SharedModels Scenario 4 — Event (manual async validation)",
        Valid = true,
        validatedEvent.Title,
        validatedEvent.StartDate,
        validatedEvent.EndDate
    }))
    .DisableValidation();

app.Run();
