using System.Text;
using SC4Cleanitol;
using System.CommandLine;

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
    private static readonly Version releaseVersion = new Version(0, 1);
    private static readonly string releaseDate = "Oct 2023";

    public static void Main(string[] args) {
        RootCommand rootCommand = new RootCommand("SC4Cleanitol is a modern implementation of the original program of the same name to remove out of date and non-game files from plugins and can tell you which dependencies you have/don't have for a given mod.");

        //Command runCmd = new Command("start", "Start the SC4Cleanitol program.");
        //Command runCmd = new Command("run", "Run a Cleanitol script.");
        //Command backupCmd = new Command("backup", "Move designated files to the output location.");
        //Command exportCmd = new Command("export", "Export TGI index to CSV file in the Cleanitol output folder.");
        //Command exitCmd = new Command("exit", "Exit the application.");
        //Command chkUpdateCmd = new Command("check-updates", "Check for updates.");
        //rootCommand.Add(runCmd);
        //rootCommand.Add(backupCmd);
        //rootCommand.Add(exportCmd);
        //rootCommand.Add(chkUpdateCmd);
        //rootCommand.Add(exitCmd);

        string[] baseCommands = { "run", "backup", "export", "about", "exit", "help" };
        List<string> arguments = new List<string>();

        string command;
        CleanitolEngine cleanitol = new CleanitolEngine(string.Empty, string.Empty, string.Empty, string.Empty);
        do {
            // Take user input
            Console.Write("SC4Cleanitol > ");
            string? input = Console.ReadLine();

            // Parse the input from the user, assuming the first word is the command and everything else is input for that command.
            if (input is null || input is "") {
                command = "help";
            } else {
                command = input.Split(" ").ToList().First().ToLower();
                arguments = input.Split("--").ToList();
            }


            // Perform action based on the command the user inputted
            switch (command) {
                case "run":
                    RunScript("", "", "", "",true, true);
                    break;
                case "backup":
                    Backup("");
                    break;
                case "export":
                    Export();
                    break;
                case "about":
                    About();
                    break;
                case "help":
                    ShowHelp("");
                    break;
                case "exit":
                    break;
            }
            Console.WriteLine();

        } while (command != "exit"); // Loop forever while the user doesn't enter 'quit'


        //rootCommand.SetHandler(RunScript, 
        //    (System.CommandLine.Binding.IValueDescriptor<string>) userPluginsArg, 
        //    (System.CommandLine.Binding.IValueDescriptor<string>) scriptPathArg, 
        //    (System.CommandLine.Binding.IValueDescriptor<string>) systemPluginsOpt, 
        //    (System.CommandLine.Binding.IValueDescriptor<string>) outputOpt, 
        //    (System.CommandLine.Binding.IValueDescriptor<bool>) updateTGIsOpt, 
        //    (System.CommandLine.Binding.IValueDescriptor<bool>) verboseOpt
        //);

        
    }


    public static void ShowHelp(string command) {
        string desc;
        string usage;
        Dictionary<string, string> options = new Dictionary<string, string>();
        switch (command) {
            case "": {
                    desc = "SC4Cleanitol is a modern implementation of the original program of the same name to remove out of date and non-game files from plugins and can tell you which dependencies you have/don't have for a given mod.";
                    usage = "command[options]";
                    options.Add("run", "Run a Cleanitol script.");
                    options.Add("backup", "Move designated files to the output location.");
                    options.Add("export", "Export TGI index to CSV file in the Cleanitol output folder.");
                    options.Add("about", "View program information and check for updates.");
                    options.Add("help", "Show help instructions for a command.");
                    options.Add("exit", "Exit the SC4Cleanitol program.");
                    break;
                }
            default: {
                    desc = "";
                    usage = " ";
                    break;
                }
        }

        WriteHelp(desc, usage, options);
    }
    private static void WriteHelp(string desc, string usage, Dictionary<string,string> options) {
        Console.WriteLine();
        Console.WriteLine("Description:");
        Console.WriteLine("    " + desc);
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("    " + usage);
        Console.WriteLine();
        Console.WriteLine("Commands:");
        foreach (string key in options.Keys) {
            Console.WriteLine("    " + key + new string(' ', 20 - key.Length) + options.GetValueOrDefault(key));
        }
    }

    //private static void StartProgram() {

    //}


    private static void RunScript(string userPlugins, string systemPlugins, string scriptPath, string cleanitolOutput, bool updateTGIs, bool verbose ) {
        Console.WriteLine("RunScript");
        Console.WriteLine("user-plugins: " + userPlugins);
        Console.WriteLine("system-plugins: " + systemPlugins);
        Console.WriteLine("script-path: " + scriptPath);
        Console.WriteLine("cleanitol-output: " + cleanitolOutput);
        Console.WriteLine("update-tgis: " + updateTGIs);
        Console.WriteLine("verbose: " + verbose);

        var progressTotalFiles = new Progress<int>(totalFiles => { });
        var progresScannedFiles = new Progress<int>(scannedFiles => { });
        var progressTotalTGIs = new Progress<int>(totalTGIs => { });


        CleanitolEngine cleanitol = new CleanitolEngine(userPlugins, systemPlugins, cleanitolOutput, scriptPath);
        List<List<GenericRun>> runList = cleanitol.RunScript(progressTotalFiles, progresScannedFiles, progressTotalTGIs, updateTGIs, scanSystemPlugins, verbose);

        StringBuilder message = new StringBuilder();
        foreach (List<GenericRun> line in runList) {
            foreach (GenericRun run in line) {
                //message.Append(ConvertRun(run));
            }
            Console.Write(message.ToString());
            message.Clear();
        }
    }

    private static void Backup(string backupPath) {
        Console.WriteLine("Backup");
        Console.WriteLine("backup-path: " + backupPath);
    }


    private static void Export() {
        Console.WriteLine("Export");
    }

    private static void About() {
        Console.WriteLine();
        Console.WriteLine("Program Name:      SC4CleanitolConsole");
        Console.WriteLine("Current Version:   " + releaseVersion);
        Console.WriteLine("Release Date:      " + releaseDate);
        Console.Write("For updates visit: ");
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.Write("https://github.com/noah-severyn/SC4Cleanitol/releases/latest\r\n");
        Console.ResetColor();
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

    private static void ConvertRun(GenericRun genericRun) {
        switch (genericRun.Type) {
            case RunType.BlueStd:
            case RunType.BlueMono:
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.Write(genericRun.Text);
                break;
            case RunType.RedStd:
            case RunType.RedMono:
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write(genericRun.Text);
                break;
            case RunType.GreenStd:
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write(genericRun.Text);
                break;
            case RunType.BlackMono:
            case RunType.BlackStd:
                Console.ResetColor();
                Console.Write(genericRun.Text);
                break;
            case RunType.BlackHeading:
                Console.ResetColor();
                Console.Write("\r\n" + genericRun.Text + new string('=', genericRun.Text.Length - 4) + "\r\n"); //Minus 4 for the ">#" at the start and the "\r\n" at the end
                break;
            case RunType.Hyperlink:
                Console.ResetColor();
                Console.Write(genericRun.Text + " >> ");
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write(genericRun.URL);
                break;
            default:
                Console.ResetColor();
                break;
        }
    }
}