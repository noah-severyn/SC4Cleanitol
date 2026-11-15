using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SC4CleanitolEngine;

namespace SC4CleanitolAvalonia.Services;

/// <summary>
/// Provides task-based operations for configuring and using the Cleanitol Engine by acting as a service layer on top of <see cref="CleanitolEngine"/>.
/// </summary>
internal class CleanitolService: ICleanitolService {
    private readonly CleanitolEngine _engine = new();

    public CleanitolEngine.CleanitolResult Results => _engine.Results;

    ///<inheritdoc/>
    public void Configure(List<string> pluginFolders, string outputDirectory) {
        _engine.PluginFolders = pluginFolders;
        _engine.OutputDirectory = outputDirectory;
    }

    ///<inheritdoc/>
    public Task ScanAsync(IProgress<CleanitolEngine.CleanitolProgress>? progress = null, bool parseTGIs = false) {
        return Task.Run(() => _engine.Scan(progress, parseTGIs));
    }

    ///<inheritdoc/>
    public Task RunAsync(string scriptPath, bool ignoreSc4pac = true, bool resetResults = true) {
        return Task.Run(() => _engine.Run(scriptPath, ignoreSc4pac, resetResults));
    }

    ///<inheritdoc/>
    public Task RunAsync(string scriptName, IEnumerable<string> rules, bool ignoreSc4pac = true, bool resetResults = false) {
        return Task.Run(() => _engine.Run(scriptName, rules, ignoreSc4pac, resetResults));
    }
}
