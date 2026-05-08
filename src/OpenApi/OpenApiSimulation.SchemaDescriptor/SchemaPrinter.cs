// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

/// <summary>
/// Prints simulated schemas as JSON Schema output.
/// </summary>
public static class SchemaPrinter
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public static void PrintSchema(SimulatedEntitySchema schema, string label)
    {
        Console.WriteLine($"--- {label} ---");

        var jsonSchema = new Dictionary<string, object>
        {
            ["type"] = "object",
            ["title"] = schema.EntityName
        };

        if (schema.Description is not null)
        {
            jsonSchema["description"] = schema.Description;
        }

        var required = new List<string>();
        var properties = new Dictionary<string, object>();

        foreach (var prop in schema.Properties)
        {
            var propObj = new Dictionary<string, object> { ["type"] = prop.Type };

            if (prop.MinLength.HasValue) propObj["minLength"] = prop.MinLength.Value;
            if (prop.MaxLength.HasValue) propObj["maxLength"] = prop.MaxLength.Value;
            if (prop.Pattern is not null) propObj["pattern"] = prop.Pattern;
            if (prop.Format is not null) propObj["format"] = prop.Format;
            if (prop.Description is not null) propObj["description"] = prop.Description;

            foreach (var (key, value) in prop.Extensions)
            {
                propObj[key] = value;
            }

            properties[ToCamelCase(prop.PropertyName)] = propObj;

            if (prop.Required) required.Add(ToCamelCase(prop.PropertyName));
        }

        jsonSchema["required"] = required;
        jsonSchema["properties"] = properties;

        foreach (var (key, value) in schema.Extensions)
        {
            jsonSchema[key] = value;
        }

        Console.WriteLine(JsonSerializer.Serialize(jsonSchema, s_jsonOptions));
        Console.WriteLine();
    }

    private static string ToCamelCase(string name) =>
        string.IsNullOrEmpty(name) ? name : char.ToLowerInvariant(name[0]) + name[1..];
}
