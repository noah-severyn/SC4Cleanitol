using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SC4Cleanitol;
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

        public Task RunAsync(string scriptPath, bool ignoreSc4pac = true, bool resetResults = true) {
            return Task.Run(() => _engine.Run(scriptPath, ignoreSc4pac, resetResults));
        }

        public Task RunAsync(string scriptName, IEnumerable<string> rules, bool ignoreSc4pac = true, bool resetResults = false) {
            return Task.Run(() => _engine.Run(scriptName, rules, ignoreSc4pac, resetResults));
        }
    }
}
