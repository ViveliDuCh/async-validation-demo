// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace SourceGenScenariosConsole;

public static class ScenarioValidationLog
{
    private static readonly Lock s_lock = new();
    private static readonly List<string> s_entries = [];

    public static void Clear()
    {
        lock (s_lock)
        {
            s_entries.Clear();
        }
    }

    public static void Log(string entry)
    {
        lock (s_lock)
        {
            s_entries.Add(entry);
        }
    }

    public static IReadOnlyList<string> Snapshot()
    {
        lock (s_lock)
        {
            return [.. s_entries];
        }
    }
}
