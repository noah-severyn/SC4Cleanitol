using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Microsoft.Win32;
using csDBPF;
using Options = SC4CleanitolWPF.Properties.Settings;

namespace SC4CleanitolWPF {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        private string _scriptPath;
        private string[] _scriptRules;
        private int _countDepsScanned;
        private int _countDepsFound;
        private int _countDepsMissing;
        private IEnumerable<string> _listOfFiles;
        private IEnumerable<string> _listOfFileNames;
        private List<string> _listOfTGIs;
        private List<string> _filesToRemove;

        private readonly Paragraph Log;
        private readonly FlowDocument Doc;
        public bool UpdateTGIdb { get; set; } = true;
        public bool VerboseOutput { get; set; } = false;

        private delegate void ProgressBarSetValueDelegate(DependencyProperty dp, object value);
        private delegate void TextBlockSetTextDelegate(DependencyProperty dp, object value);


        public MainWindow() {
            _filesToRemove = new List<string>();
            _listOfTGIs = new List<string>();
            Doc = new FlowDocument();
            Log = new Paragraph();
            //Doc.PageWidth = 1900; //hacky way to disable text wrapping because RichTextBox *always* wraps

            InitializeComponent();
            UpdateTGICheckbox.DataContext = this;
            VerboseOutputCheckbox.DataContext = this;
            StatusBar.Visibility = Visibility.Collapsed;

            //Set Properties
            if (!Options.Default.UserPluginsDirectory.Equals("")) {
                Options.Default.UserPluginsDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SimCity 4\\Plugins");
                Options.Default.Save();
            }
            if (!Options.Default.SystemPluginsDirectory.Equals("")) {
                string steamDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam\\steamapps\\common\\SimCity 4 Deluxe\\Plugins");
                if (Directory.Exists(steamDir)) {
                    Options.Default.SystemPluginsDirectory = steamDir;
                } else {
                    Options.Default.SystemPluginsDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SimCity 4\\Plugins");
                }
                Options.Default.Save();
            }
        }


        /// <summary>
        /// Opens a file dialog to choose the cleanitol script.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChooseScript_Click(object sender, RoutedEventArgs e) {
            ResetTextBox();
            OpenFileDialog dialog = new OpenFileDialog();
            if (dialog.ShowDialog() == true) {
                _scriptPath = dialog.FileName;
                ScriptPathTextBox.Text = _scriptPath;
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
            if (_scriptPath is null) return; 
            ResetTextBox();

            //Fill File List
            _scriptRules = File.ReadAllLines(_scriptPath);
            _listOfFiles = Directory.EnumerateFiles(Options.Default.UserPluginsDirectory, "*", SearchOption.AllDirectories);
            if (Options.Default.ScanSystemPlugins) {
                _listOfFiles = _listOfFiles.Concat(Directory.EnumerateFiles(Options.Default.SystemPluginsDirectory));
            }
            _listOfFileNames = _listOfFiles.Select(fileName => Path.GetFileName(fileName)); //TODO .AsParallel
            
            //Fill TGI list if required
            if (UpdateTGIdb) {
                StatusBar.Visibility = Visibility.Visible;
                int totalfiles = _listOfFiles.Count();
                double filesScanned = 0;
                _listOfTGIs.Clear();

                FileProgressBar.Minimum = 0;
                FileProgressBar.Maximum = totalfiles;
                FileProgressBar.Value = 0;
                FileProgressLabel.Text = "0 / " + totalfiles;

                ProgressBarSetValueDelegate updateProgressDelegate = new ProgressBarSetValueDelegate(FileProgressBar.SetValue);
                TextBlockSetTextDelegate updateFileCountDelegate = new TextBlockSetTextDelegate(FileProgressLabel.SetValue);
                TextBlockSetTextDelegate updateTGICountDelegate = new TextBlockSetTextDelegate(TGICountLabel.SetValue);

                foreach (string filepath in _listOfFiles) {
                    filesScanned++;
                    if (DBPFUtil.IsValidDBPF(filepath)) {
                        DBPFFile dbpf = new DBPFFile(filepath);
                        _listOfTGIs.AddRange(dbpf.GetTGIs().Select(tgi => tgi.ToStringShort()));
                    }

                    Dispatcher.Invoke(updateProgressDelegate, System.Windows.Threading.DispatcherPriority.Background, new object[] { ProgressBar.ValueProperty, filesScanned });
                    Dispatcher.Invoke(updateFileCountDelegate, System.Windows.Threading.DispatcherPriority.Background, new object[] { TextBlock.TextProperty, filesScanned + " / " + totalfiles + " files"});
                    Dispatcher.Invoke(updateTGICountDelegate, System.Windows.Threading.DispatcherPriority.Background, new object[] { TextBlock.TextProperty, _listOfTGIs.Count.ToString("N0") + " TGIs discovered" });
                }
                StatusLabel.Text = "Scan complete";

                //TODO - write TGI list to DB for local storage?
            }

            //Evaluate script and report results
            Log.Inlines.Add(RunStyles.BlackMono("-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-\r\n"));
            Log.Inlines.Add(RunStyles.BlackMono("    R E P O R T   S U M M A R Y    \r\n"));
            Log.Inlines.Add(RunStyles.BlackMono("-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-\r\n"));
            for (int idx = 0; idx < _scriptRules.Length; idx++) {
                EvaluateRule(_scriptRules[idx]);
            }
            Log.Inlines.InsertAfter(Log.Inlines.ElementAt(2), RunStyles.BlackMono($"{_filesToRemove.Count} files to remove.\r\n"));
            Log.Inlines.InsertAfter(Log.Inlines.ElementAt(3), RunStyles.BlueMono($"{_countDepsFound}/{_countDepsScanned} dependencies found.\r\n"));
            Log.Inlines.InsertAfter(Log.Inlines.ElementAt(4), RunStyles.RedMono($"{_countDepsMissing}/{_countDepsScanned} dependencies missing.\r\n"));
            Log.Inlines.InsertAfter(Log.Inlines.ElementAt(5), RunStyles.BlackMono("-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-\r\n\r\n"));

            Doc.Blocks.Add(Log);
            ScriptOutput.Document = Doc;
            UpdateTGIdb = false;
            UpdateTGICheckbox.IsChecked = false;
        }



        /// <summary>
        /// Evaluate a given rule and Log the result.
        /// </summary>
        /// <param name="ruleText">Rule to evaluate</param>
        private void EvaluateRule(string ruleText) {
            ScriptRule.RuleType result = ScriptRule.ParseRuleType(ruleText);
            switch (result) {

                case ScriptRule.RuleType.Removal:
                    IEnumerable<string> matchingFiles = Directory.EnumerateFiles(Options.Default.UserPluginsDirectory, ruleText, SearchOption.AllDirectories);
                    if (!matchingFiles.Any() && VerboseOutput) {
                        Log.Inlines.Add(RunStyles.BlueStd(ruleText));
                        Log.Inlines.Add(RunStyles.BlackStd(" not present." + "\r\n"));
                    } else {
                        string filename;
                        foreach (string file in matchingFiles) {
                            filename = Path.GetFileName(file);
                            //Make a special exception for the png images used for the in-game grid (the one that appears in the background when you play city tiles)
                            if (filename == "Background3D0.png" || filename == "Background3D1.png" || filename == "Background3D2.png" || filename == "Background3D3.png" || filename == "Background3D4.png") {
                                break;
                            }
                            Log.Inlines.Add(RunStyles.BlueStd(ruleText));
                            Log.Inlines.Add(RunStyles.BlueMono(" (" + filename + ")"));
                            Log.Inlines.Add(RunStyles.BlackStd(" found in "));
                            Log.Inlines.Add(RunStyles.RedStd(Path.GetDirectoryName(file) + "\r\n"));
                            _filesToRemove.Add(file);
                        }
                    }
                    break;


                case ScriptRule.RuleType.ConditionalDependency:
                case ScriptRule.RuleType.Dependency:
                    ScriptRule.DependencyRule rule = new ScriptRule.DependencyRule(ruleText);

                     bool isConditionalFound = true;
                    if (rule.ConditionalItem != "") {
                        if (rule.IsConditionalItemTGI) {
                            isConditionalFound = _listOfTGIs.Any(tgi => tgi.Contains(rule.ConditionalItem));
                        } else {
                            isConditionalFound = _listOfFiles.Any(tgi => tgi.Contains(rule.ConditionalItem));
                        }
                    }
                    
                    bool isItemFound = false;
                    if (isConditionalFound) {
                        if (rule.IsSearchItemTGI) {
                            isItemFound = _listOfTGIs.Any(r => r.Contains(rule.SearchItem));
                        } else {
                            isItemFound = _listOfFiles.Any(r => r.Contains(rule.SearchItem));
                        }
                    }
                    
                    if (isConditionalFound && !isItemFound) {
                        Log.Inlines.Add(RunStyles.RedMono("Missing: "));
                        Log.Inlines.Add(RunStyles.RedStd(rule.SearchItem));
                        Log.Inlines.Add(RunStyles.BlackStd(" is missing. Download from: "));

                        Hyperlink link = new Hyperlink(new Run(rule.SourceName == "" ? rule.SourceURL : rule.SourceName));
                        link.NavigateUri = new Uri(rule.SourceURL);
                        link.RequestNavigate += new RequestNavigateEventHandler(OnRequestNavigate);
                        Log.Inlines.Add(link);
                        Log.Inlines.Add(new Run("\r\n"));
                        _countDepsMissing++;
                    } else if (isConditionalFound && isItemFound && VerboseOutput) {
                        Log.Inlines.Add(RunStyles.BlueStd(rule.SearchItem));
                        Log.Inlines.Add(RunStyles.BlackStd(" was located." + "\r\n"));
                        _countDepsFound++;
                    }

                    _countDepsScanned++;
                    break;


                case ScriptRule.RuleType.UserCommentHeading:
                    Log.Inlines.Add(new Run("\r\n"));
                    Log.Inlines.Add(RunStyles.BlackHeading(string.Concat(ruleText.AsSpan(2), "\r\n")));
                    break;


                case ScriptRule.RuleType.UserComment:
                    Log.Inlines.Add(RunStyles.GreenStd(string.Concat(ruleText.AsSpan(1), "\r\n")));
                    break;

                
                case ScriptRule.RuleType.ScriptComment:
                default:
                    break;
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


        

        private void BackupFiles_Click(object sender, RoutedEventArgs e) {
            string outputDir = "C:\\Users\\Administrator\\Documents\\SimCity 4\\BSC_Cleanitol\\" + DateTime.Now.ToString("yyyyMMdd HHmmss");
            StringBuilder batchFile = new StringBuilder();
            Directory.CreateDirectory(outputDir);

            foreach (string file in _filesToRemove) {
                File.Move(file, Path.Combine(outputDir, Path.GetFileName(file)));
                //batch file: copy "Jigsaw 2010 tilesets.dat" "..\..\Plugins\Jigsaw 2010 tilesets.dat"
                batchFile.AppendLine("copy \"" + Path.GetFileName(file) + "\" \"..\\..\\Plugins\\" + Path.GetFileName(file));
            }
            File.WriteAllText(Path.Combine(outputDir, "undo.bat"), batchFile.ToString());

            //Write Summary HTML File.
            //https://stackoverflow.com/a/3314213
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "SC4CleanitolWPF.SummaryTemplate.html";
            StreamReader reader = new StreamReader(assembly.GetManifestResourceStream(resourceName));
            string summarytemplate = reader.ReadToEnd();
            summarytemplate = summarytemplate.Replace("#COUNTFILES", _filesToRemove.Count.ToString());
            summarytemplate = summarytemplate.Replace("#FOLDERPATH", outputDir);
            summarytemplate = summarytemplate.Replace("#HELPDOC", ""); //TODO - input path to help document
            summarytemplate = summarytemplate.Replace("#DATETIME", DateTime.Now.ToString("dd MMM yyyy HH:mm"));
            File.WriteAllText(Path.Combine(outputDir, "CleanupSummary.html"),summarytemplate);
        }



        private void Quit_Click(object sender, RoutedEventArgs e) {
            Application.Current.Shutdown();
        }


        /// <summary>
        /// Clear all inlines and reset file lists and counts.
        /// </summary>
        private void ResetTextBox() {
            Log.Inlines.Clear();
            _countDepsFound = 0;
            _countDepsMissing = 0;
            _countDepsScanned = 0;
            _filesToRemove.Clear();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e) {
            StatusBar.Visibility = Visibility.Collapsed;
        }

        private void Settings_Click(object sender, RoutedEventArgs e) {
            Preferences p = new Preferences();
            p.Show();
        }
    }
}
