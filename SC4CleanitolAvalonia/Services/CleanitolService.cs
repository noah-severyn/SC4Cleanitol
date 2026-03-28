using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SC4CleanitolEngine;

namespace SC4CleanitolAvalonia.Services {
    internal class CleanitolService: ICleanitolService {
        private readonly CleanitolEngine _engine = new();

        public CleanitolEngine.CleanitolResult Results => _engine.Results;

        public void Configure(List<string> pluginFolders, string outputDirectory) {
            _engine.PluginFolders = pluginFolders;
            _engine.OutputDirectory = outputDirectory;
        }

        public Task ScanAsync(IProgress<CleanitolEngine.CleanitolProgress>? progress = null, bool parseTGIs = false) {
            return Task.Run(() => _engine.Scan(progress, parseTGIs));
        }

        public Task RunAsync(string scriptPath, bool resetResults = true) {
            return Task.Run(() => _engine.Run(scriptPath, resetResults));
        }

        public Task RunAsync(string scriptName, List<string> rules, bool resetResults = false) {
            return Task.Run(() => _engine.Run(scriptName, rules, resetResults));
        }
    }
}
