// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Demonstrates that ConfigureAwait(false) inside Validator.*Async methods
// does NOT prevent callers from resuming on their captured SynchronizationContext.
// This directly addresses the concern raised at:
// https://github.com/dotnet/runtime/pull/128656#discussion_r3329340930

using System.Collections.Concurrent;
using SharedModels.EntityClasses;
using System.ComponentModel.DataAnnotations;

Console.WriteLine("=== ConfigureAwait(false) Safety Demo ===");
Console.WriteLine("  Proves that library-internal ConfigureAwait(false) does not break");
Console.WriteLine("  UI-thread resumption for callers of Validator.*Async methods.\n");
Console.WriteLine("  Background: WinForms/WPF use SynchronizationContext to marshal");
Console.WriteLine("  continuations back to the UI thread. ConfigureAwait(false) inside");
Console.WriteLine("  the Validator only affects the Validator's own continuations —");
Console.WriteLine("  the caller's await still captures and resumes on its original context.\n");

// ─── Test 1: Without SynchronizationContext (server/console behavior) ───
Console.WriteLine("--- Test 1: No SynchronizationContext (server/console default) ---");
int beforeThread = Environment.CurrentManagedThreadId;
Console.WriteLine($"  Thread before await: {beforeThread}");

var user = new User { Name = "Bob", Username = "admin" };
var results = new List<ValidationResult>();
bool isValid = await Validator.TryValidateObjectAsync(
    user, new ValidationContext(user), results, validateAllProperties: true);

int afterThread = Environment.CurrentManagedThreadId;
Console.WriteLine($"  Thread after  await: {afterThread}");
Console.WriteLine($"  Valid: {isValid} | Errors: {results.Count}");
Console.WriteLine($"  ➡️  No SynchronizationContext → continuation ran on thread pool (expected).\n");

// ─── Test 2: With a custom SynchronizationContext (simulates UI thread) ───
Console.WriteLine("--- Test 2: With SingleThreadSynchronizationContext (simulates UI) ---");
Console.WriteLine("  The custom context forces all Post/Send callbacks onto one thread,");
Console.WriteLine("  mimicking WinForms (WindowsFormsSynchronizationContext) or WPF");
Console.WriteLine("  (DispatcherSynchronizationContext).\n");

await RunWithSingleThreadedContextAsync();

// ─── Test 3: Fire-and-forget pattern (WPF INotifyDataErrorInfo) ───
Console.WriteLine("\n--- Test 3: Fire-and-forget with SynchronizationContext ---");
Console.WriteLine("  Simulates the WPF pattern where property setters fire off");
Console.WriteLine("  _ = ValidatePropertyAsync(...) without awaiting.\n");

await RunFireAndForgetTestAsync();

Console.WriteLine("\n=== All tests passed — ConfigureAwait(false) is safe for UI callers. ===");

// ────────────────────────────────────────────────────────────────────────

static async Task RunWithSingleThreadedContextAsync()
{
    var tcs = new TaskCompletionSource();

    var contextThread = new Thread(() =>
    {
        var ctx = new SingleThreadSynchronizationContext();
        SynchronizationContext.SetSynchronizationContext(ctx);

        // Queue the async work onto this context
        ctx.Post(async _ =>
        {
            try
            {
                int uiThread = Environment.CurrentManagedThreadId;
                Console.WriteLine($"  'UI thread' ID: {uiThread}");

                // Scenario 1: Mixed sync + async attributes (User)
                var user = new User { Name = "Bob", Username = "admin" };
                var results = new List<ValidationResult>();

                Console.WriteLine("  Calling Validator.TryValidateObjectAsync...");
                bool isValid = await Validator.TryValidateObjectAsync(
                    user, new ValidationContext(user), results, validateAllProperties: true);

                int resumedThread = Environment.CurrentManagedThreadId;
                Console.WriteLine($"  Resumed on thread: {resumedThread}");
                Console.WriteLine($"  Valid: {isValid} | Errors: {string.Join("; ", results.Select(r => r.ErrorMessage))}");

                bool resumedOnUiThread = (resumedThread == uiThread);
                Console.WriteLine($"  ✅ Resumed on UI thread: {resumedOnUiThread}");
                if (!resumedOnUiThread)
                    Console.WriteLine("  ❌ UNEXPECTED: Continuation did NOT resume on the UI thread!");

                // Scenario 3: IAsyncValidatableObject (MoneyTransfer)
                Console.WriteLine("\n  Testing IAsyncValidatableObject (MoneyTransfer)...");
                var transfer = new MoneyTransfer
                {
                    FromAccount = "ACC-001",
                    ToAccount = "ACC-001", // same account → error
                    Amount = 1000m
                };
                var transferResults = new List<ValidationResult>();
                bool transferValid = await Validator.TryValidateObjectAsync(
                    transfer, new ValidationContext(transfer), transferResults, validateAllProperties: true);

                int transferResumedThread = Environment.CurrentManagedThreadId;
                Console.WriteLine($"  Resumed on thread: {transferResumedThread}");
                Console.WriteLine($"  Valid: {transferValid} | Errors: {string.Join("; ", transferResults.Select(r => r.ErrorMessage))}");

                bool transferOnUi = (transferResumedThread == uiThread);
                Console.WriteLine($"  ✅ Resumed on UI thread: {transferOnUi}");

                tcs.SetResult();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        }, null);

        // Run the message pump
        ctx.RunOnCurrentThread();
    });

    contextThread.IsBackground = true;
    contextThread.Start();

    await tcs.Task;
}

static async Task RunFireAndForgetTestAsync()
{
    var tcs = new TaskCompletionSource();

    var contextThread = new Thread(() =>
    {
        var ctx = new SingleThreadSynchronizationContext();
        SynchronizationContext.SetSynchronizationContext(ctx);

        ctx.Post(async _ =>
        {
            try
            {
                int uiThread = Environment.CurrentManagedThreadId;
                Console.WriteLine($"  'UI thread' ID: {uiThread}");

                // Simulates WPF ViewModel fire-and-forget pattern:
                //   _ = ValidatePropertyAsync(value, propertyName)
                var continuationTcs = new TaskCompletionSource<int>();

                _ = Task.Run(async () =>
                {
                    // This inner code simulates ValidatePropertyAsync inside a ViewModel
                    // running from a fire-and-forget call on the UI thread.
                    var user = new User { Name = "Bob", Username = "admin" };
                    var context = new ValidationContext(user) { MemberName = nameof(User.Username) };
                    var results = new List<ValidationResult>();

                    await Validator.TryValidatePropertyAsync(user.Username, context, results);
                    continuationTcs.SetResult(Environment.CurrentManagedThreadId);
                });

                int continuationThread = await continuationTcs.Task;
                Console.WriteLine($"  Fire-and-forget continuation thread: {continuationThread}");
                Console.WriteLine("  (Without SynchronizationContext on the inner call,");
                Console.WriteLine("   continuation runs on thread pool — this is expected for Task.Run.)");

                // Now test fire-and-forget WITH SynchronizationContext captured
                Console.WriteLine("\n  Fire-and-forget WITH captured SynchronizationContext:");
                var captured = SynchronizationContext.Current!;
                var capturedTcs = new TaskCompletionSource<int>();

                // This simulates a property setter on the UI thread calling
                // _ = ValidatePropertyAsync() where ValidatePropertyAsync
                // starts on the UI thread and captures the SynchronizationContext
                _ = ValidatePropertyOnContextAsync(captured, capturedTcs);

                int capturedThread = await capturedTcs.Task;
                Console.WriteLine($"  Continuation thread: {capturedThread}");
                bool onUi = (capturedThread == uiThread);
                Console.WriteLine($"  ✅ Resumed on UI thread: {onUi}");

                tcs.SetResult();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        }, null);

        ctx.RunOnCurrentThread();
    });

    contextThread.IsBackground = true;
    contextThread.Start();

    await tcs.Task;
}

static async Task ValidatePropertyOnContextAsync(
    SynchronizationContext context, TaskCompletionSource<int> result)
{
    // Ensure we're running on the captured context
    await Task.Yield(); // allow the message pump to pick this up

    var user = new User { Name = "Bob", Username = "admin" };
    var ctx = new ValidationContext(user) { MemberName = nameof(User.Username) };
    var results = new List<ValidationResult>();

    // This await captures the current SynchronizationContext
    await Validator.TryValidatePropertyAsync(user.Username, ctx, results);

    // After await — should still be on the same context thread
    result.SetResult(Environment.CurrentManagedThreadId);
}

/// <summary>
/// Minimal single-threaded SynchronizationContext that runs a message pump,
/// similar to WinForms' WindowsFormsSynchronizationContext.
/// </summary>
sealed class SingleThreadSynchronizationContext : SynchronizationContext
{
    private readonly BlockingCollection<(SendOrPostCallback, object?)> _queue = new();

    public override void Post(SendOrPostCallback d, object? state)
        => _queue.Add((d, state));

    public override void Send(SendOrPostCallback d, object? state)
    {
        if (Thread.CurrentThread.ManagedThreadId == _threadId)
        {
            d(state);
            return;
        }

        var done = new ManualResetEventSlim(false);
        _queue.Add((_ =>
        {
            d(state);
            done.Set();
        }, null));
        done.Wait();
    }

    private int _threadId;

    public void RunOnCurrentThread()
    {
        _threadId = Thread.CurrentThread.ManagedThreadId;
        foreach (var (callback, state) in _queue.GetConsumingEnumerable())
        {
            callback(state);
        }
    }

    public void Complete() => _queue.CompleteAdding();
}
