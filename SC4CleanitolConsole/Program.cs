using System.Text;
using SC4Cleanitol;
using CommandLine;
using SC4CleanitolConsole;

public class Program {

    private static readonly Version releaseVersion = new Version(0, 2);
    private static readonly string releaseDate = "Dec 2024";



    /// <summary>
    /// 
    /// </summary>
    /// <param name="args"></param>
    public static void Main(string[] args) {
        //args should be in this order: user-plugins, system-plugins, cleanitol-output, script-path
        //optional args: 

        

        
        var result = Parser.Default.ParseArguments<Options>(args)
            .WithParsed(Run)
            .WithNotParsed(HandleParseError);

        CleanitolEngine cleanitol = new CleanitolEngine(result.Value.UserPlugins, result.Value.SystemPlugins, result.Value.CleanitolOutput, result.Value.ScriptPath);
    }


    private static void HandleParseError(IEnumerable<Error> errs) {
        if (errs.IsVersion()) {
            Console.WriteLine("Version Request");
            return;
        }

        if (errs.IsHelp()) {
            Console.WriteLine("Help Request");
            return;
        }
        Console.WriteLine("Parser Fail");
    }

    private static void Run(Options opts) {
        Console.WriteLine("Parser success");
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
