using System.Text;

// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");

string filePath = args.Last();
//accept 5 params: 2 required remaining optional
//user plugins dir, cleanitol file path, system plugins dir (scan system directory - if system plugins param omitted this is false), verbose output, update tgi database
string userPluginsDir = args[0];
string scriptPath = args[1];
string systemPluginsDir;
bool scanSystemPlugins = false;
bool updateTGIDB = false;
bool verbose = false;
try {
	systemPluginsDir = args[2];
	if (systemPluginsDir != string.Empty) {
		scanSystemPlugins = true;
	}
	bool.TryParse(args[3], out updateTGIDB);
	bool.TryParse(args[4], out verbose);
}
catch (Exception) {
	systemPluginsDir = string.Empty;
}


Console.WriteLine("userPluginsDir: " + userPluginsDir);
Console.WriteLine("scriptPath: " + scriptPath);
Console.WriteLine("systemPluginsDir: " + systemPluginsDir);
Console.WriteLine("scanSystemPlugins: " + scanSystemPlugins);
Console.WriteLine("updateTGIDB: " + updateTGIDB);
Console.WriteLine("verbose: " + verbose);