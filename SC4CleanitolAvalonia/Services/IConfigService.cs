using System.Threading.Tasks;
using SC4CleanitolAvalonia.Models;

namespace SC4CleanitolAvalonia.Services;

public interface IConfigService {
    UserConfig Load();
    Task<UserConfig> LoadAsync();
    void Save(UserConfig config);
    Task SaveAsync(UserConfig config);
}