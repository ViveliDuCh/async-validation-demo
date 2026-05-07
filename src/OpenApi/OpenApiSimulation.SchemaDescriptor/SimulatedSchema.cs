// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/// <summary>
/// Lightweight property schema — stands in for OpenApiSchema in the console simulation.
/// </summary>
public class SimulatedPropertySchema
{
    public string PropertyName { get; set; } = "";
    public string Type { get; set; } = "string";
    public bool Required { get; set; }
    public int? MinLength { get; set; }
    public int? MaxLength { get; set; }
    public string? Pattern { get; set; }
    public string? Format { get; set; }
    public string? Description { get; set; }
    public Dictionary<string, object> Extensions { get; set; } = new();
}

/// <summary>
/// Lightweight entity schema wrapping its property schemas.
/// </summary>
public class SimulatedEntitySchema
{
    public string EntityName { get; set; } = "";
    public List<SimulatedPropertySchema> Properties { get; set; } = new();
}
