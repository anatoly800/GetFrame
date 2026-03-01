using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace GetFrame.Core.Services;

public class SettingsService : ISettingsService
{
    private readonly string _settingsFilePath;

    public SettingsService(string fileName = "GetFrameSettings.json")
    {
        var appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GetFrame");
        if (!Directory.Exists(appDataFolder))
        {
            Directory.CreateDirectory(appDataFolder);
        }
        _settingsFilePath = Path.Combine(appDataFolder, fileName);
    }

    public async Task<string?> LoadAsync(string key)
    {
        if (!File.Exists(_settingsFilePath))
        {
            return null;
        }

        try
        {
            using var stream = File.OpenRead(_settingsFilePath);
            var settings = await JsonSerializer.DeserializeAsync<Dictionary<string, string>>(stream);
            if (settings != null && settings.TryGetValue(key, out var value))
            {
                return value;
            }
            return null;
        }
        catch (Exception)
        {
            // If serialization fails or file is corrupted, return default
            return null;
        }
    }

    public async Task SaveAsync(string key, string value)
    {
        try
        {
            string? dir = Path.GetDirectoryName(_settingsFilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            Dictionary<string, string> settings;
            if (File.Exists(_settingsFilePath))
            {
                using var stream = File.OpenRead(_settingsFilePath);
                settings = await JsonSerializer.DeserializeAsync<Dictionary<string, string>>(stream) ?? new Dictionary<string, string>();
            }
            else
            {
                settings = [];
            }

            settings[key] = value;

            using var writeStream = File.Create(_settingsFilePath);
            JsonSerializerOptions options = new() { WriteIndented = true };
            await JsonSerializer.SerializeAsync(writeStream, settings, options);
        }
        catch (Exception)
        {
            // Ignore or log error
        }
    }
}
