using System;
using System.Collections.Generic;

namespace SC4CleanitolAvalonia.Models;

public class UserConfig {
    public string UserPluginsPath { get; set; } = string.Empty;
    public string SystemPluginsPath { get; set; } = string.Empty;
    public bool IncludeSystemPlugins { get; set; }
    public bool CheckSc4pacDuplicates { get; set; }
    public string OutputPath { get; set; } = string.Empty;
    public DateTime LastScanned { get; set; }
    public string? PluginsChecksum { get; set; } = string.Empty;
    public List<string> AdditionalFolders { get; set; } = [];
}