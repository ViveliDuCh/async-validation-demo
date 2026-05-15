namespace Options.Shared;

/// <summary>
/// Singleton service that captures timestamped validation events.
/// The async validator writes entries here; UI components read them
/// to prove which pipeline (sync vs. async) actually ran.
/// </summary>
public sealed class ValidationLogService
{
    private readonly List<ValidationLogEntry> _entries = [];
    private readonly Lock _lock = new();

    /// <summary>Fires after a new entry is added.</summary>
    public event Action? OnEntryAdded;

    public void Log(string source, string message)
    {
        lock (_lock)
        {
            _entries.Add(new ValidationLogEntry(DateTime.Now, source, message));
        }
        OnEntryAdded?.Invoke();
    }

    public IReadOnlyList<ValidationLogEntry> GetEntries()
    {
        lock (_lock)
        {
            return [.. _entries];
        }
    }
}

public sealed record ValidationLogEntry(DateTime Timestamp, string Source, string Message);
