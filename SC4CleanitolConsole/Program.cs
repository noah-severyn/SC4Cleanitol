using System.Text;
using SC4Cleanitol;
using CommandLine;
using SC4CleanitolConsole;

public class Program {

    private static readonly Version releaseVersion = new Version(0, 2);
    private static readonly string releaseDate = "Dec 2024";

    public static void Main(string[] args) {

        var parser = new CommandLine.Parser(with => with.HelpWriter = null);

        var exitcode = parser.ParseArguments<RunOptions, CreateOptions>(args)
            .WithParsed<RunOptions>(Run)
            .WithParsed<CreateOptions>(Create)
            .WithNotParsed(HandleParseError);
        Console.WriteLine("");
    }


    private static void HandleParseError(IEnumerable<Error> errs) {
        if (errs.IsVersion()) {
            Console.WriteLine("Version Request");
            return;
        }

        if (errs.IsHelp()) {
            Console.WriteLine("Help Request");
            //DisplayHelp(parserResult, errs)
            return;
        }
        Console.WriteLine("Parser Fail");
    }
    //static void DisplayHelp<T>(ParserResult<T> result) {
    //    var helpText = HelpText.AutoBuild(result, h =>
    //    {
    //        h.AdditionalNewLineAfterOption = false;
    //        h.Heading = "Myapp 2.0.0-beta"; //change header
    //        h.Copyright = "Copyright (c) 2019 Global.com"; //change copyright text
    //        return HelpText.DefaultParsingErrorsHandler(result, h);
    //    }, e => e);
    //    Console.WriteLine(helpText);
    //}

    private static void Run(RunOptions opts) {
        Console.WriteLine("Parser success - Run");

        CleanitolEngine cleanitol = new CleanitolEngine(opts.UserPlugins, opts.SystemPlugins, opts.CleanitolOutput, opts.ScriptPath);
    }


    /// <summary>
    /// Create a new Cleanitol file containing all files in the chosen folder and its subfolders.
    /// </summary>
    private static void Create(CreateOptions opts) {
        CleanitolEngine.CreateCleanitolList(opts.FolderPath, opts.ScriptPath);
    }


    private static void ConvertRun(FormattedRun genericRun) {
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
