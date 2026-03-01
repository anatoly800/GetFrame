using System.Text.Json;

namespace GetFrame.Core.Services;

public class SettingsService : ISettingsService
{
    private readonly string _settingsFilePath;
    private Dictionary<string, string> _settingsCache = [];
    private readonly Lock _semaphore = new();

    public SettingsService(string fileName = "GetFrameSettings.json")
    {
        var appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GetFrame");
        if (!Directory.Exists(appDataFolder))
        {
            Directory.CreateDirectory(appDataFolder);
        }
        _settingsFilePath = Path.Combine(appDataFolder, fileName);
        _ = LoadSettings();
    }

    private async Task LoadSettings()
    {
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                using var stream = File.OpenRead(_settingsFilePath);
                _settingsCache = await JsonSerializer.DeserializeAsync<Dictionary<string, string>>(stream) ?? [];
            }
        }
        catch (Exception)
        {
            // Ignore loading errors, treat as empty cache
        }
    }

    public string? GetKey(string key)
    {
        lock (_semaphore)
        {
            if (_settingsCache.TryGetValue(key, out var value))
            {
                return value;
            }
            return null;
        }
    }

    public void SetKey(string key, string value)
    {
        lock (_semaphore)
        {
            try
            {
                _settingsCache[key] = value;
                string? dir = Path.GetDirectoryName(_settingsFilePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                var tmpFile = Path.Combine(_settingsFilePath, ".tmp");
                using var writeStream = File.Create(tmpFile);
                JsonSerializerOptions jsonSerializerOptions = new() { WriteIndented = true };
                JsonSerializerOptions options = jsonSerializerOptions;
                JsonSerializer.Serialize(writeStream, _settingsCache, options);
                writeStream.Flush();
                File.Move(tmpFile, _settingsFilePath, true);
            }
            catch
            {
                // Ignore
            }
        }
    }
}
