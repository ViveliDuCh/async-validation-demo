// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Reflection;

/// <summary>
/// Transformer that makes ALL AsyncValidationAttribute subclasses visible
/// in schema output — without modifying any attribute class.
///
/// This mirrors what an IOpenApiSchemaTransformer would do in ASP.NET Core,
/// but runs as a standalone delegate for console testing.
/// </summary>
public static class AsyncAttributePreTransformer
{
    /// <summary>
    /// Inspects a property for AsyncValidationAttribute subclasses and adds
    /// schema extensions. Works with the attributes as they are — no interface required.
    /// </summary>
    public static void TransformProperty(SimulatedPropertySchema schema, PropertyInfo property)
    {
        var asyncAttrs = property
            .GetCustomAttributes(inherit: true)
            .Where(a => a.GetType().IsSubclassOf(typeof(AsyncValidationAttribute)))
            .ToList();

        if (asyncAttrs.Count == 0) return;

        schema.Extensions["x-async-validation"] = true;
        schema.Extensions["x-requires-server-check"] = true;

        var validationTypes = asyncAttrs
            .Select(a => ToKebabCase(a.GetType().Name.Replace("Attribute", "")))
            .ToList();

        if (validationTypes.Count == 1)
            schema.Extensions["x-async-validation-type"] = validationTypes[0];
        else
            schema.Extensions["x-async-validation-types"] = validationTypes;

        // Include error messages if set
        foreach (var attr in asyncAttrs.OfType<ValidationAttribute>())
        {
            if (!string.IsNullOrEmpty(attr.ErrorMessage))
            {
                schema.Description ??= "";
                if (schema.Description.Length > 0) schema.Description += "; ";
                schema.Description += attr.ErrorMessage;
            }
        }
    }

    /// <summary>
    /// Inspects a class for class-level AsyncValidationAttribute subclasses
    /// (e.g., [AsyncRegistrationScreen], [AsyncScheduleCheck], [AsyncInventoryCheck]).
    /// </summary>
    public static void TransformClass(SimulatedEntitySchema schema, Type entityType)
    {
        var asyncAttrs = entityType
            .GetCustomAttributes(inherit: true)
            .Where(a => a.GetType().IsSubclassOf(typeof(AsyncValidationAttribute)))
            .ToList();

        if (asyncAttrs.Count == 0) return;

        schema.Extensions["x-async-validation"] = true;

        var validationTypes = asyncAttrs
            .Select(a => ToKebabCase(a.GetType().Name.Replace("Attribute", "")))
            .ToList();

        if (validationTypes.Count == 1)
            schema.Extensions["x-async-class-validation-type"] = validationTypes[0];
        else
            schema.Extensions["x-async-class-validation-types"] = validationTypes;
    }

    private static string ToKebabCase(string name)
    {
        var result = new System.Text.StringBuilder();
        for (int i = 0; i < name.Length; i++)
        {
            if (i > 0 && char.IsUpper(name[i]))
                result.Append('-');
            result.Append(char.ToLowerInvariant(name[i]));
        }
        return result.ToString();
    }
}
