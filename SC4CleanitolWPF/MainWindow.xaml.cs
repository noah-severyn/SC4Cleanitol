using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Navigation;
using Microsoft.Win32;
using SC4Cleanitol;
using Options = SC4CleanitolWPF.Properties.Settings;
using System.ComponentModel;

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

        private delegate void ProgressBarSetValueDelegate(DependencyProperty dp, object value);
        private delegate void TextBlockSetTextDelegate(DependencyProperty dp, object value);
        private readonly Version version = new Version(0, 1);

        private CleanitolEngine Cleanitol;
        private System.ComponentModel.BackgroundWorker backgroundWorker1;
        private List<SC4Cleanitol.GenericRun> runList;

        public MainWindow() {
            Doc = new FlowDocument();
            Log = new Paragraph();
            //Doc.PageWidth = 1900; //hacky way to disable text wrapping because RichTextBox *always* wraps

            InitializeComponent();
            InitializeBackgroundWorker();
            UpdateTGICheckbox.DataContext = this;
            VerboseOutputCheckbox.DataContext = this;
            StatusBar.Visibility = Visibility.Collapsed;

            //Set Properties
            if (!Options.Default.UserPluginsDirectory.Equals("")) {
                Options.Default.UserPluginsDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SimCity 4\\Plugins");
                Options.Default.Save();
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
                Options.Default.Save();
                _systemPluginsDir = Options.Default.SystemPluginsDirectory;
            } else {
                _systemPluginsDir = string.Empty;
            }
            _cleanitolOutputDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SimCity 4\\Plugins\\BSC_Cleanitol");

            Cleanitol = new CleanitolEngine(_userPluginsDir, _systemPluginsDir, _cleanitolOutputDir);
            this.Title = "SC4 Cleanitol 2023 - " + version.ToString();

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
                Cleanitol.ScriptPath = dialog.FileName;
                ScriptPathTextBox.Text = dialog.FileName;
            } else {
                return;
            }
        }


        /// <summary>
        /// Read and execute the script contents.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RunScript_Click(object sender, RoutedEventArgs e) {
            Log.Inlines.Clear();
            if (!Directory.Exists(Cleanitol.UserPluginsDirectory)) {
                MessageBox.Show("User plugins directory not found. Verify the folder exists in your Documents folder and it is correctly set in Settings.", "User Plugins Not Found", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (!Directory.Exists(Cleanitol.SystemPluginsDirectory)) {
                MessageBox.Show("System plugins directory not found. Verify the folder exists in the SC4 install folder and it is correctly set in Settings.", "System Plugins Not Found", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Start the asynchronous operation.
            backgroundWorker1.RunWorkerAsync();

            backgroundWorker1.CancelAsync();

            foreach (GenericRun run in runList) {
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
            Doc.Blocks.Add(Log);
            ScriptOutput.Document = Doc;
            UpdateTGIdb = false;
            UpdateTGICheckbox.IsChecked = false;
        }




        //https://learn.microsoft.com/en-us/previous-versions/visualstudio/visual-studio-2010/waw3xexc(v=vs.100)?redirectedfrom=MSDN
        // Set up the BackgroundWorker object by attaching event handlers. 
        private void InitializeBackgroundWorker() {
            backgroundWorker1.DoWork += new DoWorkEventHandler(backgroundWorker1_DoWork);
            backgroundWorker1.RunWorkerCompleted += new RunWorkerCompletedEventHandler(backgroundWorker1_RunWorkerCompleted);
            backgroundWorker1.ProgressChanged += new ProgressChangedEventHandler(backgroundWorker1_ProgressChanged);
        }

        // This event handler is where the actual, potentially time-consuming work is done.
        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e) {
            BackgroundWorker worker = sender as BackgroundWorker;// Get the BackgroundWorker that raised this event.

            // Assign the result of the computation to the Result property of the DoWorkEventArgs object. This is will be available to the RunWorkerCompleted eventhandler.
            e.Result = Cleanitol.RunScript(UpdateTGIdb, _includeSystemPlugins, worker, e);
        }

        // This event handler deals with the results of the background operation.
        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            if (e.Error != null) { // Handle the case where an exception was thrown.
                MessageBox.Show(e.Error.Message);
            } else if (e.Cancelled) { // Handle the case where the user canceled the operation.
                // Note that due to a race condition in the DoWork event handler, the Cancelled flag may not have been set, even though CancelAsync was called.
                StatusLabel.Text = "Scan canceled";
            } else { // Handle the case where the operation succeeded.
                if (e.Result is not null) {
                    runList = (List<GenericRun>) e.Result;
                }
                StatusLabel.Text = "Scan complete";
            }
        }

        // This event handler updates the progress bar.
        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e) {
            int progressPct = e.ProgressPercentage;
            int fileCount = Cleanitol.ListOfFiles.Count();
            FileProgressBar.Value = progressPct;
            FileProgressLabel.Text = (progressPct * fileCount) + " / " + fileCount + " files";
        }






        internal static Run ConvertRun(GenericRun genericRun) {
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
        /// Move the files in <see cref="_filesToRemove"/> to an external folder and create <c>undo.bat</c> and <c>CleanupSummary.html</c> files.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BackupFiles_Click(object sender, RoutedEventArgs e) {
            Cleanitol.BackupFiles();
        }

        private void Quit_Click(object sender, RoutedEventArgs e) {
            Application.Current.Shutdown();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e) {
            StatusBar.Visibility = Visibility.Collapsed;
        }

        private void Settings_Click(object sender, RoutedEventArgs e) {
            Preferences p = new Preferences();
            p.Show();
            _userPluginsDir = Options.Default.UserPluginsDirectory;
            _systemPluginsDir = Options.Default.SystemPluginsDirectory;
            _includeSystemPlugins = Options.Default.ScanSystemPlugins;
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e) {
            Cleanitol.ExportTGIs();
            MessageBox.Show("Export Complete!", "Exporting TGIs", MessageBoxButton.OK, MessageBoxImage.Exclamation, MessageBoxResult.OK);
        }
    }
}
