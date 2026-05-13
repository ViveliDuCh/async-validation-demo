// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using SharedModels.EntityClasses;

Console.WriteLine("=== OpenAPI Schema Simulation — Pre-Attribute Approach ===");
Console.WriteLine("(No attribute classes were modified — pure reflection)\n");

// ───────────────────────────────────────────
// Scenario 1: User — [UsernameAvailableAsync]
// [IsValidName] remains invisible because it is sync-only
// ───────────────────────────────────────────
Console.WriteLine("Scenario 1: User (UsernameAvailableAsync visible, IsValidName ignored)");
{
    var schemaWithout = SchemaBuilder.BuildSchema<User>();
    SchemaPrinter.PrintSchema(schemaWithout, "WITHOUT transformer (async attrs invisible)");

    var schemaWith = SchemaBuilder.BuildSchema<User>(
        AsyncAttributePreTransformer.TransformProperty,
        AsyncAttributePreTransformer.TransformClass);
    SchemaPrinter.PrintSchema(schemaWith, "WITH pre-attribute transformer (UsernameAvailableAsync visible)");
}

// ───────────────────────────────────────────
// Scenario 2: UserRegistration — async property + class-level attributes
// [PasswordPolicy] remains invisible because it is sync-only
// ───────────────────────────────────────────
Console.WriteLine("Scenario 2: UserRegistration (async attrs visible, sync-only PasswordPolicy ignored)");
{
    var schemaWithout = SchemaBuilder.BuildSchema<UserRegistration>();
    SchemaPrinter.PrintSchema(schemaWithout, "WITHOUT transformer");

    var schemaWith = SchemaBuilder.BuildSchema<UserRegistration>(
        AsyncAttributePreTransformer.TransformProperty,
        AsyncAttributePreTransformer.TransformClass);
    SchemaPrinter.PrintSchema(schemaWith, "WITH pre-attribute transformer (property + class async attrs visible)");
}

// ───────────────────────────────────────────
// Scenario 3: Event — [ReservedTitleCheck] + [AsyncScheduleCheck]
// [DateRange] remains invisible because it is sync-only
// ───────────────────────────────────────────
Console.WriteLine("Scenario 3: Event (ReservedTitleCheck + AsyncScheduleCheck)");
{
    var schemaWithout = SchemaBuilder.BuildSchema<Event>();
    SchemaPrinter.PrintSchema(schemaWithout, "WITHOUT transformer");

    var schemaWith = SchemaBuilder.BuildSchema<Event>(
        AsyncAttributePreTransformer.TransformProperty,
        AsyncAttributePreTransformer.TransformClass);
    SchemaPrinter.PrintSchema(schemaWith, "WITH pre-attribute transformer");
}

// ───────────────────────────────────────────
// Scenario 4: Order — [AsyncProductExists] + [AsyncInventoryCheck]
// [MaxOrderValue] and IValidatableObject remain invisible here
// ───────────────────────────────────────────
Console.WriteLine("Scenario 4: Order (async attrs visible, IValidatableObject ignored)");
{
    var schemaWithout = SchemaBuilder.BuildSchema<Order>();
    SchemaPrinter.PrintSchema(schemaWithout, "WITHOUT transformer");

    var schemaWith = SchemaBuilder.BuildSchema<Order>(
        AsyncAttributePreTransformer.TransformProperty,
        AsyncAttributePreTransformer.TransformClass);
    SchemaPrinter.PrintSchema(schemaWith, "WITH pre-attribute transformer");
}

Console.WriteLine("=== Simulation Complete ===");
Console.WriteLine();
Console.WriteLine("Key takeaway: Current SharedModels async attributes on");
Console.WriteLine("User, UserRegistration, Event, and Order can drive OpenAPI schema metadata");
Console.WriteLine("without surfacing sync-only attributes or validation interfaces.");
