using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MsBox.Avalonia.Enums;
using SC4CleanitolAvalonia.Models;
using SC4CleanitolAvalonia.Services;
using SC4CleanitolEngine;
using static SC4CleanitolEngine.CleanitolEngine;

namespace SC4CleanitolAvalonia.ViewModels;

internal partial class MainViewModel(ICleanitolService cleanitolService, IFolderPickerService folderPickerService, IDialogService dialogService, IConfigService configService, UserConfig config, IChecksumService checksumService) : ObservableObject {

    /// <summary>
    /// Parameterless constructor used only by the XAML previewer (<c>Design.DataContext</c>). Services are null, so commands must not be executed.
    /// </summary>
    public MainViewModel() : this(null!, null!, null!, null!, new UserConfig(), null!) {
        LastScanned = "Last scan: " + DateTime.Now.ToString();

        Results.Add(new ScriptResult("nam-team:network-addon-mod", [
            new Finding { Severity = FindingSeverity.Warning, Script = "nam-team:network-addon-mod", Item = "NetworkAddonMod_Locale_pt.dat", Message = "was found in C:\\Plugins\\075-my-plugins\\NAM\\@4^1 Core\\locale" },
            new Finding { Severity = FindingSeverity.Warning, Script = "nam-team:network-addon-mod", Item = "NetworkAddonMod_Locale_de.dat", Message = "was found in C:\\Plugins\\075-my-plugins\\NAM\\@4^1 Core\\locale" },
            new Finding { Severity = FindingSeverity.Info, Script = "nam-team:network-addon-mod", Item = "NetworkAddonMod_Controller.dat", Message = "is not present." },
        ]));
        Results.Add(new ScriptResult("b62:safeway-60s-retro-grocery", [
            new Finding { Severity = FindingSeverity.Warning, Script = "b62:safeway-60s-retro-grocery", Item = "B62-CS$$_Safeway Retro 1960s v 2.0-0x6534284a-0xd3a3e650-0x5639d97a.SC4Desc", Message = "was found in C:\\Plugins\\B62-Safeway 60's Retro" },
            new Finding { Severity = FindingSeverity.Warning, Script = "b62:safeway-60s-retro-grocery", Item = "CS$$2_6x6_B62-CS$$_Safeway Retro 1960s v 2.0_8639d990.SC4Lot", Message = "was found in C:\\Plugins\\B62-Safeway 60's Retro" },
            new Finding { Severity = FindingSeverity.Warning, Script = "b62:safeway-60s-retro-grocery", Item = "b62 1960s Safeway Retro v 2.0-0x5ad0e817_0xb2a42fa9_0x30000.SC4Model", Message = "was found in C:\\Plugins\\B62-Safeway 60's Retro\\B62-Safeway 60's Retro" },
            //new LogItem { Level = LogLevel.Error, Script = "b62:safeway-60s-retro-grocery", Item = "Missing dependency", Message = "is missing. Download at: #url#", Link = new Link { Name = "SC4Evermore", Path = "https://www.sc4evermore.com" } },
        ]));
    }

    [ObservableProperty]
    private string _userPluginsPath = config.UserPluginsPath;
    partial void OnUserPluginsPathChanged(string value) {
        config.UserPluginsPath = value;
    }

    [ObservableProperty]
    private string _systemPluginsPath = config.SystemPluginsPath;
    partial void OnSystemPluginsPathChanged(string value) {
        config.SystemPluginsPath = value;
    }

    [ObservableProperty]
    private bool _includeSystemPlugins = config.IncludeSystemPlugins;
    partial void OnIncludeSystemPluginsChanged(bool value) {
        config.IncludeSystemPlugins = value;
    }

    [ObservableProperty]
    private string _outputPath = config.OutputPath;
    partial void OnOutputPathChanged(string value) {
        config.OutputPath = value;
    }

    [ObservableProperty]
    private bool _checkSc4pacDuplicates = config.CheckSc4pacDuplicates;
    partial void OnCheckSc4pacDuplicatesChanged(bool value) {
        config.CheckSc4pacDuplicates = value;
    }

    [ObservableProperty]
    private string _additionalFoldersText = string.Join(Environment.NewLine, config.AdditionalFolders);
    partial void OnAdditionalFoldersTextChanged(string value) {
        config.AdditionalFolders = value
            .Split(new[] { Environment.NewLine, "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(f => f.Trim())
            .Where(f => !string.IsNullOrWhiteSpace(f))
            .ToList();
    }


    /// <summary>
    /// Gets or sets the total number of files processed during the scan.
    /// </summary>
    /// <remarks>Changes to this property will also update <see cref="ScanProgress"/></remarks>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ScanProgress))]
    private int _filesTotal;
    partial void OnFilesTotalChanged(int value) {
        OnPropertyChanged(nameof(ScanProgress));
    }

    /// <summary>
    /// Gets or sets the number of files that have been scanned.
    /// </summary>
    /// <remarks>Changes to this property will also update <see cref="ScanProgress"/></remarks>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ScanProgress))]
    private int _filesProcessed;
    partial void OnFilesProcessedChanged(int value) {
        OnPropertyChanged(nameof(ScanProgress));
    }



    [ObservableProperty]
    private string _lastScanned = "Last scan: " + config.LastScanned.ToString();
    partial void OnLastScannedChanged(string value) {
        config.LastScanned = DateTime.Now;
    }

    [ObservableProperty]
    private string? _checksum = null;
    partial void OnChecksumChanged(string? value) {
        config.PluginsChecksum = Checksum;
    }

    [ObservableProperty]
    private ObservableCollection<ScriptResult> _results = [];

    public string ScanProgress {
        get {
            if (FilesTotal == 0) {
                return string.Empty;
            } else if (FilesProcessed == FilesTotal) {
                return $"Scanned {FilesProcessed} / {FilesTotal} files";
            } else {
                return $"Scanning {FilesProcessed} / {FilesTotal} files";
            }
        }
    }

    public string ChangesDetected => (Checksum != config.PluginsChecksum ? "Changes detected since last scan." : "No changes since last scan.");

    public int TgisProcessed { get; private set; }

    public ScanResult? ScanResults { get; private set; }

    public List<RunResult>? RunResults { get; private set; }

    [RelayCommand]
    private async Task ChooseUserPluginsFolder() {
        var folder = await folderPickerService.PickFolderAsync();
        if (folder != null) {
            UserPluginsPath = folder.Path.LocalPath;
        }
    }

    [RelayCommand]
    private async Task ChooseSystemPluginsFolder() {
        var folder = await folderPickerService.PickFolderAsync();
        if (folder != null) {
            SystemPluginsPath = folder.Path.LocalPath;
        }
    }

    [RelayCommand]
    private async Task ChooseOutputFolder() {
        var folder = await folderPickerService.PickFolderAsync();
        if (folder != null) {
            OutputPath = folder.Path.LocalPath;
        }
    }


    private List<string> ValidateFolderPaths() {
        List<string> invalidFolders = [];
        if (!Directory.Exists(config.UserPluginsPath)) {
            invalidFolders.Add(config.UserPluginsPath);
        }
        if (!Directory.Exists(config.SystemPluginsPath)) {
            invalidFolders.Add(config.SystemPluginsPath);
        }
        if (!Directory.Exists(config.OutputPath)) {
            invalidFolders.Add(config.OutputPath);
        }
        foreach (var folder in config.AdditionalFolders) {
            if (!Directory.Exists(folder)) {
                invalidFolders.Add(folder);
            }
            
        }
        return invalidFolders;
    }

    private bool AllPathsValid() {
        return ValidateFolderPaths().Count == 0;
    }




    //[RelayCommand(CanExecute = nameof(AllPathsValid))]
    [RelayCommand]
    private async Task ScanPlugins() {
        var invalidFolders = ValidateFolderPaths();
        if (invalidFolders.Count > 0) {
            var message = "The following folders were not found. Do you want to create them?\n\n" + string.Join("\n", invalidFolders);
            var result = await dialogService.ShowMessageBoxAsync("Invalid Folder Paths", message, Icon.Error);
            if (result == ButtonResult.Yes) {
                foreach (var folder in invalidFolders) {
                    Directory.CreateDirectory(folder);
                }
            }
        }

        await configService.SaveAsync(config);

        LastScanned = "Last scan: " + DateTime.Now.ToString();


        List<string> foldersToScan = [config.UserPluginsPath];
        if (IncludeSystemPlugins) {
            foldersToScan.Add(config.SystemPluginsPath);
        }
        foldersToScan.AddRange(config.AdditionalFolders);

        cleanitolService.Configure(foldersToScan, OutputPath);
        var progress = new Progress<CleanitolProgress>(p => {
            FilesProcessed = p.FilesProcessed;
            FilesTotal = p.FilesTotal;
            TgisProcessed = p.TgisProcessed;
        });

        ScanResults = await cleanitolService.ScanAsync(progress);

        if (config.CheckSc4pacDuplicates) {
            await CheckForSc4pacDuplicates(foldersToScan);
        }
    }

    /// <summary>
    /// Since the Cleanitol Engine can now execute a collection of rules as if they were a script, treat the files in an sc4pac package folder as a Cleanitol script where every file contained inside should be targeted for removal if found elsewhere.
    /// </summary>
    async private Task CheckForSc4pacDuplicates(List<string> foldersToScan) {
        Results.Clear();

        //We first want to know what sc4pac packages are already installed to find any duplicates. Treat each package folder as a cleanitol script with its contents as the rules.
        var pkgFolders = new HashSet<string>();
        foreach (var folder in foldersToScan) {
            foreach (var dir in Directory.EnumerateDirectories(folder, "*", new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = true })) {
                if (dir.EndsWith(".sc4pac")) {
                    pkgFolders.Add(dir);
                }
            }
        }
        foreach (var folder in pkgFolders) {
            var filesToFind = ScanResults.ScannedFiles.Where(k => k.Key.StartsWith(folder)).Select(k => k.Value);
            if (!filesToFind.Any()) {
                continue;
            }
            var pkg = ExtractPackageIdFromFolder(folder);
            var result = await cleanitolService.RunAsync(pkg, filesToFind);
            Results.Add(new ScriptResult(pkg, result.Findings.ToList()));
        }
    }

    /// <summary>
    /// Extract the sc4pac package identifier from a file path.
    /// </summary>
    /// <param name="path">File path to parse</param>
    /// <returns>A string representing an sc4pac package identifier in the format <c>group:name</c>.</returns>
    private static string ExtractPackageIdFromFolder(string path) {
        //Example paths are:
        //"C:\Users\Administrator\Documents\SimCity 4\Plugins\060-config\config.sc4-edition.Windows-digital.1.sc4pac"
        //"C:\Users\Administrator\Documents\SimCity 4\Plugins\300-commercial\b62.safeway-60s-retro-grocery.1.sc4pac\B62-Safeway 60's Retro\b62 safeway_sign_70s v 2.0-0x6534284a-0x50ec22ad-0xf2a67ca6.SC4Desc"
        string? pkgInfo = path.Split('\\').FirstOrDefault(f => f.Contains(".sc4pac"));
        var pkgParts = pkgInfo?.Split('.').Take(2).ToArray(); //keep only the group and name parts
        return pkgParts?[0] + ":" + pkgParts?[1];
    }



    public partial class ScriptResult(string scriptName, List<Finding> findings) {
        /// <summary>
        /// Name of script run - either the Cleanitol file name or a sc4pac package.
        /// </summary>
        public string ScriptName { get; } = scriptName;
        /// <summary>
        /// Number of findings at warning level or higher.
        /// </summary>
        public int ItemCount { get; } = findings.Count(i => i.Severity >= FindingSeverity.Warning);
        public string ItemCountLabel => $"({ItemCount} files)";
        /// <summary>
        /// Items that don't have any results are not shown in the window.
        /// </summary>
        public bool HasItems => ItemCount > 0;
        /// <summary>
        /// Gets a value indicating whether the script name is in sc4pac package format of <c>group:name</c>.
        /// </summary>
        public bool IsSc4pacPackage {
            get {
                return Regex.IsMatch(ScriptName, @"^[a-z\-]*:[a-z\-]*$");
            }
        }
        public List<LogItemDisplay> DisplayLog {
            get {
                List<LogItemDisplay> log = [];
                foreach (var item in findings) {
                    log.Add(new LogItemDisplay(item));
                }
                return log;
            }
        }

        /// <summary>
        /// Opens the current package in sc4pac using a package URI built from the URL-encoded script name.
        /// </summary>
        [RelayCommand]
        public void OpenPackageInSc4pac() {
            //A package link looks like: sc4pac:///package?pkg=b62%3Asafeway-60s-retro-grocery
            string uri = "sc4pac:///package?pkgInfo=" + HttpUtility.UrlEncode(ScriptName);
            try {
                OpenUrl(uri);
            }
            catch (Exception) {
                throw;
            }
        }

        [RelayCommand]
        public void CleanUpFiles() {

        }

        /// <summary>
        /// A wrapper for a <see cref="Finding"/> containing all the properties needed for display.
        /// /// </summary>
        /// <param name="item"></param>
        public partial class LogItemDisplay(Finding item) {
            public FindingSeverity Level { get; private set; } = item.Severity;
            public string Icon {
                get {
                    return item.Severity switch {
                        FindingSeverity.Output => "\ue06c",
                        FindingSeverity.Info => "\ue2ce",
                        FindingSeverity.Warning => "\uee44",
                        FindingSeverity.Error => "\ue7fc",
                        _ => "\ue31e",
                    };
                }
            }
            public IImmutableSolidColorBrush IconColor {
                get {
                    return item.Severity switch {
                        FindingSeverity.Output => Brushes.Green,
                        FindingSeverity.Info => Brushes.SteelBlue,
                        FindingSeverity.Warning => Brushes.Orange,
                        FindingSeverity.Error => Brushes.Crimson,
                        _ => Brushes.Black,
                    };
                }
            }
            public string Item { get; private set; } = Path.GetFileName(item.Item);
            public string Message { get; private set; } = item.Message;
            //public Exception Error { get; private set; } = item.Error;
            public Link Link { get; private set; } = item.Link;
        }
    }


    private static void OpenUrl(string url) {
        Process.Start(new ProcessStartInfo {
            FileName = url,
            UseShellExecute = true
        });
    }

}
