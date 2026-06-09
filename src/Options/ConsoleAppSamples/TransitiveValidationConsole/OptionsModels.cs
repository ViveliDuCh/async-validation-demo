// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace TransitiveValidationConsole;

// ═══════════════════════════════════════════════════════════════════
// Options models demonstrating transitive async validation:
//
//   TenantSettings (top-level)
//     ├── [ValidateObjectMembers] Database  → DatabaseSettings (nested)
//     ├── [ValidateObjectMembers] Failover  → FailoverSettings (nested)
//     │       └── [ValidateObjectMembers] Primary → DatabaseSettings (shared)
//     └── [ValidateEnumeratedItems] Endpoints → List<EndpointEntry> (collection)
//
//   CircularParent ←→ CircularChild (circular reference, cycle detection)
// ═══════════════════════════════════════════════════════════════════

/// <summary>
/// Top-level options with nested objects and a validated collection.
/// </summary>
public class TenantSettings
{
    [Required(ErrorMessage = "Tenant name is required.")]
    public string TenantName { get; set; } = "";

    /// <summary>Nested object — async-validated via recursive walk.</summary>
    [ValidateObjectMembers]
    public DatabaseSettings Database { get; set; } = new();

    /// <summary>
    /// Another nested object that itself contains a [ValidateObjectMembers]
    /// reference, demonstrating multi-level recursion.
    /// </summary>
    [ValidateObjectMembers]
    public FailoverSettings Failover { get; set; } = new();

    /// <summary>
    /// Collection of endpoints — each item is individually async-validated
    /// via the recursive walk.
    /// </summary>
    [ValidateEnumeratedItems]
    public List<EndpointEntry> Endpoints { get; set; } = [];
}

/// <summary>
/// Nested options with an async attribute on a property.
/// </summary>
public class DatabaseSettings
{
    [Required(ErrorMessage = "Connection string is required.")]
    [AsyncConnectionStringValid]
    public string ConnectionString { get; set; } = "";

    [Range(1, 300, ErrorMessage = "Timeout must be between 1 and 300 seconds.")]
    public int TimeoutSeconds { get; set; } = 30;
}

/// <summary>
/// Second-level nesting: contains its own [ValidateObjectMembers] reference,
/// creating a multi-level recursive validation chain.
/// </summary>
public class FailoverSettings
{
    public bool Enabled { get; set; }

    /// <summary>
    /// The primary DB for failover — re-uses DatabaseSettings,
    /// so the recursive walk goes TenantSettings → FailoverSettings → DatabaseSettings.
    /// </summary>
    [ValidateObjectMembers]
    public DatabaseSettings Primary { get; set; } = new();
}

/// <summary>
/// Item type for [ValidateEnumeratedItems]. Each entry in the collection
/// is individually validated by the recursive walk.
/// </summary>
public class EndpointEntry
{
    [Required(ErrorMessage = "Endpoint name is required.")]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "Endpoint URL is required.")]
    [AsyncEndpointHealthy]
    public string Url { get; set; } = "";
}

// ═══════════════════════════════════════════════════════════════════
// Circular reference models — tests cycle detection
// ═══════════════════════════════════════════════════════════════════

/// <summary>
/// Parent that references a child, which references back to the parent.
/// The recursive walk must detect this cycle and not infinite-loop.
/// </summary>
public class CircularParent
{
    [Required(ErrorMessage = "Parent name is required.")]
    public string Name { get; set; } = "";

    [ValidateObjectMembers]
    public CircularChild? Child { get; set; }
}

/// <summary>
/// Child that references back to the parent, creating a circular dependency.
/// </summary>
public class CircularChild
{
    [Required(ErrorMessage = "Child name is required.")]
    public string Name { get; set; } = "";

    /// <summary>Back-reference to parent — creates a cycle.</summary>
    [ValidateObjectMembers]
    public CircularParent? BackRef { get; set; }
}
