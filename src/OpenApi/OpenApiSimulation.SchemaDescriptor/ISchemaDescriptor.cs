// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/// <summary>
/// Optional interface for validation attributes that wish to contribute
/// metadata to OpenAPI/JSON Schema generators.
///
/// In a real ASP.NET Core app, the IOpenApiSchemaTransformer would look for
/// attributes implementing this interface and apply their metadata to the schema.
/// </summary>
public interface ISchemaDescriptor
{
    string? SchemaDescription { get; }
    string? SchemaFormat { get; }
    string? SchemaPattern { get; }
    int? SchemaMaxLength { get; }
    int? SchemaMinLength { get; }
    IReadOnlyDictionary<string, object>? SchemaExtensions { get; }
}
