// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;

namespace SourceGenScenariosConsole;

[OptionsValidator]
public partial class CacheSettingsValidator
    : IValidateOptions<CacheSettings>, IAsyncValidateOptions<CacheSettings>
{
}
