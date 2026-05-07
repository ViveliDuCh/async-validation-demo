// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using SharedModels.EntityClasses;

Console.WriteLine("=== OpenAPI Schema Simulation — Pre-Attribute Approach ===");
Console.WriteLine("(No attribute classes were modified — pure reflection)\n");

// ───────────────────────────────────────────
// Scenario 1: UserRegistration — has [UniqueUsername] and [UniqueEmail]
// Same entity used by AsyncValidationConsoleDemo
// ───────────────────────────────────────────
Console.WriteLine("Scenario 1: UserRegistration (same entity as AsyncValidationConsoleDemo)");
{
    var schemaWithout = SchemaBuilder.BuildSchema<UserRegistration>();
    SchemaPrinter.PrintSchema(schemaWithout, "WITHOUT transformer (async attrs invisible)");

    var schemaWith = SchemaBuilder.BuildSchema<UserRegistration>(
        AsyncAttributePreTransformer.TransformProperty);
    SchemaPrinter.PrintSchema(schemaWith, "WITH pre-attribute transformer (async attrs visible)");
}

// ───────────────────────────────────────────
// Scenario 2: User — has [IsValidName] and [AsyncOnlyEmailDomain("contoso.com")]
// ───────────────────────────────────────────
Console.WriteLine("Scenario 2: User (multiple async-only attributes)");
{
    var schemaWithout = SchemaBuilder.BuildSchema<User>();
    SchemaPrinter.PrintSchema(schemaWithout, "WITHOUT transformer");

    var schemaWith = SchemaBuilder.BuildSchema<User>(
        AsyncAttributePreTransformer.TransformProperty);
    SchemaPrinter.PrintSchema(schemaWith, "WITH pre-attribute transformer");
}

// ───────────────────────────────────────────
// Scenario 3: Event — class-level [AsyncDateRangeValid]
// Shows that class-level async validation attributes are also discoverable
// ───────────────────────────────────────────
Console.WriteLine("Scenario 3: Event (class-level async attribute)");
{
    var schemaWithout = SchemaBuilder.BuildSchema<Event>();
    SchemaPrinter.PrintSchema(schemaWithout, "WITHOUT transformer");

    var schemaWith = SchemaBuilder.BuildSchema<Event>(
        AsyncAttributePreTransformer.TransformProperty,
        AsyncAttributePreTransformer.TransformClass);
    SchemaPrinter.PrintSchema(schemaWith, "WITH pre-attribute transformer (class-level)");
}

// ───────────────────────────────────────────
// Scenario 4: MoneyTransfer — IAsyncValidatableObject (class-level, no per-property attrs)
// The transformer correctly produces NO property-level extensions
// ───────────────────────────────────────────
Console.WriteLine("Scenario 4: MoneyTransfer (IAsyncValidatableObject — no per-property async attrs)");
{
    var schema = SchemaBuilder.BuildSchema<MoneyTransfer>(
        AsyncAttributePreTransformer.TransformProperty);
    SchemaPrinter.PrintSchema(schema,
        "MoneyTransfer — IAsyncValidatableObject is class-level, not per-property");
}

Console.WriteLine("=== Simulation Complete ===");
Console.WriteLine();
Console.WriteLine("Key takeaway: The same async validation attributes used by");
Console.WriteLine("AsyncValidationConsoleDemo can produce OpenAPI schema metadata");
Console.WriteLine("via an IOpenApiSchemaTransformer — without modifying any attribute class.");
