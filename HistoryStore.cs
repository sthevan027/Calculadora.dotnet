using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Calculadora;

public static class HistoryStore
{
    private static readonly string SettingsDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Calculadora");

    private static readonly string HistoryPath = Path.Combine(SettingsDir, "history.json");

    public static List<HistoryEntry> Load()
    {
        try
        {
            if (!File.Exists(HistoryPath)) return new List<HistoryEntry>();
            var json = File.ReadAllText(HistoryPath);
            return JsonSerializer.Deserialize<List<HistoryEntry>>(json) ?? new List<HistoryEntry>();
        }
        catch
        {
            return new List<HistoryEntry>();
        }
    }

    public static void Save(List<HistoryEntry> entries)
    {
        Directory.CreateDirectory(SettingsDir);
        var json = JsonSerializer.Serialize(entries, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(HistoryPath, json);
    }
}

