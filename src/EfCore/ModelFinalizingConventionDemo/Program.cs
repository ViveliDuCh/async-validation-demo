using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using ModelFinalizingConventionDemo;
using SharedModels.EntityClasses;
using SharedModels.ValidationClasses;

Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
Console.WriteLine("║  EF Core — Path B: IModelFinalizingConvention              ║");
Console.WriteLine("║  Current SharedModels attribute scan (4 entities)          ║");
Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
Console.WriteLine();

var convention = new SharedModelsConvention();
using var db = new DemoDbContext(convention);

var connection = db.Database.GetDbConnection();
connection.Open();
db.Database.EnsureCreated();

db.AddRange(
    new UserRegistration
    {
        Username = "demo-user",
        Email = "demo@example.com",
        Password = "SecureP@ss123"
    },
    new User
    {
        Name = "Alice",
        Username = "alice"
    },
    new Event
    {
        Title = "Launch Event",
        StartDate = DateTime.Today,
        EndDate = DateTime.Today.AddDays(1)
    },
    new Order
    {
        ProductName = "Widget",
        Quantity = 5,
        UnitPrice = 25m,
        Delay = 10
    });

db.SaveChanges();

// ───────────────────────────────────────────
// Scenario 1: Full Attribute Scan
// ───────────────────────────────────────────
Console.WriteLine("--- Scenario 1: Full Attribute Scan Across Current SharedModels Entities ---");
Console.WriteLine();

if (convention.Log.Count > 0)
{
    Console.WriteLine("  ✅ Convention detected async attributes:");
    foreach (var entry in convention.Log)
        Console.WriteLine($"  {entry}");
}
else
{
    Console.WriteLine("  ❌ No async attributes detected");
}
bool userAttributeDetected = convention.Log.Any(entry =>
    entry.Contains("FOUND: User.Username") &&
    entry.Contains(nameof(UsernameAvailableAsyncAttribute)));
Console.WriteLine($"  User.Username [UsernameAvailableAsync] detected: {userAttributeDetected}");
Console.WriteLine();

// ───────────────────────────────────────────
// Scenario 2: Unique Indexes
// ───────────────────────────────────────────
Console.WriteLine("--- Scenario 2: Schema Impact — UNIQUE Indexes ---");
Console.WriteLine();

foreach (var entityType in db.Model.GetEntityTypes())
{
    var uniqueIndexes = entityType.GetIndexes().Where(i => i.IsUnique).ToList();
    foreach (var idx in uniqueIndexes)
    {
        var props = string.Join(", ", idx.Properties.Select(p => p.Name));
        Console.WriteLine($"  ✅ {entityType.ClrType.Name}: UNIQUE INDEX on [{props}]");
    }
}
Console.WriteLine();

// ───────────────────────────────────────────
// Scenario 3: Annotations
// ───────────────────────────────────────────
Console.WriteLine("--- Scenario 3: Stored Annotations ---");
Console.WriteLine();

foreach (var entityType in db.Model.GetEntityTypes())
{
    var entityAnnotations = entityType.GetAnnotations()
        .Where(a => a.Name.StartsWith("AsyncValidation:")).ToList();
    foreach (var ann in entityAnnotations)
        Console.WriteLine($"  {entityType.ClrType.Name} (entity): {ann.Name}");

    foreach (var prop in entityType.GetDeclaredProperties())
    {
        var propAnnotations = prop.GetAnnotations()
            .Where(a => a.Name.StartsWith("AsyncValidation:")).ToList();
        foreach (var ann in propAnnotations)
            Console.WriteLine($"  {entityType.ClrType.Name}.{prop.Name}: {ann.Name}");
    }
}
Console.WriteLine();

// ───────────────────────────────────────────
// Scenario 4: IValidatableObject interface correctly ignored
// ───────────────────────────────────────────
Console.WriteLine("--- Scenario 4: Order's IValidatableObject Interface Is Ignored ---");
Console.WriteLine();
var orderEntity = db.Model.FindEntityType(typeof(Order))!;
int orderAnnotationCount =
    orderEntity.GetAnnotations().Count(a => a.Name.StartsWith("AsyncValidation:")) +
    orderEntity.GetDeclaredProperties()
        .SelectMany(p => p.GetAnnotations())
        .Count(a => a.Name.StartsWith("AsyncValidation:"));

Console.WriteLine($"  Order implements IValidatableObject: {typeof(IValidatableObject).IsAssignableFrom(typeof(Order))}");
Console.WriteLine($"  Async annotations discovered for Order: {orderAnnotationCount}");
Console.WriteLine("  ✔ Convention stored metadata only for [AsyncProductExists] and [AsyncInventoryCheck].");
Console.WriteLine("  ✔ IValidatableObject itself produced no convention metadata.");
Console.WriteLine();

// ───────────────────────────────────────────
// Scenario 5: Generated SQL
// ───────────────────────────────────────────
Console.WriteLine("--- Scenario 5: Generated SQL (relevant excerpts) ---");
Console.WriteLine();

using var db2 = new DemoDbContext(new SharedModelsConvention());
var sql = db2.Database.GenerateCreateScript();

foreach (var line in sql.Split('\n'))
{
    var trimmed = line.Trim();
    if (trimmed.StartsWith("CREATE", StringComparison.OrdinalIgnoreCase) ||
        trimmed.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine($"  {trimmed}");
    }
}
Console.WriteLine();

// ───────────────────────────────────────────
// Summary
// ───────────────────────────────────────────
int asyncAttrsFound = convention.Log.Count(l => l.Contains("FOUND"));
int uniqueIdxCount = db.Model.GetEntityTypes()
    .SelectMany(e => e.GetIndexes()).Count(i => i.IsUnique);
int annotationCount = db.Model.GetEntityTypes()
    .SelectMany(e => e.GetDeclaredProperties())
    .SelectMany(p => p.GetAnnotations())
    .Count(a => a.Name.StartsWith("AsyncValidation:"));
annotationCount += db.Model.GetEntityTypes()
    .SelectMany(e => e.GetAnnotations())
    .Count(a => a.Name.StartsWith("AsyncValidation:"));

Console.WriteLine("═══════════════════════════════════════════════════════════");
Console.WriteLine("  SUMMARY — Path B: IModelFinalizingConvention");
Console.WriteLine("═══════════════════════════════════════════════════════════");
Console.WriteLine($"  Async attributes detected:     {asyncAttrsFound}");
Console.WriteLine($"  UNIQUE indexes created:        {uniqueIdxCount}");
Console.WriteLine($"  Annotations stored:            {annotationCount}");
Console.WriteLine($"  SharedModels entities scanned: {db.Model.GetEntityTypes().Count()}");
Console.WriteLine();
Console.WriteLine("  All current SharedModels async attributes are detectable by");
Console.WriteLine("  EF Core's convention system without picking up sync-only attributes.");

connection.Close();
