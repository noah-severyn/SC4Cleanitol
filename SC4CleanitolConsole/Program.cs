using System.Text;
using SC4Cleanitol;

public class Program {
    //accept 5 params: 2 required remaining optional
    //user plugins dir, cleanitol file path, system plugins dir (scan system directory - if system plugins param omitted this is false), verbose output, update tgi database
    public static string userPluginsDir = string.Empty;
    private static string systemPluginsDir = string.Empty;
    private static bool scanSystemPlugins = false;
    private static bool verbose = false;
    private static bool updateTGIdb = false;
    private static string scriptPath = string.Empty;

    public static void Main(string[] args) {
        ValidateInputs(args);

        string cleantiolOutputDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SimCity 4\\BSC_Cleanitol");
        CleanitolEngine cleanitol = new CleanitolEngine(userPluginsDir, systemPluginsDir, cleantiolOutputDir, scriptPath);
        List<GenericRun> runList = cleanitol.RunScript(updateTGIdb, scanSystemPlugins, verbose);

        foreach (GenericRun run in runList) {
            Console.WriteLine(ConvertRun(run));
        }
    }

    static void ValidateInputs(string[] args) {
        if (args.Length < 2 || args.Length > 5) {
            throw new ArgumentException("Invalid number of arguments provided! Must provide 2 to 5 arguments.");
        }

        //1 - User Plugins Directory
        userPluginsDir = args[0];
        if (!Directory.Exists(userPluginsDir)) {
            throw new DirectoryNotFoundException("Specified user plugins folder not found!");
        }

        //2 - Script Path
        scriptPath = args[1];
        if (!File.Exists(scriptPath)) {
            throw new DirectoryNotFoundException("The script at the specified path was not found!");
        }

        //3 - System Plugins Directory (and Scan System Plugins)
        scanSystemPlugins = false;
        if (args.Length >= 3) {
            systemPluginsDir = args[2];
            if (!Directory.Exists(systemPluginsDir)) {
                throw new DirectoryNotFoundException("The script at the specified path was not found!");
            } else if (systemPluginsDir != string.Empty) {
                scanSystemPlugins = true;
            }
        } else {
            systemPluginsDir = string.Empty;
        }

        //4 - Verbose Output
        if (args.Length >= 4) {
            if (!bool.TryParse(args[3], out verbose)) {
                throw new ArgumentException($"Argument to 'verbose' must be a valid boolean value!. '{args[3]}' was provided.");
            };
        }

        //5 - Update TGI Database
        if (args.Length == 5) {
            if (!bool.TryParse(args[4], out updateTGIdb)) {
                throw new ArgumentException($"Argument to 'update TGI database' must be a valid boolean value!. '{args[4]}' was provided.");
            }
        }
    }



    static string ConvertRun(GenericRun genericRun) {
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
                return "\r\n" + genericRun.Text + "\r\n" + new string('=', genericRun.Text.Length);
            case RunType.Hyperlink:
                return genericRun.Text + " >> " + genericRun.URL;
            default:
                return string.Empty;
        }
    }


    static void EchoArguments() {
        Console.WriteLine("userPluginsDir: " + userPluginsDir);
        Console.WriteLine("scriptPath: " + scriptPath);
        Console.WriteLine("systemPluginsDir: " + systemPluginsDir);
        Console.WriteLine("scanSystemPlugins: " + scanSystemPlugins);
        Console.WriteLine("updateTGIDB: " + updateTGIdb);
        Console.WriteLine("verbose: " + verbose);
    }
}