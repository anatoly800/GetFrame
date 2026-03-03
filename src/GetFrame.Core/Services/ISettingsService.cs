namespace GetFrame.Core.Services;

public interface ISettingsService
{
    event Action<string>? OnError;
    string? GetKey(string key);
    void SetKey(string key, string value);
}
