// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace AsyncLambdaConsole;

/// <summary>
/// Simulated storage service that checks endpoints against a known set.
/// Simulates async I/O with a small delay.
/// </summary>
public class FakeStorageService : IStorageService
{
    private static readonly HashSet<string> s_knownEndpoints = new(StringComparer.OrdinalIgnoreCase)
    {
        "https://mystorage.blob.core.windows.net",
        "https://backup.blob.core.windows.net",
        "https://prod-storage.blob.core.windows.net"
    };

    public async Task<bool> ExistsAsync(string endpoint, CancellationToken cancellationToken = default)
    {
        // Simulate an async network probe
        await Task.Delay(50, cancellationToken);
        return s_knownEndpoints.Contains(endpoint);
    }
}
