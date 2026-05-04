using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using 全局文件搜索.Models;

namespace 全局文件搜索.Services;

public sealed class SearchHistoryService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    private readonly string _historyFilePath;

    public SearchHistoryService()
    {
        string appDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "全局文件搜索");
        Directory.CreateDirectory(appDataDir);
        _historyFilePath = Path.Combine(appDataDir, "search_history.json");
    }

    public void AddEntry(SearchHistoryEntry entry)
    {
        var history = LoadAll();
        history.Insert(0, entry);

        const int maxEntries = 100;
        if (history.Count > maxEntries)
        {
            history = history.Take(maxEntries).ToList();
        }

        Save(history);
    }

    public bool AddSavedResults(Guid entryId, IEnumerable<SavedResultSnapshot> results)
    {
        var history = LoadAll();
        SearchHistoryEntry? entry = history.FirstOrDefault(item => item.Id == entryId);

        if (entry is null)
        {
            return false;
        }

        foreach (SavedResultSnapshot snapshot in results)
        {
            bool exists = entry.SavedResults.Any(item =>
                item.FullPath.Equals(snapshot.FullPath, StringComparison.OrdinalIgnoreCase));

            if (!exists)
            {
                entry.SavedResults.Add(snapshot);
            }
        }

        Save(history);
        return true;
    }

    public List<SearchHistoryEntry> LoadAll()
    {
        try
        {
            if (!File.Exists(_historyFilePath))
                return [];

            string json = File.ReadAllText(_historyFilePath);
            return JsonSerializer.Deserialize<List<SearchHistoryEntry>>(json, JsonOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }

    public List<SearchHistoryEntry> LoadRecent(int count)
    {
        return LoadAll().Take(count).ToList();
    }

    public int GetTotalSearchCount()
    {
        return LoadAll().Count;
    }

    public double GetTotalElapsedSeconds()
    {
        return LoadAll().Sum(e => e.ElapsedSeconds);
    }

    public void ClearAll()
    {
        try
        {
            if (File.Exists(_historyFilePath))
            {
                File.Delete(_historyFilePath);
            }
        }
        catch
        {
        }
    }

    private void Save(List<SearchHistoryEntry> history)
    {
        try
        {
            string json = JsonSerializer.Serialize(history, JsonOptions);
            File.WriteAllText(_historyFilePath, json);
        }
        catch
        {
        }
    }
}
