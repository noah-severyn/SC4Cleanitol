using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Navigation;
using Microsoft.Win32;
using SC4Cleanitol;
using Options = SC4CleanitolWPF.Properties.Settings;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace SC4CleanitolWPF {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        private string _userPluginsDir;
        private string _systemPluginsDir;
        private string _cleanitolOutputDir;
        private bool _includeSystemPlugins;

        private readonly Paragraph Log;
        private readonly FlowDocument Doc;
        public bool UpdateTGIdb { get; set; } = true;
        public bool VerboseOutput { get; set; } = false;

        public readonly Version ReleaseVersion = new Version(0, 2);
        public readonly string ReleaseDate = "Jun 2023";
        private CleanitolEngine cleanitol;

        public MainWindow() {
            Doc = new FlowDocument();
            Log = new Paragraph();
            //Doc.PageWidth = 1900; //hacky way to disable text wrapping because RichTextBox *always* wraps

            InitializeComponent();
            //InitializeBackgroundWorker();
            UpdateTGICheckbox.DataContext = this;
            VerboseOutputCheckbox.DataContext = this;
            StatusBar.Visibility = Visibility.Collapsed;
            BackupFiles.IsEnabled = false;
            RunScript.IsEnabled = false;

            //Set Properties
            if (!Options.Default.UserPluginsDirectory.Equals("")) {
                Options.Default.UserPluginsDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SimCity 4\\Plugins");
                _userPluginsDir = Options.Default.UserPluginsDirectory;
            } else {
                _userPluginsDir = string.Empty;
            }

            if (!Options.Default.SystemPluginsDirectory.Equals("")) {
                string steamDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam\\steamapps\\common\\SimCity 4 Deluxe\\Plugins");
                if (Directory.Exists(steamDir)) {
                    Options.Default.SystemPluginsDirectory = steamDir;
                } else {
                    Options.Default.SystemPluginsDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SimCity 4\\Plugins");
                }
                _systemPluginsDir = Options.Default.SystemPluginsDirectory;
            } else {
                _systemPluginsDir = string.Empty;
            }

            if (Options.Default.CleanitolOutputDirectory.Equals("")) {

            }

            Options.Default.Save();
            _cleanitolOutputDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SimCity 4\\BSC_Cleanitol");

            cleanitol = new CleanitolEngine(_userPluginsDir, _systemPluginsDir, _cleanitolOutputDir);
            this.Title = "SC4 Cleanitol 2023 - " + ReleaseVersion.ToString();

        }


        /// <summary>
        /// Opens a file dialog to choose the cleanitol script.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChooseScript_Click(object sender, RoutedEventArgs e) {
            Log.Inlines.Clear();
            OpenFileDialog dialog = new OpenFileDialog();
            if (dialog.ShowDialog() == true) {
                cleanitol.SetScriptPath(dialog.FileName);
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
            Log.Inlines.Clear();
            if (!Directory.Exists(cleanitol.UserPluginsDirectory)) {
                MessageBox.Show("User plugins directory not found. Verify the folder exists in your Documents folder and it is correctly set in Settings.", "User Plugins Not Found", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (!Directory.Exists(cleanitol.SystemPluginsDirectory)) {
                MessageBox.Show("System plugins directory not found. Verify the folder exists in the SC4 install folder and it is correctly set in Settings.", "System Plugins Not Found", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (UpdateTGIdb) {
                StatusBar.Visibility = Visibility.Visible;
            }
            

            var progressTotalFiles = new Progress<int>(totalFiles => { FileProgressBar.Maximum = totalFiles; });
            var progresScannedFiles = new Progress<int>(scannedFiles => { 
                FileProgressBar.Value = scannedFiles; 
                FileProgressLabel.Text = scannedFiles + " / " + FileProgressBar.Maximum + " files";
                if (scannedFiles == FileProgressBar.Maximum) {
                    StatusLabel.Text = "Creating Report ...";
                }
            });
            var progressTotalTGIs = new Progress<int>(totalTGIs => { TGICountLabel.Text = totalTGIs.ToString("N0") + " TGIs discovered"; });

            List<List<GenericRun>> runList = await Task.Run(() => cleanitol.RunScript(progressTotalFiles, progresScannedFiles, progressTotalTGIs, UpdateTGIdb, false, VerboseOutput));
            
            foreach (List<GenericRun> line in runList) {
                foreach (GenericRun run in line) {
                    if (run.Type is RunType.Hyperlink) {
                        Hyperlink link = new Hyperlink(new Run(run.Text + "\r\n"));
                        link.NavigateUri = new Uri(run.URL);
                        link.RequestNavigate += new RequestNavigateEventHandler(OnRequestNavigate);
                        Log.Inlines.Add(link);
                        Log.Inlines.Add(new Run("\r\n"));
                    } else {
                        Log.Inlines.Add(ConvertRun(run));
                    }
                }
            }

            Doc.Blocks.Add(Log);
            ScriptOutput.Document = Doc;
            UpdateTGIdb = false;
            UpdateTGICheckbox.IsChecked = false;
            StatusLabel.Text = "Scan Complete";
            if (cleanitol.FilesToRemove.Count > 0) {
                BackupFiles.IsEnabled = true;
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
            var sinfo = new ProcessStartInfo(e.Uri.AbsoluteUri);
            sinfo.UseShellExecute = true;
            Process.Start(sinfo);
            e.Handled = true;
        }



        /// <summary>
        /// Move the files in the current instance of <see cref="CleanitolEngine.FilesToRemove"/> to an external folder and create <c>undo.bat</c> and <c>CleanupSummary.html</c> files.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BackupFiles_Click(object sender, RoutedEventArgs e) {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "SC4CleanitolWPF.SummaryTemplate.html";
            Stream? stream = assembly.GetManifestResourceStream(resourceName);
            cleanitol.BackupFiles(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),"SummaryTemplate.html"));
            Log.Inlines.Add(ConvertRun(new GenericRun("\r\nRemoval Summary\r\n", RunType.BlackHeading)));

            Hyperlink link;
            if (cleanitol.FilesToRemove.Count > 0) {
                Log.Inlines.Add(ConvertRun(new GenericRun($"{cleanitol.FilesToRemove.Count} files removed from plugins. Files moved to: ", RunType.BlackStd)));
                link = new Hyperlink(new Run(cleanitol.CleanitolOutputDirectory + "\r\n")) {
                    NavigateUri = new Uri(cleanitol.CleanitolOutputDirectory)
                };
                link.RequestNavigate += new RequestNavigateEventHandler(OnRequestNavigate);
                Log.Inlines.Add(link);
            }
            link = new Hyperlink(ConvertRun(new GenericRun("View Summary",RunType.BlueMono))) {
                NavigateUri = new Uri(Path.Combine(cleanitol.CleanitolOutputDirectory, "CleanupSummary.html"))
            };
            link.RequestNavigate += new RequestNavigateEventHandler(OnRequestNavigate);
            Log.Inlines.Add(link);

            Doc.Blocks.Add(Log);
            BackupFiles.IsEnabled = false;
        }
        /// <summary>
        /// Exit the application
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Quit_Click(object sender, RoutedEventArgs e) {
            Application.Current.Shutdown();
        }
        /// <summary>
        /// Hide the status bar.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OkButton_Click(object sender, RoutedEventArgs e) {
            StatusBar.Visibility = Visibility.Collapsed;
        }
        /// <summary>
        /// Show the settings window, and pre-populate it with the current settings.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Settings_Click(object sender, RoutedEventArgs e) {
            Preferences p = new Preferences();
            p.Show();
            _userPluginsDir = Options.Default.UserPluginsDirectory;
            _systemPluginsDir = Options.Default.SystemPluginsDirectory;
            _includeSystemPlugins = Options.Default.ScanSystemPlugins;
        }
        /// <summary>
        /// Eport the list of TGIs to a CSV file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExportButton_Click(object sender, RoutedEventArgs e) {
            cleanitol.ExportTGIs();
            MessageBox.Show("Export Complete!", "Exporting TGIs", MessageBoxButton.OK, MessageBoxImage.Exclamation, MessageBoxResult.OK);
        }
    }
}
