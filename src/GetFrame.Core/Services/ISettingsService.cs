using System.Threading.Tasks;

namespace GetFrame.Core.Services;

public interface ISettingsService
{
    string? GetKey(string key);
    void SetKey(string key, string value);
}
