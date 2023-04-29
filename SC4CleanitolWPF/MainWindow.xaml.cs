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

        //Output setting
        private bool _isWrapped = false;
        private bool _isVerbose;


        //TODO - verbose & non verbose outputs



        public MainWindow() {
            _filesToRemove = new List<string>();
            doc = new FlowDocument();
            //doc.PageWidth = 1900; //hacky way to disable text wrapping because RichTextBox *always* wraps
            
            log = new Paragraph();

            InitializeComponent();
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
            ResetTextBox();
            _scriptRules = File.ReadAllLines(_scriptPath);
            _allFiles = Directory.EnumerateFiles(_playerPluginsFolder, "*", SearchOption.AllDirectories);
            _allFiles = _allFiles.Select(fileName => Path.GetFileName(fileName)); //TODO .AsParallel

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
            ScriptRule.RuleType result = ScriptRule.ParseRuleType(ruleText);
            switch (result) {
                case ScriptRule.RuleType.Removal:
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


                case ScriptRule.RuleType.Dependency:
                    ScriptRule.DependencyRule rule = new ScriptRule.DependencyRule(ruleText);
                    bool isMissing = _allFiles.Any(r => r.Contains(rule.SourceName));

                    //https://stackoverflow.com/questions/2288999/how-can-i-get-a-flowdocument-hyperlink-to-launch-browser-and-go-to-url-in-a-wpf
                    if (isMissing) {
                        log.Inlines.Add(RunStyles.RedMono("Missing: "));
                        log.Inlines.Add(RunStyles.RedStd(rule.SearchItem));
                        log.Inlines.Add(RunStyles.BlackStd(" is missing. Download at: "));
                        Hyperlink link = new Hyperlink(new Run(rule.SourceName));
                        link.NavigateUri = new Uri(rule.SourceURL);
                        //Hyperlink link = new Hyperlink(new Run("test link"));
                        //link.NavigateUri = new Uri("http://www.google.com");
                        link.RequestNavigate += new RequestNavigateEventHandler(OnRequestNavigate);
                        log.Inlines.Add(link);
                        log.Inlines.Add(new Run("\r\n"));
                    } else {
                        log.Inlines.Add(RunStyles.BlueStd(rule.SearchItem));
                        log.Inlines.Add(RunStyles.BlackStd(" was located." + "\r\n"));

                    }

                    _countDepsScanned++;
                    break;


                case ScriptRule.RuleType.UserCommentHeading:
                    log.Inlines.Add(new Run("\r\n"));
                    log.Inlines.Add(RunStyles.BlackHeading(ruleText.Substring(2) + "\r\n"));
                    break;


                case ScriptRule.RuleType.UserComment:
                    log.Inlines.Add(RunStyles.GreenStd(ruleText.Substring(1) + "\r\n"));
                    break;

                
                case ScriptRule.RuleType.ScriptComment:
                default:
                    break;
            }
        }
        void OnRequestNavigate(object sender, RequestNavigateEventArgs e) {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }


        

        private void BackupFiles_Click(object sender, RoutedEventArgs e) {

        }

        private void Quit_Click(object sender, RoutedEventArgs e) {
            Application.Current.Shutdown();
        }


        /// <summary>
        /// Clear all inlines and reset file lists and counts.
        /// </summary>
        private void ResetTextBox() {
            log.Inlines.Clear();
            _countDepsFound = 0;
            _countDepsMissing = 0;
            _filesToRemove.Clear();
        }

        private void WordWrapCheckbox_Checked(object sender, RoutedEventArgs e) {
            //_isWrapped = !_isWrapped;
            //if (_isWrapped) {
            //    doc.PageWidth = this.ActualWidth - 10;
            //} else {
            //    //doc.PageWidth = 1900; //hacky way to disable text wrapping because RichTextBox *always* wraps
            //}
        }
    }
}
