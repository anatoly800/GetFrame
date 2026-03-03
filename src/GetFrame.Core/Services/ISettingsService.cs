namespace GetFrame.Core.Services;

public interface ISettingsService
{
    event Action<string>? OnStatusChanged;
    string? GetKey(string key);
    void SetKey(string key, string value);
}
