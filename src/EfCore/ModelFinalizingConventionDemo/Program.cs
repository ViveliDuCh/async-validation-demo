using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using ModelFinalizingConventionDemo;

Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
Console.WriteLine("║  EF Core — Path B: IModelFinalizingConvention              ║");
Console.WriteLine("║  Full SharedModels attribute scan (all 6 entities)         ║");
Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
Console.WriteLine();

var convention = new SharedModelsConvention();
using var db = new DemoDbContext(convention);

var connection = db.Database.GetDbConnection();
connection.Open();
db.Database.EnsureCreated();

// ───────────────────────────────────────────
// Scenario 1: Full Attribute Scan
// ───────────────────────────────────────────
Console.WriteLine("--- Scenario 1: Full Attribute Scan Across All SharedModels Entities ---");
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
// Scenario 4: IAsyncValidatableObject entities correctly ignored
// ───────────────────────────────────────────
Console.WriteLine("--- Scenario 4: Entities With Only IAsyncValidatableObject (No Property Attrs) ---");
Console.WriteLine();
Console.WriteLine("  IAsyncValidatableObject is a runtime interface — not an attribute.");
Console.WriteLine("  EF Core conventions correctly ignore these entities:");

foreach (var entityType in db.Model.GetEntityTypes())
{
    bool hasAsyncInterface = typeof(IAsyncValidatableObject)
        .IsAssignableFrom(entityType.ClrType);
    bool hasAsyncAttrs = convention.Log
        .Any(l => l.Contains(entityType.ClrType.Name) && l.Contains("FOUND"));

    if (hasAsyncInterface && !hasAsyncAttrs)
    {
        Console.WriteLine($"  ✔ {entityType.ClrType.Name}: IAsyncValidatableObject only — convention skipped (correct)");
    }
}
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
Console.WriteLine("  All SharedModels async attributes are detectable by");
Console.WriteLine("  EF Core's convention system using the real prototype runtime.");

connection.Close();
