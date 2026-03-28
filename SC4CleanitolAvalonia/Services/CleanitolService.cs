using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public Task ScanAsync(bool parseTGIs = false, IProgress<CleanitolEngine.CleanitolProgress>? progress = null) {
            return Task.Run(() => _engine.Scan(parseTGIs, progress));
        }

        public Task RunAsync(string scriptPath, bool resetResults = true) {
            return Task.Run(() => _engine.Run(scriptPath, resetResults));
        }

        public Task ProcessRuleAsync(string rule, bool resetResults = false) {
            return Task.Run(() => _engine.ProcessRule(rule, resetResults));
        }
    }
}
