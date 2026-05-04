using System;
using System.IO;
using System.Text.Json;
using 全局文件搜索.Models;

namespace 全局文件搜索.Services;

public sealed class AppPreferencesService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    private readonly string _settingsFilePath;

    public AppPreferencesService()
    {
        string appDataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "全局文件搜索");

        Directory.CreateDirectory(appDataDirectory);
        _settingsFilePath = Path.Combine(appDataDirectory, "settings.json");
    }

    public AppPreferences Load()
    {
        try
        {
            if (!File.Exists(_settingsFilePath))
            {
                return new AppPreferences();
            }

            string json = File.ReadAllText(_settingsFilePath);
            return JsonSerializer.Deserialize<AppPreferences>(json, JsonOptions) ?? new AppPreferences();
        }
        catch
        {
            return new AppPreferences();
        }
    }

    public void Save(AppPreferences preferences)
    {
        try
        {
            string json = JsonSerializer.Serialize(preferences, JsonOptions);
            File.WriteAllText(_settingsFilePath, json);
        }
        catch
        {
        }
    }
}
