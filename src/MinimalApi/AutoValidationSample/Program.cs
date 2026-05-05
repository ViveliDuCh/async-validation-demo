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
        new { Method = "POST", Path = "/api/user/valid", Scenario = "SharedModels User (async-validated)" },
        new { Method = "POST", Path = "/api/event/invalid", Scenario = "SharedModels Event (async-validated)" },
        new { Method = "POST", Path = "/api/order/over-limit", Scenario = "SharedModels Order (async-validated)" },
        new { Method = "POST", Path = "/api/profile/invalid", Scenario = "SharedModels Profile (async-validated)" }
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
    Func<T, object> successFactory) where T : notnull
{
    var results = new List<ValidationResult>();
    bool isValid = await Validator.TryValidateObjectAsync(instance, new ValidationContext(instance), results, validateAllProperties: true);

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

app.MapPost("/api/user/valid", async (User user) =>
    await ValidateSharedModelAsync(user, "SharedModels User (async-validated)", validatedUser => new
    {
        Scenario = "SharedModels User (async-validated)",
        Valid = true,
        validatedUser.Name,
        validatedUser.Email
    }))
    .DisableValidation();

app.MapPost("/api/event/invalid", async (Event ev) =>
    await ValidateSharedModelAsync(ev, "SharedModels Event (async-validated)", validatedEvent => new
    {
        Scenario = "SharedModels Event (async-validated)",
        Valid = true,
        validatedEvent.Title
    }))
    .DisableValidation();

app.MapPost("/api/order/over-limit", async (Order order) =>
    await ValidateSharedModelAsync(order, "SharedModels Order (async-validated)", validatedOrder => new
    {
        Scenario = "SharedModels Order (async-validated)",
        Valid = true,
        validatedOrder.ProductName
    }))
    .DisableValidation();

app.MapPost("/api/profile/invalid", async (Profile profile) =>
    await ValidateSharedModelAsync(profile, "SharedModels Profile (async-validated)", validatedProfile => new
    {
        Scenario = "SharedModels Profile (async-validated)",
        Valid = true,
        validatedProfile.Username
    }))
    .DisableValidation();

app.Run();
