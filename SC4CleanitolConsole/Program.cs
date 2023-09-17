using System.Text;
using SC4Cleanitol;
using System.CommandLine;
using System.IO;

public class Program {
    //accept 5 params: 2 required remaining optional
    //user plugins dir, cleanitol file path, system plugins dir (scan system directory - if system plugins param omitted this is false), verbose output, update tgi database
    //public static string userPluginsFolder = string.Empty;
    //private static string scriptPath = string.Empty;
    //private static string systemPluginsFolder = string.Empty;
    private static bool scanSystemPlugins = false;
    //private static bool verbose = false;
    //private static bool updateTGIdb = false;
    //private static string outputFolder = string.Empty;

    public static int Main(string[] args) {
        RootCommand rootCommand = new RootCommand("SC4Cleanitol is a modern implementation of the original program of the same name to remove out of date and non-game files from plugins and can tell you which dependencies you have/don't have for a given mod.");

        //Arguments
        Argument userPluginsArg = new Argument<string>(name: "userPluginsFolder", description: "Path of the user plugins folder.", parse: result => {
            string path = result.Tokens.Single().Value;
            if (!Directory.Exists(path)) {
                result.ErrorMessage = "The user plugins folder can not found.";
                return string.Empty;
            } else {
                return path;
            }
        });
        Argument scriptPathArg = new Argument<string>(name: "script", description: "Path of the script to execute.", isDefault: true, parse: result => {
            string path = result.Tokens.Single().Value;
            if (!File.Exists(path)) {
                result.ErrorMessage = "The script at the specified path was not found.";
                return string.Empty;
            } else {
                return path;
            }
        });
        rootCommand.Add(userPluginsArg);
        rootCommand.Add(scriptPathArg);

        //Options
        Option systemPluginsOpt = new Option<string>(name: "--systemPluginsFolder", description: "Path of the system plugins folder. Specify to scan the system plugins folder too.", parseArgument: result => {
            if (!result.Tokens.Any()) {
                scanSystemPlugins = true;
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SimCity 4\\BSC_Cleanitol");
            } else {
                return result.Tokens.Single().Value;
            }
        });
        systemPluginsOpt.AddValidator(result => {
            string? folder = (string?) result.GetValueForOption(systemPluginsOpt);
            if (folder is null || !Directory.Exists(folder)) {
                result.ErrorMessage = "System plugins folder is not valid.";
            }
        });
        Option outputOpt = new Option<string>(name: "--output", description: "Files to remove are moved to this folder.", parseArgument: result => {
            if (!result.Tokens.Any()) {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SimCity 4\\BSC_Cleanitol");
            } else {
                return result.Tokens.Single().Value;
            }
        });
        outputOpt.Arity = ArgumentArity.ZeroOrOne;
        outputOpt.AddValidator(result => {
            string? folder = (string?) result.GetValueForOption(outputOpt);
            if (folder is null || !Directory.Exists(folder)) {
                result.ErrorMessage = "Output folder is not valid.";
            }
        });
        Option verboseOpt = new Option<bool>(name: "--verbose", description: "Show all results, or only results that indicate an action to be taken.", getDefaultValue: () => true);
        Option updateTGIsOpt = new Option<bool>(name: "--updatetgis", description: "Update the internal TGI index by scanning all DBPF files in the plugin folder(s).", getDefaultValue: () => true);
        rootCommand.Add(systemPluginsOpt);
        rootCommand.Add(outputOpt);
        rootCommand.Add(verboseOpt); 
        rootCommand.Add(updateTGIsOpt);
        systemPluginsOpt.AddAlias("-sp");
        outputOpt.AddAlias("-o");
        verboseOpt.AddAlias("-v");
        updateTGIsOpt.AddAlias("-u");


        //Commands: RunScript, BackupFiles, ExportTGIs
        //Command runScript = new Command("run", "Execute a Cleanitol script");
        Command backupFiles = new Command("backup", "Move designated files to the output location");
        Command exportTGIs = new Command("export", "Export TGI index to CSV file in the root user plugins folder");
        //rootCommand.Add(runScript);
        rootCommand.Add(backupFiles);
        rootCommand.Add(exportTGIs);

        rootCommand.SetHandler(RunScript, 
            (System.CommandLine.Binding.IValueDescriptor<string>) userPluginsArg, 
            (System.CommandLine.Binding.IValueDescriptor<string>) scriptPathArg, 
            (System.CommandLine.Binding.IValueDescriptor<string>) systemPluginsOpt, 
            (System.CommandLine.Binding.IValueDescriptor<string>) outputOpt, 
            (System.CommandLine.Binding.IValueDescriptor<bool>) updateTGIsOpt, 
            (System.CommandLine.Binding.IValueDescriptor<bool>) verboseOpt
        );

        return rootCommand.InvokeAsync(args).Result;
    }

    private static void RunScript(string userPlugins, string scriptPath, string systemPlugins, string outputFolder, bool updateTGIs, bool verbose ) {
        Console.WriteLine("RunScript");
        Console.WriteLine("Plugins: " + userPlugins);
        Console.WriteLine("Script: " + scriptPath);
        Console.WriteLine("System Plugins: " + systemPlugins);
        Console.WriteLine("Output: " + outputFolder);
        Console.WriteLine("Update TGIs: " + updateTGIs);
        Console.WriteLine("Verbose: " + verbose);

        var progressTotalFiles = new Progress<int>(totalFiles => { });
        var progresScannedFiles = new Progress<int>(scannedFiles => { });
        var progressTotalTGIs = new Progress<int>(totalTGIs => { });


        CleanitolEngine cleanitol = new CleanitolEngine(userPlugins, systemPlugins, outputFolder, scriptPath);
        List<List<GenericRun>> runList = cleanitol.RunScript(progressTotalFiles, progresScannedFiles, progressTotalTGIs, updateTGIs, scanSystemPlugins, verbose);

        StringBuilder message = new StringBuilder();
        foreach (List<GenericRun> line in runList) {
            foreach (GenericRun run in line) {
                message.Append(ConvertRun(run));
            }
            Console.Write(message.ToString());
            message.Clear();
        }
    }





    //private static void RunScript() { 
    //    Console.WriteLine("RunScript");
    //    Console.WriteLine("Plugins: " + userPluginsFolder);
    //    Console.WriteLine("Script: " + scriptPath);

    //    outputFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SimCity 4\\BSC_Cleanitol");
    //    CleanitolEngine cleanitol = new CleanitolEngine(userPluginsFolder, systemPluginsFolder, outputFolder, scriptPath);
    //    List<List<GenericRun>> runList = cleanitol.RunScript(updateTGIdb, scanSystemPlugins, verbose);

    //    StringBuilder message = new StringBuilder();
    //    foreach (List<GenericRun> line in runList) {
    //        foreach (GenericRun run in line) {
    //            message.Append(ConvertRun(run));
    //        }
    //        Console.Write(message.ToString());
    //        message.Clear();
    //    }
    //}


    //private static void ValidateInputs(string[] args) {
    //    if (args.Length < 2 || args.Length > 5) {
    //        throw new ArgumentException("Invalid number of arguments provided! Must provide 2 to 5 arguments.");
    //    }


    //    userPluginsFolder = args[0];
    //    if (!Directory.Exists(userPluginsFolder)) {
    //        throw new DirectoryNotFoundException("Specified user plugins folder not found!");
    //    }

    //    //2 - Script Path
    //    scriptPath = args[1];
    //    if (!File.Exists(scriptPath)) {
    //        throw new DirectoryNotFoundException("The script at the specified path was not found!");
    //    }

    //    //3 - System Plugins Directory (and Scan System Plugins)
    //    scanSystemPlugins = false;
    //    if (args.Length >= 3) {
    //        systemPluginsFolder = args[2];
    //        if (!Directory.Exists(systemPluginsFolder)) {
    //            throw new DirectoryNotFoundException("The script at the specified path was not found!");
    //        } else if (systemPluginsFolder != string.Empty) {
    //            scanSystemPlugins = true;
    //        }
    //    } else {
    //        systemPluginsFolder = string.Empty;
    //    }

    //    //4 - Verbose Output
    //    if (args.Length >= 4) {
    //        if (!bool.TryParse(args[3], out verbose)) {
    //            throw new ArgumentException($"Argument to 'verbose' must be a valid boolean value!. '{args[3]}' was provided.");
    //        };
    //    }

    //    //5 - Update TGI Database
    //    if (args.Length == 5) {
    //        if (!bool.TryParse(args[4], out updateTGIdb)) {
    //            throw new ArgumentException($"Argument to 'update TGI database' must be a valid boolean value!. '{args[4]}' was provided.");
    //        }
    //    }
    //}

    private static string ConvertRun(GenericRun genericRun) {
        switch (genericRun.Type) {
            case RunType.BlueStd:
            case RunType.BlueMono:
            case RunType.RedStd:
            case RunType.RedMono:
            case RunType.GreenStd:
            case RunType.BlackMono:
            case RunType.BlackStd:
                return genericRun.Text;
            case RunType.BlackHeading:
                return "\r\n" + genericRun.Text + new string('=', genericRun.Text.Length - 4) + "\r\n"; //Minus 4 for the ">#" at the start and the "\r\n" at the end
            case RunType.Hyperlink:
                return genericRun.Text + " >> " + genericRun.URL;
            default:
                return string.Empty;
        }
    }


    //static void EchoArguments() {
    //    Console.WriteLine("userPluginsDir: " + userPluginsFolder);
    //    Console.WriteLine("scriptPath: " + scriptPath);
    //    Console.WriteLine("systemPluginsDir: " + systemPluginsFolder);
    //    Console.WriteLine("scanSystemPlugins: " + scanSystemPlugins);
    //    Console.WriteLine("updateTGIDB: " + updateTGIdb);
    //    Console.WriteLine("verbose: " + verbose);
    //}
}