
namespace SC4CleanitolEngine {
    /// <summary>
    /// Provides task-based operations for configuring and using the Cleanitol Engine by acting as a service layer on top of <see cref="CleanitolEngine"/>. Applications should implement this interface to code against.
    /// </summary>
    public interface ICleanitolService {
        /// <summary>
        /// Configure the engine with the folders to scan and the output directory.
        /// </summary>
        /// <param name="pluginFolders">One or more folder paths to scan.</param>
        /// <param name="outputDirectory">Directory path where output is written and files are moved to.</param>
        void Configure(List<string> pluginFolders, string outputDirectory);

        /// <summary>
        /// Parse the specified folders to generate an internal index of files and optionally, TGIs. Required before any other actions may be taken.
        /// </summary>
        /// <param name="progress">Progress status of the scan.</param>
        /// <param name="parseTGIs">Parse the TGIs out of each file found in <see cref="CleanitolEngine.PluginFolders"/>. If <see langword="false"/> (the default value), only files are scanned.</param>
        /// <returns>A task that represents the asynchronous scan operation.</returns>
        Task ScanAsync(IProgress<CleanitolEngine.CleanitolProgress>? progress = null, bool parseTGIs = false);

        /// <summary>
        /// Run the specified script.
        /// </summary>
        /// <remarks>Execution is offloaded by using Task.Run; exceptions are captured in the returned task.</remarks>
        /// <param name="scriptPath">Path of the script to run. May either be a local file path or a GitHub raw url.</param>
        /// <param name="ignoreSc4pac">Ignore files found in an sc4pac folder. Default is <see langword="true"/>.</param>
        /// <param name="resetResults">Reset the contents of <see cref="CleanitolEngine.Results"/> before running the script. Default is <see langword="true"/>.</param>
        Task RunAsync(string scriptPath, bool ignoreSc4pac = true, bool resetResults = true);

        /// <summary>
        /// Process a collection of rules as if they were a script.
        /// </summary>
        /// <param name="scriptName">Name of the script.</param>
        /// <param name="rules">A collection of rules to run.</param>
        /// <param name="ignoreSc4pac">Ignore files found in an sc4pac folder. Default is <see langword="true"/>.</param>
        /// <param name="resetResults">Reset the contents of <see cref="CleanitolEngine.Results"/> before running the script. Default is <see langword="true"/>.</param>
        Task RunAsync(string scriptName, IEnumerable<string> rules, bool ignoreSc4pac = true, bool resetResults = true);

        /// <summary>
        /// The results of the most recent run.
        /// </summary>
        CleanitolEngine.CleanitolResult Results { get; }
    }
}
