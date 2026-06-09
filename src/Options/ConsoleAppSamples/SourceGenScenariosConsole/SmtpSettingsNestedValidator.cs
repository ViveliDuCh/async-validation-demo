// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;

namespace SourceGenScenariosConsole;

public sealed class SmtpSettingsNestedValidator
    : IValidateOptions<SmtpSettings>, IAsyncValidateOptions<SmtpSettings>
{
    private readonly SmtpCredentialsValidator _credentialsValidator = new();

    public ValidateOptionsResult Validate(string? name, SmtpSettings options)
    {
        if (options.Credentials is null)
        {
            return ValidateOptionsResult.Success;
        }

        return PrefixFailures(_credentialsValidator.Validate(name, options.Credentials));
    }

    public async ValueTask<ValidateOptionsResult> ValidateAsync(
        string? name,
        SmtpSettings options,
        CancellationToken cancellationToken)
    {
        if (options.Credentials is null)
        {
            return ValidateOptionsResult.Success;
        }

        ValidateOptionsResult nestedResult = await _credentialsValidator.ValidateAsync(name, options.Credentials, cancellationToken);
        return PrefixFailures(nestedResult);
    }

    private static ValidateOptionsResult PrefixFailures(ValidateOptionsResult result)
    {
        if (!result.Failed)
        {
            return ValidateOptionsResult.Success;
        }

        return ValidateOptionsResult.Fail(result.Failures?.Select(static failure => $"Credentials.{failure}") ?? []);
    }
}
