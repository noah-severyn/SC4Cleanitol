using CommandLine;

namespace SC4CleanitolConsole {

    [Verb("run", HelpText = "Execute the script and return the results of each rule.")]
    internal class RunOptions {
        //Required Arguments
        [Value(0, Required = true)]
        public string ScriptPath { get; set; }

        [Option('u', "user-plugins", Required = true, HelpText = "Location of the plugins folder found in the user's Documents folder.")]
        public string UserPlugins { get; set; }

        [Option('o', "cleanitol-output", Required = true, HelpText = "Location files will be removed to and the output summary will be saved to.")]
        public string CleanitolOutput { get; set; }


        //Run Options
        [Option('t', "update-tgis", Default = false, HelpText = "Whether to update the TGI index by rescanning the plugins folder.")]
        public bool UpdateTTGIdb { get; set; }

        [Option('s', "system-plugins",  HelpText = "Location of the plugins folder found in the game install directory.")]
        public string SystemPlugins { get; set; }

        [Option('y', "scan-system-plugins", Default = true, HelpText = "Whether to include the system plugins folder in the TGI scan or only the user plugins.")]
        public bool ScanSystemPlugins { get; set; }

        [Option('a', "include-addditional-folders", Default = 0, HelpText = "Specify which folders to scan. 0: Plugins only, 1: Plugins + additional folders, 2: Additional folders only.")]
        public byte AdditionalFolderOption { get; set; }

        [Option('f', "additional-folders", Separator = ';', Max = 50, HelpText = "A list of additional folders to scan in addition to the current plugins folders.")]
        public IEnumerable<string> AdditionalFolders { get; set; }


        //Output Options
        [Option('v', "verbose", Default = false, HelpText = "Whether to show all output to the screen or just actionable outputs.")]
        public bool DetailedOutput { get; set; }
    }
    


    [Verb("create", HelpText = "Create a new Cleanitol file containing all files in the chosen folder and its subfolders.")]
    internal class CreateOptions {

        [Option('f', "input-folder", Required = true, HelpText = "Folder containing files to add to the script.")]
        public string FolderPath { get; set; }

        [Option('o', "output-file", Required = true, HelpText = "Path of the Cleanitol script to create.")]
        public string ScriptPath { get; set; }

    }
}
