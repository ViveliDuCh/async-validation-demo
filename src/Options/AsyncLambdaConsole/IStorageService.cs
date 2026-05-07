// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace AsyncLambdaConsole;

/// <summary>
/// Async storage service abstraction for validating storage endpoints.
/// In real apps this would perform actual HTTP probes, DNS lookups, etc.
/// </summary>
public interface IStorageService
{
    Task<bool> ExistsAsync(string endpoint, CancellationToken cancellationToken = default);
}
