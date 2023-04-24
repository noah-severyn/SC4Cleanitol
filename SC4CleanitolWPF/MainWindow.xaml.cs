using System;
using System.Collections.Generic;
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
        private FileInfo _script;
        private string[] _scriptContents;
        private int _scriptVersion;
        private int _countDepsFound;
        private int _countDepsMissing;
        private List<string> _filesToRemove;

        private string _playerPluginsFolder = "C:\\Users\\Administrator\\Documents\\SimCity 4\\Plugins";
        private string _systemPluginsFolder = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\SimCity 4 Deluxe\\Plugins";
        private Paragraph log;
        private FlowDocument doc;

        public MainWindow() {
            _filesToRemove = new List<string>();
            doc = new FlowDocument();
            doc.PageWidth = 1900; //hacky way to disable text wrapping because RichTextBox *always* wraps
            log = new Paragraph();

            InitializeComponent();
        }
        private void ChooseScript_Click(object sender, RoutedEventArgs e) {
            Reset();
            OpenFileDialog dialog = new OpenFileDialog();
            if (dialog.ShowDialog() == true) {
                _script = new FileInfo(dialog.FileName);
                ScriptPathTextBox.Text = _script.FullName;
                _scriptContents = File.ReadAllLines(_script.FullName);
            } else {
                return;
            }
        }

        private void RunScript_Click(object sender, RoutedEventArgs e) {
            Reset();
            _scriptContents = File.ReadAllLines(_script.FullName);
            _scriptVersion = GetScriptVersion();

            //if script version is 2 additionally support output comments with ">" and searching of TGIs
            log.Inlines.Add(RunStyles.BlackMono("-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-\r\n"));
            log.Inlines.Add(RunStyles.BlackMono("            R E P O R T            \r\n"));
            log.Inlines.Add(RunStyles.BlackMono("-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-\r\n"));

            for (int idx = 0; idx < _scriptContents.Length; idx++) {
                EvaluateRule(_scriptContents[idx]);
            }

            log.Inlines.InsertAfter(log.Inlines.ElementAt(2), RunStyles.BlackMono($"{_filesToRemove.Count} files found to remove.\r\n"));
            log.Inlines.InsertAfter(log.Inlines.ElementAt(3), RunStyles.BlueMono($"##/### dependencies found.\r\n"));
            log.Inlines.InsertAfter(log.Inlines.ElementAt(4), RunStyles.RedMono($"##/### dependencies missing.\r\n"));
            log.Inlines.InsertAfter(log.Inlines.ElementAt(5), RunStyles.BlackMono("-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-\r\n\r\n"));

            doc.Blocks.Add(log);
            ScriptOutput.Document = doc;
        }


        private void EvaluateRule(string ruleText) {
            switch (ParseRule(ruleText)) {
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
                    log.Inlines.Add(RunStyles.RedStd(ruleText + "\r\n"));
                    break;

                case RuleType.UserComment:
                    log.Inlines.Add(RunStyles.GreenStd(ruleText + "\r\n"));
                    break;

                case RuleType.ScriptComment:
                default:
                    break;
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
            _scriptVersion = 0;
        }

        private int GetScriptVersion() {
            string firstLine = _scriptContents[0];
            int locn = firstLine.IndexOf('=');
            int.TryParse(firstLine.AsSpan(locn + 1), out int version);
            return version;
        }


        /// <summary>
        /// Determine the action to be taken for this ruleText.
        /// </summary>
        /// <param name="rule">Rule text</param>
        /// <returns>A <see cref="RuleType"/> informing the action to be taken</returns>
        private static RuleType ParseRule(string rule) {
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
