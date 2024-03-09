using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Navigation;
using Microsoft.Win32;
using SC4Cleanitol;
using Options = SC4CleanitolWPF.Properties.Settings;
using System.Threading.Tasks;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Linq;

namespace SC4CleanitolWPF {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {    
        /// <summary>
        /// Whether to update the TGI index by rescanning the plugins folder.
        /// </summary>
        /// <remarks>
        /// Will result in a longer execution time while the index is created.
        /// </remarks>
        public bool UpdateTGIdb { get; set; } = true;
        /// <summary>
        /// Whether to show all output to the screen or just actionable outputs.
        /// </summary>
        public bool DetailedOutput { get; set; } = false;

        internal readonly Version releaseVersion = new Version(0, 7);
        internal readonly string releaseDate = "Mar 2024"; 
        private readonly Paragraph log;
        private readonly FlowDocument doc;
        private CleanitolEngine cleanitol;

        /// <summary>
        /// Initialize the MainWindow Window.
        /// </summary>
        public MainWindow() {
            doc = new FlowDocument();
            log = new Paragraph();
            //Doc.PageWidth = 1900; //hacky way to disable text wrapping because RichTextBox *always* wraps

            InitializeComponent();
            UpdateTGICheckbox.DataContext = this;
            VerboseOutputCheckbox.DataContext = this;
            StatusBar.Visibility = Visibility.Collapsed;
            BackupFiles.IsEnabled = false;
            RunScript.IsEnabled = false;

            //Set Properties
            if (Options.Default.UserPluginsDirectory.Equals("")) {
                Options.Default.UserPluginsDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SimCity 4\\Plugins");
            }

            if (Options.Default.SystemPluginsDirectory.Equals("")) {
                string steamDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam\\steamapps\\common\\SimCity 4 Deluxe\\Plugins");
                if (Directory.Exists(steamDir)) {
                    Options.Default.SystemPluginsDirectory = steamDir;
                } else {
                    Options.Default.SystemPluginsDirectory = Options.Default.UserPluginsDirectory;
                }
            }

            if (Options.Default.BaseOutputDirectory.Equals("")) {
                Options.Default.BaseOutputDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SimCity 4\\BSC_Cleanitol");
            }

            Options.Default.Save();

            cleanitol = new CleanitolEngine(Options.Default.UserPluginsDirectory, Options.Default.SystemPluginsDirectory, Options.Default.BaseOutputDirectory, string.Empty);
            this.Title = "SC4 Cleanitol 2023 - " + releaseVersion.ToString();

        }


        /// <summary>
        /// Opens a file dialog to choose the Cleanitol script.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChooseScript_Click(object sender, RoutedEventArgs e) {
            log.Inlines.Clear();
            OpenFileDialog dialog = new OpenFileDialog();
            if (dialog.ShowDialog() == true) {
                ScriptPathTextBox.Text = dialog.FileName;
                RunScript.IsEnabled = true;
            } else {
                return;
            }
        }


        /// <summary>
        /// Read and execute the script contents.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void RunScript_Click(object sender, RoutedEventArgs e) {
            log.Inlines.Clear();
            if (!Directory.Exists(Options.Default.UserPluginsDirectory)) {
                MessageBox.Show("User plugins directory not found. Verify the folder exists in your Documents folder and it is correctly set in Settings.", "User Plugins Not Found", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (!Directory.Exists(Options.Default.SystemPluginsDirectory)) {
                MessageBox.Show("System plugins directory not found. Verify the folder exists in the SC4 install folder and it is correctly set in Settings.", "System Plugins Not Found", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (!Directory.Exists(Options.Default.BaseOutputDirectory)) {
                MessageBox.Show("Cleanitol output directory not found. Verify the folder exists or set it in Settings.", "System Plugins Not Found", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            StatusLabel.Text = "Scanning Files ..."; //TODO - update the status bar file count even if not updating tgis
            if (UpdateTGIdb) {
                TGICountLabel.Visibility = Visibility.Visible;
                ExportTGIs.Visibility = Visibility.Visible;
                Separator1.Visibility = Visibility.Visible;
                Separator2.Visibility = Visibility.Visible;
            } else if (cleanitol is not null && cleanitol.ListOfTGIs.Count == 0) {
                TGICountLabel.Visibility = Visibility.Hidden;
                ExportTGIs.Visibility = Visibility.Hidden;
                Separator1.Visibility = Visibility.Hidden;
                Separator2.Visibility = Visibility.Hidden;
                
            } else {
                FileProgressBar.IsIndeterminate = true;
            }
            StatusBar.Visibility = Visibility.Visible;

            cleanitol.UserPluginsDirectory = Options.Default.UserPluginsDirectory;
            cleanitol.SystemPluginsDirectory = Options.Default.SystemPluginsDirectory;
            cleanitol.BaseOutputDirectory = Options.Default.BaseOutputDirectory;
            cleanitol.ScriptPath = ScriptPathTextBox.Text;
            
            var progressTotalFiles = new Progress<int>(totalFiles => { FileProgressBar.Maximum = totalFiles; });
            var progressScannedFiles = new Progress<int>(scannedFiles => { 
                FileProgressBar.Value = scannedFiles; 
                FileProgressLabel.Text = scannedFiles + " / " + FileProgressBar.Maximum + " files";
                if (scannedFiles == FileProgressBar.Maximum) {
                    StatusLabel.Text = "Creating Report ...";
                }
            });
            var progressTotalTGIs = new Progress<int>(totalTGIs => { TGICountLabel.Text = totalTGIs.ToString("N0") + " TGIs discovered"; });

            List<List<GenericRun>> runList = await Task.Run(() => cleanitol.RunScript(progressTotalFiles, progressScannedFiles, progressTotalTGIs, UpdateTGIdb, false, DetailedOutput));
            if (runList.Count == 0) {
                MessageBox.Show("Error Reading Files", "An error occurred while accessing files. It is possible one of the files is open in another program.", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
            }

            
            foreach (List<GenericRun> line in runList) {
                foreach (GenericRun run in line) {
                    if (run.Type is RunType.Hyperlink || run.Type is RunType.HyperlinkMono) {
                        try {
                            Hyperlink link = new Hyperlink(new Run(run.Text)) {
                                NavigateUri = new Uri(run.URL)
                            };
                            link.RequestNavigate += new RequestNavigateEventHandler(OnRequestNavigate);
                            log.Inlines.Add(link);
                        }
                        catch (Exception ex) {
                            using StreamWriter sw = new StreamWriter(cleanitol.LogPath, true);
                            sw.WriteLine("=============== Log Start ===============");
                            sw.WriteLine("Time: " + DateTime.Now);
                            sw.WriteLine("Script: " + cleanitol.ScriptPath);
                            sw.WriteLine("Link: " + run.Text);
                            sw.WriteLine("URI: " + run.URL);
                            sw.WriteLine($"Error: {ex.GetType()}: {ex.Message}");
                            sw.WriteLine("Trace: \r\n" + ex.StackTrace);
                            sw.WriteLine("================ Log End ================");
                        }
                    } else {
                        log.Inlines.Add(ConvertRun(run));
                    }
                }
            }

            doc.Blocks.Add(log);
            ScriptOutput.Document = doc;
            UpdateTGIdb = false;
            UpdateTGICheckbox.IsChecked = false;
            StatusLabel.Text = "Report Complete";
            FileProgressBar.IsIndeterminate = false;
            if (cleanitol.FilesToRemove.Count > 0) {
                BackupFiles.IsEnabled = true;
            }
            if (cleanitol.ListOfTGIs.Count == 0) {
                ExportTGIs.IsEnabled = false;
            } else {
                ExportTGIs.IsEnabled = true;
            }
        }


        /// <summary>
        /// Converts a <see cref="GenericRun"/> to a specific run style based on its <see cref="GenericRun.Type"/> property.
        /// </summary>
        /// <param name="genericRun">Run to convert</param>
        /// <returns></returns>
        private static Run ConvertRun(GenericRun genericRun) {
            switch (genericRun.Type) {
                case RunType.BlueStd:
                    return RunStyles.BlueStd(genericRun.Text);
                case RunType.BlueMono:
                    return RunStyles.BlueMono(genericRun.Text);
                case RunType.RedStd:
                    return RunStyles.RedStd(genericRun.Text);
                case RunType.RedMono:
                    return RunStyles.RedMono(genericRun.Text);
                case RunType.GreenStd:
                    return RunStyles.GreenStd(genericRun.Text);
                case RunType.BlackMono:
                    return RunStyles.BlackMono(genericRun.Text);
                case RunType.BlackStd:
                    return RunStyles.BlackStd(genericRun.Text);
                case RunType.BlackHeading:
                    return RunStyles.BlackHeading(genericRun.Text);
                case RunType.Hyperlink:
                case RunType.HyperlinkMono:
                default:
                    return new Run();
            }
        }



        /// <summary>
        /// Helper function to handle the hyperlink request.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void OnRequestNavigate(object sender, RequestNavigateEventArgs e) {
            var sinfo = new ProcessStartInfo(e.Uri.AbsoluteUri) {
                UseShellExecute = true
            };
            Process.Start(sinfo);
            e.Handled = true;
        }



        /// <summary>
        /// Move the files in the current instance of <see cref="CleanitolEngine.FilesToRemove"/> to an external folder and create <c>undo.bat</c> and <c>CleanupSummary.html</c> files.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BackupFiles_Click(object sender, RoutedEventArgs e) {
            cleanitol.BackupFiles(Properties.Resources.SummaryTemplate);
            log.Inlines.Add(ConvertRun(new GenericRun("\r\nRemoval Summary\r\n", RunType.BlackHeading)));

            Hyperlink link;
            if (cleanitol.FilesToRemove.Count > 0) {
                log.Inlines.Add(ConvertRun(new GenericRun($"{cleanitol.FilesToRemove.Count} files removed from plugins. Files moved to: ", RunType.BlackStd)));
                link = new Hyperlink(new Run(cleanitol.ScriptOutputDirectory + "\r\n")) {
                    NavigateUri = new Uri(cleanitol.ScriptOutputDirectory)
                };
                link.RequestNavigate += new RequestNavigateEventHandler(OnRequestNavigate);
                log.Inlines.Add(link);
            }
            link = new Hyperlink(ConvertRun(new GenericRun("View Summary",RunType.BlueMono))) {
                NavigateUri = new Uri(Path.Combine(cleanitol.ScriptOutputDirectory, "CleanupSummary.html"))
            };
            link.RequestNavigate += new RequestNavigateEventHandler(OnRequestNavigate);
            log.Inlines.Add(link);

            doc.Blocks.Add(log);
            BackupFiles.IsEnabled = false;
        }

        /// <summary>
        /// Create a new Cleanitol script in the Cleanitol output directory with all of the file names contained with a chosen folder and its subfolders.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CreateCleanitol_Click(object sender, RoutedEventArgs e) {
            string? folderpath;
            CommonOpenFileDialog dialog = new CommonOpenFileDialog {
                Title = "Choose Folder",
                InitialDirectory = cleanitol.UserPluginsDirectory,
                IsFolderPicker = true
            };
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok) {
                folderpath = dialog.FileName;
            } else {
                return;
            }

            string? scriptname;
            CommonSaveFileDialog dialog2 = new CommonSaveFileDialog {
                Title = "Save As",
                InitialDirectory = cleanitol.BaseOutputDirectory,
                
                AlwaysAppendDefaultExtension = true,
                DefaultExtension = ".txt",
                
            };
            dialog2.Filters.Add(new CommonFileDialogFilter("Text files", ".txt"));
            if (dialog2.ShowDialog() == CommonFileDialogResult.Ok) {
                scriptname = dialog2.FileName;
            } else {
                return;
            }

            if (folderpath is not null &&  scriptname is not null) {
                CleanitolEngine.CreateCleanitolList(folderpath, scriptname);
                MessageBox.Show("Script created successfully.", "", MessageBoxButton.OK);
            }


        }

        /// <summary>
        /// Show the settings window, and pre-populate it with the current settings.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Settings_Click(object sender, RoutedEventArgs e) {
            Preferences p = new Preferences();
            p.ShowDialog();
        }

        /// <summary>
        /// Export the list of TGIs to a CSV file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExportTGIs_Click(object sender, RoutedEventArgs e) {
            cleanitol.ExportTGIs();
            MessageBox.Show("Export Complete!", "Exporting TGIs", MessageBoxButton.OK, MessageBoxImage.Exclamation, MessageBoxResult.OK);
        }
    }
}
