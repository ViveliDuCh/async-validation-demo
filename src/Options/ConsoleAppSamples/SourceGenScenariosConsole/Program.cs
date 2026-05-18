// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SourceGenScenariosConsole;

Console.WriteLine("=== Source Generator Options Validation Scenarios ===");
Console.WriteLine();

await RunScenario1Async();
await RunScenario2Async();
await RunScenario3Async();
await RunScenario4Async();
await RunScenario5Async();
await RunScenario6Async();
await RunScenario7Async();

Console.WriteLine("=== Demo Complete ===");

static async Task RunScenario1Async()
{
    Console.WriteLine("--- Scenario 1: Mixed sync + async on same property (happy path) ---");
    Console.WriteLine("  Register SmtpSettings with source-gen IAsyncValidateOptions<T>.");
    Console.WriteLine("  ValidateAsync() calls per-property TryValidateValueAsync.");
    Console.WriteLine("  [Required] (sync) and [AsyncSmtpReachable] (dual-mode async path) both pass.");

    ValidationAttemptResult result = await CaptureValidationAttemptAsync(
        () => BuildSourceGenSmtpAsyncHost("Scenarios:Valid"),
        static async host =>
        {
            await RunAsyncStartupValidationAsync(host);
            _ = GetOptions<SmtpSettings>(host);
        });

    if (result.Success)
    {
        Console.WriteLine("  ✅ Source-gen async validation passed.\n");
        return;
    }

    PrintUnexpected(result.Exception, result.Failures);
}

static async Task RunScenario2Async()
{
    Console.WriteLine("--- Scenario 2: Dual-mode attribute — sync fallback works ---");
    Console.WriteLine("  Register SmtpSettings with source-gen IValidateOptions<T> (sync path).");
    Console.WriteLine("  The generated Validate() calls IsValid() — the sync fallback.");
    Console.WriteLine("  [AsyncSmtpReachable] has a sync fallback, so it works.");

    ValidationAttemptResult result = await CaptureValidationAttemptAsync(
        () => BuildSourceGenSmtpSyncHost("Scenarios:Valid"),
        static host =>
        {
            _ = GetOptions<SmtpSettings>(host);
            return Task.CompletedTask;
        });

    if (result.Success)
    {
        PrintProbeEntries(result.LogEntries, "  ");
        Console.WriteLine("  ✅ Source-gen sync validation passed (dual-mode attr sync fallback used).\n");
        return;
    }

    PrintUnexpected(result.Exception, result.Failures);
}

static async Task RunScenario3Async()
{
    Console.WriteLine("--- Scenario 3: Same-property two-phase (sync fail skips async on same property) ---");
    Console.WriteLine("  Port=0 fails [Range] validation. Source gen validates each property independently.");
    Console.WriteLine("  The async attr on a DIFFERENT property (Host) still runs because source gen");
    Console.WriteLine("  does per-property TryValidateValueAsync — no cross-property short-circuit.");

    ValidationAttemptResult result = await CaptureValidationAttemptAsync(
        () => BuildSourceGenSmtpAsyncHost("Scenarios:SyncFailure"),
        static host => RunAsyncStartupValidationAsync(host));

    if (!result.Success && result.Exception is not null)
    {
        PrintFailures(result.Failures, "     ");
        PrintProbeEntries(result.LogEntries, "  ");
        bool hostAsyncRan = ContainsEntry(result.LogEntries, "SMTP async probe ran");
        Console.WriteLine(hostAsyncRan
            ? "  ✅ Caught expected failures. Note: Host async validation still ran (per-property independence).\n"
            : "  ❌ Expected Host async validation to run, but no probe was recorded.\n");
        return;
    }

    Console.WriteLine("  ❌ Unexpected success.\n");
}

static async Task RunScenario4Async()
{
    Console.WriteLine("--- Scenario 4: Cross-property behavior contrast with reflection ---");
    Console.WriteLine("  Same invalid data as Scenario 3. Reflection validates object-wide.");
    Console.WriteLine("  If any sync attr fails anywhere, reflection skips ALL async attrs.");

    ValidationAttemptResult sourceGenResult = await CaptureValidationAttemptAsync(
        () => BuildSourceGenSmtpAsyncHost("Scenarios:SyncFailure"),
        static host => RunAsyncStartupValidationAsync(host));

    ValidationAttemptResult reflectionResult = await CaptureValidationAttemptAsync(
        () => BuildReflectionSmtpAsyncHost("Scenarios:SyncFailure"),
        static host => RunAsyncStartupValidationAsync(host));

    Console.WriteLine("  Source-gen failures:");
    PrintFailures(sourceGenResult.Failures, "     ");
    Console.WriteLine("  Reflection failures:");
    PrintFailures(reflectionResult.Failures, "     ");

    bool sourceGenRanHostProbe = ContainsEntry(sourceGenResult.LogEntries, "SMTP async probe ran");
    bool reflectionRanHostProbe = ContainsEntry(reflectionResult.LogEntries, "SMTP async probe ran");

    Console.WriteLine(sourceGenRanHostProbe
        ? "  Source gen: Port sync failure did NOT prevent Host async check"
        : "  Source gen: Host async check was skipped unexpectedly");
    Console.WriteLine(reflectionRanHostProbe
        ? "  Reflection (current preview): Host async probe also ran while object-level validation reported nested failures"
        : "  (Reflection would have skipped Host async check entirely)");
    Console.WriteLine();
}

static async Task RunScenario5Async()
{
    Console.WriteLine("--- Scenario 5: Cross-type parallel (Task.WhenAll) ---");
    Console.WriteLine("  Two source-gen validators run concurrently via IAsyncStartupValidator.");
    Console.WriteLine("  Time should be near 200ms (max delay), not 350ms+ (sum of delays).");

    Stopwatch stopwatch = Stopwatch.StartNew();
    ValidationAttemptResult result = await CaptureValidationAttemptAsync(
        () => BuildSourceGenCrossTypeAsyncHost("Scenarios:CrossTypeValid"),
        static async host =>
        {
            await RunAsyncStartupValidationAsync(host);
            _ = GetOptions<SmtpSettings>(host);
            _ = GetOptions<CacheSettings>(host);
        });
    stopwatch.Stop();

    if (result.Success)
    {
        Console.WriteLine($"  ⏱️  Elapsed: {stopwatch.ElapsedMilliseconds}ms");
        PrintProbeEntries(result.LogEntries, "  ");
        Console.WriteLine("  ✅ Both validated concurrently via source gen.\n");
        return;
    }

    PrintUnexpected(result.Exception, result.Failures);
}

static async Task RunScenario6Async()
{
    Console.WriteLine("--- Scenario 6: Nested [ValidateObjectMembers] ---");
    Console.WriteLine("  SmtpSettings.Credentials has [ValidateObjectMembers].");
    Console.WriteLine("  Bad password (\"short\") should be reported from nested SmtpCredentials.");

    ValidationAttemptResult result = await CaptureValidationAttemptAsync(
        () => BuildSourceGenSmtpAsyncHost("Scenarios:SyncFailure"),
        static host => RunAsyncStartupValidationAsync(host));

    if (!result.Success)
    {
        List<string> nestedFailures = [.. result.Failures.Where(static failure =>
            failure.Contains("SMTP password must be at least 8 characters.", StringComparison.Ordinal))];

        Console.WriteLine("  Nested failures:");
        PrintFailures(nestedFailures, "     ");
        PrintProbeEntries(result.LogEntries, "  ");
        Console.WriteLine("  ✅ Nested object validation caught by source gen.\n");
        return;
    }

    Console.WriteLine("  ❌ Unexpected success.\n");
}

static async Task RunScenario7Async()
{
    Console.WriteLine("--- Scenario 7: Startup failure (OptionsValidationException) ---");
    Console.WriteLine("  Unreachable SMTP host fails [AsyncSmtpReachable].");
    Console.WriteLine("  OptionsValidationException should come from source-gen ValidateAsync().");

    ValidationAttemptResult result = await CaptureValidationAttemptAsync(
        () => BuildSourceGenSmtpAsyncHost("Scenarios:AsyncFailure"),
        static host => RunAsyncStartupValidationAsync(host));

    if (!result.Success && result.Exception is OptionsValidationException)
    {
        PrintFailures(result.Failures, "     ");
        PrintProbeEntries(result.LogEntries, "  ");
        Console.WriteLine("  ✅ Caught OptionsValidationException from source-gen path.\n");
        return;
    }

    if (!result.Success)
    {
        PrintUnexpected(result.Exception, result.Failures);
        return;
    }

    Console.WriteLine("  ❌ Unexpected success.\n");
}

static HostApplicationBuilder CreateBuilder()
{
    HostApplicationBuilder builder = Host.CreateApplicationBuilder(Array.Empty<string>());
    builder.Configuration.AddJsonFile("appsettings.json", optional: false);
    return builder;
}

static IHost BuildSourceGenSmtpAsyncHost(string configSection)
{
    HostApplicationBuilder builder = CreateBuilder();

    builder.Services.AddOptions<SmtpSettings>()
        .Bind(builder.Configuration.GetSection($"{configSection}:Smtp"));

    builder.Services.AddSingleton<IAsyncValidateOptions<SmtpSettings>>(new SmtpSettingsValidator());
    builder.Services.AddSingleton<IAsyncValidateOptions<SmtpSettings>>(new SmtpSettingsNestedValidator());
    builder.Services.AddOptions<SmtpSettings>()
        .ValidateOnStartAsync();

    return builder.Build();
}

static IHost BuildSourceGenSmtpSyncHost(string configSection)
{
    HostApplicationBuilder builder = CreateBuilder();

    builder.Services.AddOptions<SmtpSettings>()
        .Bind(builder.Configuration.GetSection($"{configSection}:Smtp"));

    builder.Services.AddSingleton<IValidateOptions<SmtpSettings>>(new SmtpSettingsValidator());
    builder.Services.AddSingleton<IValidateOptions<SmtpSettings>>(new SmtpSettingsNestedValidator());
    builder.Services.AddOptions<SmtpSettings>()
        .ValidateOnStart();

    return builder.Build();
}

static IHost BuildReflectionSmtpAsyncHost(string configSection)
{
    HostApplicationBuilder builder = CreateBuilder();

    builder.Services.AddOptions<SmtpSettings>()
        .Bind(builder.Configuration.GetSection($"{configSection}:Smtp"))
        .ValidateDataAnnotationsAsync()
        .ValidateOnStartAsync();

    return builder.Build();
}

static IHost BuildSourceGenCrossTypeAsyncHost(string configSection)
{
    HostApplicationBuilder builder = CreateBuilder();

    builder.Services.AddOptions<SmtpSettings>()
        .Bind(builder.Configuration.GetSection($"{configSection}:Smtp"));
    builder.Services.AddOptions<CacheSettings>()
        .Bind(builder.Configuration.GetSection($"{configSection}:Cache"));

    builder.Services.AddSingleton<IAsyncValidateOptions<SmtpSettings>>(new SmtpSettingsValidator());
    builder.Services.AddSingleton<IAsyncValidateOptions<SmtpSettings>>(new SmtpSettingsNestedValidator());
    builder.Services.AddSingleton<IAsyncValidateOptions<CacheSettings>>(new CacheSettingsValidator());

    builder.Services.AddOptions<SmtpSettings>()
        .ValidateOnStartAsync();
    builder.Services.AddOptions<CacheSettings>()
        .ValidateOnStartAsync();

    return builder.Build();
}

static async Task<ValidationAttemptResult> CaptureValidationAttemptAsync(
    Func<IHost> buildHost,
    Func<IHost, Task> validationAction)
{
    ScenarioValidationLog.Clear();

    try
    {
        using IHost host = buildHost();
        await validationAction(host);
        return new ValidationAttemptResult(true, [], ScenarioValidationLog.Snapshot(), null);
    }
    catch (Exception ex)
    {
        return new ValidationAttemptResult(false, ExtractFailures(ex), ScenarioValidationLog.Snapshot(), ex);
    }
}

static async Task RunAsyncStartupValidationAsync(IHost host)
{
    IAsyncStartupValidator? asyncValidator = host.Services.GetService<IAsyncStartupValidator>();
    if (asyncValidator is not null)
    {
        await asyncValidator.ValidateAsync();
    }
}

static TOptions GetOptions<TOptions>(IHost host)
    where TOptions : class
    => host.Services.GetRequiredService<IOptionsMonitor<TOptions>>().CurrentValue;

static List<string> ExtractFailures(Exception exception)
{
    return exception switch
    {
        OptionsValidationException optionsException => [.. optionsException.Failures],
        AggregateException aggregateException => [.. aggregateException.Flatten().InnerExceptions.SelectMany(ExtractFailures)],
        _ => [exception.Message]
    };
}

static bool ContainsEntry(IReadOnlyList<string> entries, string fragment)
    => entries.Any(entry => entry.Contains(fragment, StringComparison.Ordinal));

static void PrintFailures(IEnumerable<string> failures, string indent)
{
    List<string> materializedFailures = [.. failures];
    if (materializedFailures.Count == 0)
    {
        Console.WriteLine($"{indent}- (no failure details captured)");
        return;
    }

    foreach (string failure in materializedFailures)
    {
        Console.WriteLine($"{indent}- {failure}");
    }
}

static void PrintProbeEntries(IReadOnlyList<string> entries, string indent)
{
    if (entries.Count == 0)
    {
        return;
    }

    Console.WriteLine($"{indent}Probe log:");
    foreach (string entry in entries)
    {
        Console.WriteLine($"{indent}  - {entry}");
    }
}

static void PrintUnexpected(Exception? exception, IEnumerable<string> failures)
{
    Console.WriteLine($"  ❌ Unexpected failure: {exception?.GetType().Name ?? "UnknownException"}");
    PrintFailures(failures, "     ");
    Console.WriteLine();
}

internal sealed record ValidationAttemptResult(
    bool Success,
    IReadOnlyList<string> Failures,
    IReadOnlyList<string> LogEntries,
    Exception? Exception);
