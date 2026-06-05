// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;

namespace StartupValidatorConsole;

/// <summary>
/// Custom sync startup validator that replaces the built-in one.
/// Used in Scenarios 3, 3b, 4, 6, 8, and Edge Case B.
/// </summary>
public class MyCustomStartupValidator : IStartupValidator
{
    public void Validate()
    {
        Console.WriteLine("    >>> MyCustomStartupValidator.Validate() called!");

        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DEMO_API_KEY")))
        {
            throw new InvalidOperationException("DEMO_API_KEY environment variable is required!");
        }
    }
}

/// <summary>
/// Custom async startup validator that replaces the built-in one.
/// Used in Scenarios 5 and 6.
/// </summary>
public class MyCustomAsyncStartupValidator : IAsyncStartupValidator
{
    public async Task ValidateAsync(CancellationToken cancellationToken = default)
    {
        Console.WriteLine("    >>> MyCustomAsyncStartupValidator.ValidateAsync() called!");

        await Task.Delay(10, cancellationToken);

        string? healthUrl = Environment.GetEnvironmentVariable("DEMO_HEALTH_URL");
        if (string.IsNullOrEmpty(healthUrl))
        {
            throw new InvalidOperationException("DEMO_HEALTH_URL environment variable is required!");
        }
    }
}
