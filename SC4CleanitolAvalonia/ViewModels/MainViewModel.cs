using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MsBox.Avalonia.Enums;
using SC4CleanitolAvalonia.Models;
using SC4CleanitolAvalonia.Services;
using SC4CleanitolEngine;
using SC4Cleanitol;

namespace SC4CleanitolAvalonia.ViewModels;

internal partial class MainViewModel(ICleanitolService cleanitolService, IFolderPickerService folderPickerService, IDialogService dialogService, IConfigService configService, UserConfig config, IChecksumService checksumService) : ObservableObject {

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

    //[ObservableProperty]
    //private ObservableCollection<PackageGroup> _results = [];

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
            var message = "The following folders were not found. Validate they were typed correctly and exist:\n\n" + string.Join("\n", invalidFolders);
            await dialogService.ShowMessageBoxAsync("Invalid Folder Paths", message, Icon.Error);
            return;
        }

        await configService.SaveAsync(config);

        LastScanned = "Last scan: " + DateTime.Now.ToString();


        List<string> foldersToScan = [config.UserPluginsPath];
        if (IncludeSystemPlugins) {
            foldersToScan.Add(config.SystemPluginsPath);
        }
        foldersToScan.AddRange(config.AdditionalFolders);

        cleanitolService.Configure(foldersToScan, OutputPath);
        var progress = new Progress<CleanitolEngine.CleanitolProgress>(p => {
            FilesProcessed = p.FilesProcessed;
            FilesTotal = p.FilesTotal;
            TgisProcessed = p.TgisProcessed;
        });

        await cleanitolService.ScanAsync(progress, false);

        if (config.CheckSc4pacDuplicates) {
            await CheckForSc4pacDuplicates(foldersToScan);
        }
        //Results.Clear();
        //var results = await cleanitolService.ScanAsync(config.UserPluginsPath, config.SystemPluginsPath, config.IncludeSystemPlugins, config.AdditionalFolders, progress);

        //var grouped = results.GroupBy(i => i.Package);
        //foreach (var g in grouped) {
        //    var filesList = g.Select(scanresult => scanresult.Path).ToList();
        //    for (int idx = 0; idx < filesList.Count; idx++) {
        //        string file = filesList[idx].Replace(UserPluginsPath, string.Empty);
        //        if (!file.StartsWith('\\')) file = "\\" + file;
        //        filesList[idx] = file;
        //    }
        //    Results.Add(new PackageGroup(this, g.Key, filesList));
        //}
    }

    /// <summary>
    /// Since the CleanitolEngine was rewritten to allow a collection of rules to be run without a script file, we can treat the files in an sc4pac folder as a cleantiol script, where every file contained inside should be targeted for removal if found elsewhere.
    /// </summary>
    async private Task CheckForSc4pacDuplicates(List<string> foldersToScan) {
        List<string> packageFolders = [];
        foreach (var folder in foldersToScan) {
            packageFolders.AddRange(Directory.EnumerateDirectories(folder, "*", SearchOption.AllDirectories).Where(f => f.EndsWith(".sc4pac")));
        }
        foreach (var folder in packageFolders) {
            var files = cleanitolService.Results.ScannedFiles.Keys.Where(k => k.StartsWith(folder));
            if (!files.Any()) {
                continue;
            }
            var pkg = ExtractSc4pacPackage(folder);
            await cleanitolService.RunAsync(pkg, files, true, false);
        }
    }

    /// <summary>
    /// Extract the sc4pac package identifier from a file path.
    /// </summary>
    /// <param name="path">File path to parse</param>
    /// <returns>A string representing an sc4pac package identifier in the format <c>group:name</c>.</returns>
    private static string ExtractSc4pacPackage(string path) {
        //Example paths are:
        //"C:\Users\Administrator\Documents\SimCity 4\Plugins\100-props-textures\simmer2.mega-prop-pack-vol1.1.0.sc4pac\SM2 Mega Prop Pack Vol1.dat"
        //"C:\Users\Administrator\Documents\SimCity 4\Plugins\300-commercial\b62.safeway-60s-retro-grocery.1.sc4pac\B62-Safeway 60's Retro\b62 safeway_sign_70s v 2.0-0x6534284a-0x50ec22ad-0xf2a67ca6.SC4Desc"
        string? pkg = path.Split('\\').FirstOrDefault(f => f.Contains(".sc4pac"));
        var pkgParts = pkg?.Split('.').SkipLast(2).ToArray(); //remove the version and sc4pac suffex
        return pkgParts?[0] + ":" + pkgParts?[1];
    }

    //public partial class PackageGroup(MainViewModel viewModel, string package, List<string> files) {
    //    private readonly MainViewModel _mvm = viewModel;

    //    public string Package { get; } = package;
    //    public List<string> Files { get; } = files;

    //    [RelayCommand]
    //    public void OpenPackageInSc4pac() {
    //        //A package link looks like: sc4pac:///package?pkg=b62%3Asafeway-60s-retro-grocery
    //        string uri = "sc4pac:///package?pkg=" + HttpUtility.UrlEncode(Package);
    //        try {
    //            OpenUrl(uri);
    //        }
    //        catch (Exception) {
    //            throw;
    //        }
    //    }

    //    /// <summary>
    //    /// Find the lowest level folder common among all the files and open it.
    //    /// </summary>
    //    [RelayCommand]
    //    public void OpenParentFolder() {
    //        var min = Files.MinBy(p => p.Length);
    //        var minFldrs = min.Split(Path.DirectorySeparatorChar);
    //        foreach (var path in Files) {
    //            var fldrs = path.Split(Path.DirectorySeparatorChar);
    //            if (fldrs.Length < minFldrs.Length) {
    //                minFldrs = fldrs;
    //            }
    //        }

    //        OpenUrl(Path.Combine(_mvm.UserPluginsPath, Path.Combine(minFldrs.SkipLast(1).ToArray())));
    //    }
    //}


    private static void OpenUrl(string url) {
        Process.Start(new ProcessStartInfo {
            FileName = url,
            UseShellExecute = true
        });
    }

}
