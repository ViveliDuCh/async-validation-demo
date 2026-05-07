// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

/// <summary>
/// Generic transformer that finds ALL ISchemaDescriptor attributes on a property
/// and applies their self-described metadata to the schema.
///
/// This is the console simulation equivalent of an IOpenApiSchemaTransformer
/// that reads the ISchemaDescriptor interface.
/// </summary>
public static class SchemaDescriptorTransformer
{
    public static void Transform(SimulatedPropertySchema schema, PropertyInfo property)
    {
        var descriptors = property
            .GetCustomAttributes(inherit: true)
            .OfType<ISchemaDescriptor>();

        foreach (var desc in descriptors)
        {
            if (desc.SchemaDescription is { } description)
                schema.Description = description;

            if (desc.SchemaFormat is { } format)
                schema.Format = format;

            if (desc.SchemaPattern is { } pattern)
                schema.Pattern = pattern;

            if (desc.SchemaMaxLength is { } maxLen)
                schema.MaxLength = maxLen;

            if (desc.SchemaMinLength is { } minLen)
                schema.MinLength = minLen;

            if (desc.SchemaExtensions is { } extensions)
            {
                foreach (var (key, value) in extensions)
                {
                    schema.Extensions[key] = value;
                }
            }
        }
    }
}
