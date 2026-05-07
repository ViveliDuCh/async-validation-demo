// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

Console.WriteLine("=== OpenAPI Schema Simulation — ISchemaDescriptor Approach ===");
Console.WriteLine("(Attributes implement ISchemaDescriptor to self-describe schema contributions)\n");

// ───────────────────────────────────────────
// Compare original UserRegistration (invisible) vs descriptor-enabled (visible)
// ───────────────────────────────────────────
Console.WriteLine("Comparison: Original SharedModels vs ISchemaDescriptor-enabled copies\n");
{
    // Original — async attrs invisible (same as what OpenAPI produces today)
    var original = SchemaBuilder.BuildSchema<SharedModels.EntityClasses.UserRegistration>();
    SchemaPrinter.PrintSchema(original, "Original UserRegistration (no transformer — async attrs invisible)");

    // ISchemaDescriptor-enabled — attrs self-describe
    var enhanced = SchemaBuilder.BuildSchema<UserRegistrationWithDescriptor>(
        SchemaDescriptorTransformer.Transform);
    SchemaPrinter.PrintSchema(enhanced, "UserRegistrationWithDescriptor (ISchemaDescriptor — attrs visible)");
}

Console.WriteLine("=== Simulation Complete ===");
Console.WriteLine();
Console.WriteLine("Key takeaway: Async validation attributes can implement ISchemaDescriptor");
Console.WriteLine("to self-describe their OpenAPI schema contributions. A single transformer");
Console.WriteLine("class handles ALL attributes that implement the interface.");
