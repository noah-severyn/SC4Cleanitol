using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using SC4CleanitolAvalonia.Models;

namespace SC4CleanitolAvalonia.Services;

/// <summary>
/// Provides methods to load and save user configuration settings.
/// </summary>
/// <remarks>Configuration is saved in <c>%AppData%\SC4Cleanitol\config.json</c>. Loading applies default user plugin and output folders when values are missing.</remarks>
public class ConfigService : IConfigService {
    private static readonly string configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SC4Cleanitol", "config.json");
    private static readonly string userPlugins = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SimCity 4", "Plugins");
    private static readonly string pluginsBackup = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SimCity 4", "Plugins_Backup");

    public UserConfig Load() {
        if (File.Exists(configPath)) {
            var json = File.ReadAllText(configPath);
            return ApplyDefaults(JsonSerializer.Deserialize<UserConfig>(json) ?? new UserConfig());
        }

        return ApplyDefaults(new UserConfig());
    }

    public async Task<UserConfig> LoadAsync() {
        if (File.Exists(configPath)) {
            using var stream = File.OpenRead(configPath);
            return ApplyDefaults(await JsonSerializer.DeserializeAsync<UserConfig>(stream) ?? new UserConfig());
        }

        return ApplyDefaults(new UserConfig());
    }

    public void Save(UserConfig config) {
        Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(configPath, json);
    }

    public async Task SaveAsync(UserConfig config) {
        Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
        using var stream = File.Create(configPath);
        await JsonSerializer.SerializeAsync(stream, config, new JsonSerializerOptions { WriteIndented = true });
    }

    private static UserConfig ApplyDefaults(UserConfig config) {
        if (string.IsNullOrWhiteSpace(config.UserPluginsPath)) {
            config.UserPluginsPath = userPlugins;
        }

        if (string.IsNullOrWhiteSpace(config.OutputPath)) {
            config.OutputPath = pluginsBackup;
        }

        return config;
    }
}
