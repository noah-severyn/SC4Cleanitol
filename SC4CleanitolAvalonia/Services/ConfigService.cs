using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using SC4CleanitolAvalonia.Models;

namespace SC4CleanitolAvalonia.Services;

public class ConfigService : IConfigService {
    private static readonly string ConfigPath = Path.Combine( Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SC4Cleanitol", "config.json");

    public UserConfig Load() {
        if (File.Exists(ConfigPath)) {
            var json = File.ReadAllText(ConfigPath);
            return JsonSerializer.Deserialize<UserConfig>(json) ?? new UserConfig();
        }
        return new UserConfig();
    }

    public async Task<UserConfig> LoadAsync() {
        if (File.Exists(ConfigPath)) {
            using var stream = File.OpenRead(ConfigPath);
            return await JsonSerializer.DeserializeAsync<UserConfig>(stream) ?? new UserConfig();
        }
        return new UserConfig();
    }

    public void Save(UserConfig config) {
        Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(ConfigPath, json);
    }

    public async Task SaveAsync(UserConfig config) {
        Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
        using var stream = File.Create(ConfigPath);
        await JsonSerializer.SerializeAsync(stream, config, new JsonSerializerOptions { WriteIndented = true });
    }
}