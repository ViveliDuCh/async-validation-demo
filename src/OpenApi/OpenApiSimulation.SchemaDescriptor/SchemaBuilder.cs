// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Reflection;

/// <summary>
/// Simulates the OpenAPI schema generation pipeline:
/// 1. Reads standard attributes (mimics ApplyValidationAttributes if/else if chain)
/// 2. Runs the custom transformer (mimics IOpenApiSchemaTransformer)
/// </summary>
public static class SchemaBuilder
{
    public static SimulatedEntitySchema BuildSchema<TEntity>(
        Action<SimulatedPropertySchema, PropertyInfo>? customTransformer = null)
    {
        var entityType = typeof(TEntity);
        var schema = new SimulatedEntitySchema { EntityName = entityType.Name };

        foreach (var prop in entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var propSchema = new SimulatedPropertySchema
            {
                PropertyName = prop.Name,
                Type = MapClrTypeToJsonSchemaType(prop.PropertyType)
            };

            // Phase 1: Simulate the built-in if/else if chain
            foreach (var attr in prop.GetCustomAttributes(inherit: true))
            {
                switch (attr)
                {
                    case RequiredAttribute:
                        propSchema.Required = true;
                        break;
                    case StringLengthAttribute sl:
                        propSchema.MinLength = sl.MinimumLength;
                        propSchema.MaxLength = sl.MaximumLength;
                        break;
                    case MaxLengthAttribute ml:
                        propSchema.MaxLength = ml.Length;
                        break;
                    case MinLengthAttribute mnl:
                        propSchema.MinLength = mnl.Length;
                        break;
                    case RangeAttribute range:
                        propSchema.Extensions["minimum"] = range.Minimum;
                        propSchema.Extensions["maximum"] = range.Maximum;
                        break;
                    case RegularExpressionAttribute regex:
                        propSchema.Pattern = regex.Pattern;
                        break;
                }
            }

            // Phase 2: Run custom transformer
            customTransformer?.Invoke(propSchema, prop);

            schema.Properties.Add(propSchema);
        }

        return schema;
    }

    private static string MapClrTypeToJsonSchemaType(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;
        return underlying switch
        {
            _ when underlying == typeof(string) => "string",
            _ when underlying == typeof(int) || underlying == typeof(long) => "integer",
            _ when underlying == typeof(decimal) || underlying == typeof(double) || underlying == typeof(float) => "number",
            _ when underlying == typeof(bool) => "boolean",
            _ when underlying == typeof(DateTime) => "string",
            _ => "object"
        };
    }
}
