using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SC4CleanitolEngine;

namespace SC4CleanitolAvalonia.Services {
    internal interface ICleanitolService {
        /// <summary>
        /// Configure the engine with the folders to scan and the output directory.
        /// </summary>
        void Configure(List<string> pluginFolders, string outputDirectory);

        /// <summary>
        /// Parse the folders provided for files and optionally TGIs.
        /// </summary>
        /// <param name="progress">Progress status of the scan.</param>
        /// <param name="parseTGIs">Parse the TGIs out of each file found in <see cref="PluginFolders"/></param>
        Task ScanAsync(IProgress<CleanitolEngine.CleanitolProgress>? progress = null, bool parseTGIs = false);

        /// <summary>
        /// Run the specified script.
        /// </summary>
        /// <param name="scriptPath">Path of the script to run. May either be a local file path or a Github raw url.</param>
        /// <param name="resetResults">Reset the contents of <see cref="Results"/> before running the script. Default is <see langword="true"/>.</param>
        Task RunAsync(string scriptPath, bool resetResults = true);

        /// <summary>
        /// Process a collection of rules as if they were a script.
        /// </summary>
        /// <param name="scriptName">Name of the script.</param>
        /// <param name="rules">A collection of rules to run.</param>
        /// <param name="resetResults">Reset the contents of <see cref="Results"/> before running the script. Default is <see langword="true"/>.</param>
        Task RunAsync(string scriptName, List<string> rules, bool resetResults = true);

        /// <summary>
        /// The results of the most recent scan/run.
        /// </summary>
        CleanitolEngine.CleanitolResult Results { get; }
    }
}
