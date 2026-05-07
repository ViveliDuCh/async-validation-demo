using Microsoft.EntityFrameworkCore;
using PropertyAttributeConventionDemo;

Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
Console.WriteLine("║  EF Core — Path A: PropertyAttributeConventionBase<T>          ║");
Console.WriteLine("║  Typed convention for [UniqueUsername] (self-contained)         ║");
Console.WriteLine("╚════════════════════════════════════════════════════════════════╝");
Console.WriteLine();

var convention = new UniqueUsernameConvention(null!);
using var db = new DemoDbContext(convention);

var connection = db.Database.GetDbConnection();
connection.Open();
db.Database.EnsureCreated();

// ───────────────────────────────────────────
// Scenario 1: Convention Detection
// ───────────────────────────────────────────
Console.WriteLine("--- Scenario 1: Convention Detection ---");
Console.WriteLine("  Question: Did EF Core detect [UniqueUsername] via PropertyAttributeConventionBase<T>?");
Console.WriteLine();

if (convention.Log.Count > 0)
{
    Console.WriteLine("  ✅ YES — Convention detected async attribute:");
    foreach (var entry in convention.Log)
        Console.WriteLine($"  {entry}");
}
else
{
    Console.WriteLine("  ❌ NO — Convention did not fire");
}
Console.WriteLine();

// ───────────────────────────────────────────
// Scenario 2: Schema Impact — UNIQUE INDEX
// ───────────────────────────────────────────
Console.WriteLine("--- Scenario 2: Schema Impact — UNIQUE INDEX ---");
Console.WriteLine();

var entityType = db.Model.FindEntityType(typeof(UserRegistration))!;
var usernameIndexes = entityType.GetIndexes()
    .Where(i => i.Properties.Any(p => p.Name == "Username"))
    .ToList();

if (usernameIndexes.Any(i => i.IsUnique))
{
    Console.WriteLine("  ✅ UNIQUE INDEX exists on Username");
    foreach (var idx in usernameIndexes)
    {
        var props = string.Join(", ", idx.Properties.Select(p => p.Name));
        Console.WriteLine($"     Index: [{props}] IsUnique={idx.IsUnique}");
    }
}
else
{
    Console.WriteLine("  ❌ No unique index on Username");
}
Console.WriteLine();

// ───────────────────────────────────────────
// Scenario 3: Annotation Storage
// ───────────────────────────────────────────
Console.WriteLine("--- Scenario 3: Annotation Storage ---");
Console.WriteLine();

var usernameProp = entityType.FindProperty("Username")!;
var asyncAnnotations = usernameProp.GetAnnotations()
    .Where(a => a.Name.StartsWith("AsyncValidation:"))
    .ToList();

if (asyncAnnotations.Any())
{
    Console.WriteLine("  ✅ Custom annotations stored:");
    foreach (var ann in asyncAnnotations)
        Console.WriteLine($"     {ann.Name} = \"{ann.Value}\"");
}
else
{
    Console.WriteLine("  ❌ No async validation annotations found");
}
Console.WriteLine();

// ───────────────────────────────────────────
// Scenario 4: Built-in Attributes Still Work
// ───────────────────────────────────────────
Console.WriteLine("--- Scenario 4: Built-in Attributes Still Work ---");
Console.WriteLine();

var usernameMeta = entityType.FindProperty("Username")!;
var emailMeta = entityType.FindProperty("Email")!;
var passwordMeta = entityType.FindProperty("Password")!;

Console.WriteLine($"  Username: IsNullable={usernameMeta.IsNullable}, MaxLength={usernameMeta.GetMaxLength()}");
Console.WriteLine($"  Email:    IsNullable={emailMeta.IsNullable}, MaxLength={emailMeta.GetMaxLength()}");
Console.WriteLine($"  Password: IsNullable={passwordMeta.IsNullable}, MaxLength={passwordMeta.GetMaxLength()}");

bool allCorrect = !usernameMeta.IsNullable && usernameMeta.GetMaxLength() == 50
               && !emailMeta.IsNullable && emailMeta.GetMaxLength() == 100
               && !passwordMeta.IsNullable && passwordMeta.GetMaxLength() == 100;

Console.WriteLine($"  {(allCorrect ? "✅" : "❌")} Built-in conventions: {(allCorrect ? "all correct" : "MISMATCH")}");
Console.WriteLine();

// ───────────────────────────────────────────
// Scenario 5: Generated SQL
// ───────────────────────────────────────────
Console.WriteLine("--- Scenario 5: Generated SQL ---");
Console.WriteLine();

using var db2 = new DemoDbContext(new UniqueUsernameConvention(null!));
var sql = db2.Database.GenerateCreateScript();
Console.WriteLine(sql);

bool hasUniqueIndex = sql.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase)
                   && sql.Contains("Username", StringComparison.OrdinalIgnoreCase);
Console.WriteLine($"  {(hasUniqueIndex ? "✅" : "❌")} SQL contains UNIQUE constraint on Username: {hasUniqueIndex}");
Console.WriteLine();

// ───────────────────────────────────────────
// Summary
// ───────────────────────────────────────────
Console.WriteLine("═══════════════════════════════════════════════════════════");
Console.WriteLine("  SUMMARY — Path A: PropertyAttributeConventionBase<T>");
Console.WriteLine("═══════════════════════════════════════════════════════════");
Console.WriteLine($"  Convention detected async attribute:   {(convention.Log.Count > 0 ? "✅ PASS" : "❌ FAIL")}");
Console.WriteLine($"  UNIQUE INDEX created from metadata:   {(usernameIndexes.Any(i => i.IsUnique) ? "✅ PASS" : "❌ FAIL")}");
Console.WriteLine($"  Custom annotations stored:            {(asyncAnnotations.Any() ? "✅ PASS" : "❌ FAIL")}");
Console.WriteLine($"  Built-in conventions unaffected:      {(allCorrect ? "✅ PASS" : "❌ FAIL")}");
Console.WriteLine($"  SQL reflects async metadata:          {(hasUniqueIndex ? "✅ PASS" : "❌ FAIL")}");
Console.WriteLine();
Console.WriteLine("  Conclusion: PropertyAttributeConventionBase<T> successfully");
Console.WriteLine("  detects AsyncValidationAttribute subclasses for schema gen.");

connection.Close();
