// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace CrossTypeParallelConsole;

/// <summary>
/// Cache endpoint options — validated asynchronously at startup.
/// The [AsyncCacheReachable] attribute simulates a 200ms connectivity check.
/// </summary>
public class CacheSettings
{
    [Required(ErrorMessage = "Cache endpoint is required.")]
    [AsyncCacheReachable]
    public string Endpoint { get; set; } = "";

    [Range(1, 3600, ErrorMessage = "TTL must be between 1 and 3600 seconds.")]
    public int DefaultTtlSeconds { get; set; } = 300;
}
