using System.Text.Json;

namespace GetFrame.Core.Services;

public class SettingsService : ISettingsService
{
    private readonly string _settingsFilePath;
    private readonly Dictionary<string, string> _settingsCache = [];
    private readonly Lock _semaphore = new();

    public SettingsService(string fileName = "GetFrameSettings.json")
    {
        var appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GetFrame");
        if (!Directory.Exists(appDataFolder))
        {
            Directory.CreateDirectory(appDataFolder);
        }
        _settingsFilePath = Path.Combine(appDataFolder, fileName);
        if (!File.Exists(_settingsFilePath))
        {
            return;
        }
        try
        {
            _settingsCache = JsonSerializer.Deserialize<Dictionary<string, string>>(File.OpenRead(_settingsFilePath)) ?? [];
            Console.WriteLine($"Loaded settings: {_settingsFilePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load settings: {ex.Message}");
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
