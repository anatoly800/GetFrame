using System.Threading.Tasks;

namespace GetFrame.Core.Services;

public interface ISettingsService
{
    Task<string?> LoadAsync(string key);
    Task SaveAsync(string key, string value);
}
