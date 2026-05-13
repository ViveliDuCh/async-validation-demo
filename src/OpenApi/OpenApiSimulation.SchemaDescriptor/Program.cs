// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

Console.WriteLine("=== OpenAPI Schema Simulation — ISchemaDescriptor Approach ===");
Console.WriteLine("(Descriptor-enabled attributes self-describe property and class-level schema contributions)\n");

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
        SchemaDescriptorTransformer.TransformProperty,
        SchemaDescriptorTransformer.TransformClass);
    SchemaPrinter.PrintSchema(enhanced, "UserRegistrationWithDescriptor (ISchemaDescriptor — property + class attrs visible)");
}

Console.WriteLine("Note: SharedModels.User is intentionally not shown here because");
Console.WriteLine("[UsernameAvailableAsync] does not implement ISchemaDescriptor. This sample only");
Console.WriteLine("lights up attributes that opt into schema metadata directly.\n");

Console.WriteLine("=== Simulation Complete ===");
Console.WriteLine();
Console.WriteLine("Key takeaway: Descriptor-enabled copies of the current UserRegistration");
Console.WriteLine("attributes can self-describe both property and entity-level OpenAPI metadata");
Console.WriteLine("through one shared ISchemaDescriptor transformer.");
