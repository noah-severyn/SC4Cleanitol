using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
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
        private IEnumerable<string> _allFiles;
        private List<string> _filesToRemove;

        private string _playerPluginsFolder = "C:\\Users\\Administrator\\Documents\\SimCity 4\\Plugins\\";
        private string _systemPluginsFolder = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\SimCity 4 Deluxe\\Plugins\\";
        private Paragraph log;
        private FlowDocument doc;

        public MainWindow() {
            _filesToRemove = new List<string>();
            doc = new FlowDocument();
            doc.PageWidth = 1900; //hacky way to disable text wrapping because RichTextBox *always* wraps
            log = new Paragraph();

            InitializeComponent();
        }

        /// <summary>
        /// Opens a file dialog to choose the cleanitol script.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChooseScript_Click(object sender, RoutedEventArgs e) {
            Reset();
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
            Reset();
            _scriptRules = File.ReadAllLines(_scriptPath);
            _allFiles = Directory.EnumerateFiles(_playerPluginsFolder, "*", SearchOption.AllDirectories);
            _allFiles = _allFiles.Select(fileName => fileName.Replace(_playerPluginsFolder, string.Empty));

            log.Inlines.Add(RunStyles.BlackMono("-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-\r\n"));
            log.Inlines.Add(RunStyles.BlackMono("            R E P O R T            \r\n"));
            log.Inlines.Add(RunStyles.BlackMono("-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-\r\n"));
            for (int idx = 0; idx < _scriptRules.Length; idx++) {
                EvaluateRule(_scriptRules[idx]);
            }
            log.Inlines.InsertAfter(log.Inlines.ElementAt(2), RunStyles.BlackMono($"{_filesToRemove.Count} files to remove.\r\n"));
            log.Inlines.InsertAfter(log.Inlines.ElementAt(3), RunStyles.BlueMono($"{_countDepsFound}/{_countDepsScanned} dependencies found.\r\n"));
            log.Inlines.InsertAfter(log.Inlines.ElementAt(4), RunStyles.RedMono($"{_countDepsMissing}/{_countDepsScanned} dependencies missing.\r\n"));
            log.Inlines.InsertAfter(log.Inlines.ElementAt(5), RunStyles.BlackMono("-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-\r\n\r\n"));

            doc.Blocks.Add(log);
            ScriptOutput.Document = doc;
        }

        /// <summary>
        /// Evaluate a given rule and log the result.
        /// </summary>
        /// <param name="ruleText">Rule to evaluate</param>
        private void EvaluateRule(string ruleText) {
            switch (ParseRuleType(ruleText)) {

                case RuleType.Removal:
                    IEnumerable<string> matchingFiles = Directory.EnumerateFiles(_playerPluginsFolder, ruleText, SearchOption.AllDirectories);
                    if (!matchingFiles.Any()) {
                        log.Inlines.Add(RunStyles.BlueStd(ruleText));
                        log.Inlines.Add(RunStyles.BlackStd(" not present." + "\r\n"));
                    } else {
                        foreach (string file in matchingFiles) {
                            log.Inlines.Add(RunStyles.BlueStd(ruleText));
                            log.Inlines.Add(RunStyles.BlueMono(" (" + Path.GetFileName(file) + ")"));
                            log.Inlines.Add(RunStyles.BlackStd(" found in "));
                            log.Inlines.Add(RunStyles.RedStd(Path.GetDirectoryName(file) + "\r\n"));
                            _filesToRemove.Add(file);
                        }
                    }
                    break;


                case RuleType.Dependency:
                    //run loop for the file matching the criteria
                    
                    
                    DependencyRule dr = new DependencyRule(ruleText);
                    bool isMissing = true;


                    //log.Inlines.Add(RunStyles.RedStd(ruleText + "\r\n"));
                    //https://stackoverflow.com/questions/2288999/how-can-i-get-a-flowdocument-hyperlink-to-launch-browser-and-go-to-url-in-a-wpf
                    if (isMissing) {
                        log.Inlines.Add(RunStyles.RedMono("Missing: "));
                        log.Inlines.Add(RunStyles.RedStd(dr.TargetItem));
                        log.Inlines.Add(RunStyles.BlackStd(" is missing. Download at: "));
                        Hyperlink link = new Hyperlink(new Run(dr.SourceName));
                        link.NavigateUri = new Uri(dr.SourceURL);
                        link.RequestNavigate += new System.Windows.Navigation.RequestNavigateEventHandler(hlink_RequestNavigate);
                        log.Inlines.Add(link);
                        log.Inlines.Add(new Run("\r\n"));
                    }

                    _countDepsScanned++;
                    break;

                
                case RuleType.UserComment:
                    //TODO - allow markdown formatting???
                    //https://github.com/xoofx/markdig
                    log.Inlines.Add(RunStyles.GreenStd(ruleText + "\r\n"));
                    break;

                
                case RuleType.ScriptComment:
                default:
                    break;
            }
        }
        void hlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e) {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }


        private class DependencyRule {
            public string TargetItem { get; set; }
            public bool IsTargetTGI { get; set; }
            public string SourceName { get; set; }
            public string SourceURL { get; set; }

            public DependencyRule(string ruleText) {
                int semicolonLocn = ruleText.IndexOf(';');
                TargetItem = ruleText.Substring(0, semicolonLocn);
                IsTargetTGI = ruleText.Substring(0, 2) == "0x";
                int httpLocn = ruleText.IndexOf("http");
                if (httpLocn > semicolonLocn + 1) { //if there's no source file name specified.
                    SourceName = ruleText.Substring(semicolonLocn + 1, httpLocn - semicolonLocn - 2);
                } else {
                    SourceName = ruleText.Substring(httpLocn); ;
                }
                
                SourceURL = ruleText.Substring(httpLocn);
            }
        }

        private void BackupFiles_Click(object sender, RoutedEventArgs e) {

        }

        private void Quit_Click(object sender, RoutedEventArgs e) {
            Application.Current.Shutdown();
        }

        private void Reset() {
            log.Inlines.Clear();
            _countDepsFound = 0;
            _countDepsMissing = 0;
            _filesToRemove.Clear();
        }


        /// <summary>
        /// Determine the action to be taken for this ruleText.
        /// </summary>
        /// <param name="rule">Rule text</param>
        /// <returns>A <see cref="RuleType"/> informing the action to be taken</returns>
        private static RuleType ParseRuleType(string rule) {
            int locn = rule.IndexOf('>');
            if (locn == 0) { return RuleType.UserComment; }

            locn = rule.IndexOf(';');
            if (locn == 0) {
                return RuleType.ScriptComment;
            } else if (locn > 0) {
                return RuleType.Dependency;
            }

            locn = rule.IndexOf("http");
            if (locn > 0) { return RuleType.Dependency; }

            return RuleType.Removal;
        }

        /// <summary>
        /// The action to be taken for this ruleText.
        /// </summary>
        /// <see ref="https://www.sc4devotion.com/forums/index.php?topic=3797.0"/>
        public enum RuleType {
            /// <summary>
            /// Specifying a filename or extension type (wildcards ARE supported) signals a removal ruleText.
            /// </summary>
            Removal,
            /// <summary>
            /// Specifying a filename then semicolon then a HTTP URL signals a dependency required ruleText.
            /// </summary>
            /// <remarks>
            /// A friendly file name for the URL can also be specified. Following the semicolon specify the friendly name and the URL, separated by a space.
            /// </remarks>
            Dependency,
            /// <summary>
            /// Text shown to the user in the script output.
            /// </summary>
            UserComment,
            /// <summary>
            /// Internal comments not shown to the user for script documentation.
            /// </summary>
            ScriptComment
        }
    }
}
