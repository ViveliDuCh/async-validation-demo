// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;

namespace SyncFallbackConsole;

/// <summary>
/// The [OptionsValidator] source generator fills in the Validate() and
/// ValidateAsync() method bodies at compile time.
/// Validate() calls IsValid() on each attribute — including async-only ones,
/// which throw InvalidOperationException. ValidateAsync() calls IsValidAsync() instead.
/// </summary>
[OptionsValidator]
public partial class CloudInfoOptionsValidator
    : IValidateOptions<Options.Shared.CloudInfoOptions>,
      IAsyncValidateOptions<Options.Shared.CloudInfoOptions>
{
}
