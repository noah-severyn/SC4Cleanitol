using System.Collections.Generic;
using System.Text;
using csDBPF;

namespace SC4Cleanitol {
    public class CleanitolEngine {
        private string _userPlugins;
        /// <summary>
        /// Location of the plugins folder found in the user's Documents folder.
        /// </summary>
        public string UserPluginsDirectory {
            get { return _userPlugins; }
            set {
                if (!Directory.Exists(value)) {
                    _userPlugins = string.Empty;
                } else {
                    _userPlugins = value;
                }
            }
        }

        private string _systemPlugins;
        /// <summary>
        /// Location of the plugins folder found in the game install directory.
        /// </summary>
        public string SystemPluginsDirectory {
            get { return _systemPlugins; }
            set {
                if (!Directory.Exists(value)) {
                    _systemPlugins = string.Empty;
                } else {
                    _systemPlugins = value;
                }
            }
        }

        private string _baseOutput;
        /// <summary>
        /// Location files will be removed to and the output summary will be saved to.
        /// </summary>
        public string BaseOutputDirectory {
            get { return _baseOutput; }
            set {
                if (!Directory.Exists(value)) {
                    _baseOutput = string.Empty;
                } else {
                    _baseOutput = value;
                }
            }
        }

        private string _scriptOutput;
        /// <summary>
        /// Location the last script run moved files to. Equivalent to the <see cref="BaseOutputDirectory"/> plus a date time stamp.
        /// </summary>
        public string ScriptOutputDirectory { 
            get { return _scriptOutput; }
            private set { _scriptOutput = value; } 
        }

        private string _scriptPath;
        /// <summary>
        /// Path to the Cleanitol script to run.
        /// </summary>
        public string ScriptPath {
            get { return _scriptPath; }
            set {
                if (!File.Exists(value)) {
                    _scriptPath = string.Empty;
                } else {
                    _scriptPath = value;
                }
            }
        }

        private List<string> _additionalFolders;
        /// <summary>
        /// A list of additional folders to scan in addition to the current plugins folders.
        /// </summary>
        public List<string> AdditionalFolders {
            get { return _additionalFolders; }
            set { _additionalFolders = value; }
        }




        /// <summary>
        /// Count of dependencies scanned.
        /// </summary>
        public int CountDepsScanned { get; private set; }
        /// <summary>
        /// Count of dependencies located in plugins.
        /// </summary>
        public int CountDepsFound { get; private set; }
        /// <summary>
        /// Count of missing dependencies.
        /// </summary>
        public int CountDepsMissing { get; private set; }
        /// <summary>
        /// Files scanned by the script.
        /// </summary>
        public List<string> ListOfFiles { get; private set; }
        /// <summary>
        /// File names trimmed from <see cref="ListOfFiles"/>.
        /// </summary>
        public IEnumerable<string> ListOfFileNames { get; private set; }
        /// <summary>
        /// All TGIs scanned by the script in a comma separated format: <c>0x00000000, 0x00000000, 0x00000000</c>.
        /// </summary>
        public List<TGI> ListOfTGIs { get; private set; }
        /// <summary>
        /// Files found to be removed from the script.
        /// </summary>
        public List<string> FilesToRemove { get; private set; }

        public string LogPath { get; private set; }

        private List<string> _scriptRules;
        private int _highestPctReached;
        private List<FormattedRun> _runs;



        /// <summary>
        /// Instantiate a Cleanitol instance which holds the functions and logic to operate on a user's plugins folder.
        /// </summary>
        /// <param name="userPluginsDirectory">Path to the user plugins folder (in the Documents folder).</param>
        /// <param name="systemPluginsDirectory">Path to the system plugins folder (in the game install folder).</param>
        /// <param name="outputDirectory">Path to the folder where files will be moved to after the cleaning process (recommended to set to <c>\Documents\SimCity 4\BSC_Cleanitol\</c>  unless there is a specific desire to alter the path).</param>
        /// <param name="scriptPath">Path to the cleanitol script to run.</param>
        public CleanitolEngine(string userPluginsDirectory, string systemPluginsDirectory, string outputDirectory, string scriptPath) {
            if (!Directory.Exists(userPluginsDirectory)) {
                _userPlugins = string.Empty;
            } else {
                _userPlugins = userPluginsDirectory;
            }
            if (!Directory.Exists(systemPluginsDirectory)) {
                _systemPlugins = string.Empty;
            } else {
                _systemPlugins = systemPluginsDirectory;
            }
            if (!Directory.Exists(outputDirectory)) {
                _baseOutput = string.Empty;
            } else {
                _baseOutput = outputDirectory;
            }
            _scriptOutput = _baseOutput;
            if (!File.Exists(scriptPath)) {
                _scriptPath = string.Empty;
            } else {
                _scriptPath = scriptPath;
            }

            _additionalFolders = new List<string>();
            _scriptRules = new List<string>();
            _runs = new List<FormattedRun>();
            ListOfFiles = new List<string>();
            ListOfFileNames = new List<string>();
            ListOfTGIs = new List<TGI>();
            FilesToRemove = new List<string>();

            LogPath = Path.Combine(BaseOutputDirectory, "SC4Cleanitol_Error_Log.txt");
            if (BaseOutputDirectory != string.Empty && !Directory.Exists(BaseOutputDirectory)) {
                Directory.CreateDirectory(BaseOutputDirectory);
            }
        }



        /// <summary>
        /// Execute the script and return the results of each rule.
        /// </summary>
        /// <param name="totalFiles">The Progress tracker for the total number of files.</param>
        /// <param name="progressFiles">The progress tracker for the current progress of scanned files.</param>
        /// <param name="progressTGIs">The progress tracker for the current progress of scanned TGIs.</param>
        /// <param name="updateTGIdatabase">Specify to rebuild the internal index of TGIs.</param>
        /// <param name="includeSystemPlugins">Specify to include the system plugins folder in the TGI scan or only the user plugins (recommended).</param>
        /// <param name="includeExtraFolders">Specify to include the additional folders with the plugins folder in the scan.</param>
        /// <param name="verboseOutput">Specify to return a message for every rule, or only return a message if an action needs to be taken.</param>
        /// <returns>A series of formatted messages detailing the result of each script rule.</returns>
        public List<FormattedRun> RunScript(IProgress<int> totalFiles, IProgress<int> progressFiles, IProgress<int> progressTGIs, bool updateTGIdatabase, bool includeSystemPlugins, bool includeExtraFolders, bool verboseOutput = true) {
            CountDepsFound = 0;
            CountDepsMissing = 0;
            CountDepsScanned = 0;
            FilesToRemove.Clear();
            ListOfFiles.Clear();
            ListOfFileNames = Enumerable.Empty<string>();
            _runs.Clear();
            List<FormattedRun> fileErrors = new List<FormattedRun>();
            using StreamWriter sw = new StreamWriter(LogPath, false);


            //Fill File List
            _scriptRules = File.ReadAllLines(_scriptPath).ToList();
            try {
                ListOfFiles = Directory.EnumerateFiles(_userPlugins, "*", SearchOption.AllDirectories).ToList();
                if (includeSystemPlugins) {
                    ListOfFiles.AddRange(Directory.EnumerateFiles(_systemPlugins, "*", SearchOption.AllDirectories).ToList());
                }
                if (includeExtraFolders) {
                    foreach (string folder in _additionalFolders) {
                        if (Path.Exists(folder)) {
                            ListOfFiles.AddRange(Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories).ToList());
                        }
                    }
                }
                ListOfFiles.Sort(); //Critical because this makes the binary search perform many times better

                totalFiles.Report(ListOfFiles.Count);
                ListOfFileNames = ListOfFiles.AsParallel().Select(fileName => Path.GetFileName(fileName));
            }
            catch (IOException) {
                return _runs;
            }
            
            
            //Fill TGI list if required
            if (updateTGIdatabase) {
                int totalfiles = ListOfFiles.Count;
                int filesScanned = 0;
                ListOfTGIs.Clear();

                foreach (string filepath in ListOfFiles) {
                    filesScanned++;
                    progressFiles.Report(filesScanned);

                    //TODO - MAKE SURE THIS WORKS FOR REMOVAL FUNCTIONALITY TOO
                    try {
                        if (DBPFUtil.IsValidDBPF(filepath)) {
                            DBPFFile dbpf = new DBPFFile(filepath);
                            ListOfTGIs.AddRange(dbpf.GetTGIs());
                            progressTGIs.Report(ListOfTGIs.Count);
                        }
                    } catch (Exception ex) {
                        fileErrors.Add(new FormattedRun("Error: ", RunType.RedMono));
                        fileErrors.Add(new FormattedRun($"{filepath} was skipped.\r\n", RunType.RedStd));
                            
                        sw.WriteLine("=============== Log Start ===============");
                        sw.WriteLine("Time: " + DateTime.Now);
                        sw.WriteLine("Script: " + ScriptPath);
                        sw.WriteLine("File: " + filepath);
                        sw.WriteLine($"Error: {ex.GetType()}: {ex.Message}");
                        sw.WriteLine("Trace: \r\n" + ex.StackTrace);
                        sw.WriteLine("================ Log End ================");
                    }
                    

                    int pctComplete = filesScanned / totalfiles * 100;
                    if (pctComplete > _highestPctReached) {
                        _highestPctReached = pctComplete;
                    }
                }
                ListOfTGIs.Sort();
            } else {
                progressFiles.Report(ListOfFiles.Count);
            }

            //TODO - write TGI list to DB for local storage?


            //Evaluate script and report results
            _runs.Add(new FormattedRun("-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-\r\n", RunType.BlackMono));
            _runs.Add(new FormattedRun("    R E P O R T   S U M M A R Y    \r\n", RunType.BlackMono));
            _runs.Add(new FormattedRun("-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-\r\n", RunType.BlackMono));
            for (int idx = 0; idx < _scriptRules.Count; idx++) {
                EvaluateRule(_scriptRules[idx].Trim(), verboseOutput);
            }
            _runs.Add(new FormattedRun("\r\n\r\n"));
            _runs.Insert(3, new FormattedRun($"{FilesToRemove.Count} files to remove.\r\n", RunType.BlackMonoBold));
            _runs.Insert(4, new FormattedRun($"{CountDepsFound}/{CountDepsScanned} dependencies found." + (CountDepsFound != CountDepsScanned ? $" ({CountDepsScanned - CountDepsFound} dependencies not required due to conditional rules)" : "") +  "\r\n", RunType.BlueMono));
            _runs.Insert(5, new FormattedRun($"{CountDepsMissing}/{CountDepsFound} dependencies missing.\r\n", RunType.RedMono));
            _runs.Insert(6, new FormattedRun("-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-\r\n", RunType.BlackMono));

            if (fileErrors.Count > 0) {
                fileErrors.Add(new FormattedRun("Consult the ", RunType.RedMono));
                fileErrors.Add(new FormattedRun("error log", RunType.Hyperlink, LogPath));
                fileErrors.Add(new FormattedRun(" located in the output directory for detailed troubleshooting information.\r\n\r\n", RunType.RedMono));
                _runs.AddRange(fileErrors);
            }

            return _runs;
        }

        
        /// <summary>
        /// Evaluate whether any of the rules in this script rely on TGI dependencies, either as the search item or as the condition.
        /// </summary>
        /// <returns>TRUE if any of the rules requires a TGI; FALSE if all rules involve file names</returns>
        public bool ScriptHasTGIRules() {
            _scriptRules = File.ReadAllLines(_scriptPath).ToList();
            string rule;
            ScriptRule.RuleType rt;
            for (int idx = 0; idx < _scriptRules.Count; idx++) {
                rule = _scriptRules[idx].Trim();
                rt = ScriptRule.ParseRuleType(rule);
                if ((rt == ScriptRule.RuleType.ConditionalDependency || rt == ScriptRule.RuleType.Dependency) && IsTGIDependencyRule(rule)) {
                    return true;
                }
            }
            return false;
        }


        /// <summary>
        /// Evaluate a rule and return the outcome.
        /// </summary>
        /// <remarks>
        /// This function can be used to run individual rules if you do not want to create a script file. Otherwise the <see cref="RunScript"/>  function is recommended to process rules from a script file. Ensure the rule is syntactically correct or unpredictable results can occur.
        /// </remarks>
        /// <param name="ruleText">Rule to evaluate</param>
        /// <param name="verboseOutput">Show verbose output or not</param>
        /// <returns>A list of runs describing the outcome of the rule</returns>
        private void EvaluateRule(string ruleText, bool verboseOutput = true) {
            switch (ScriptRule.ParseRuleType(ruleText)) {
                case ScriptRule.RuleType.Removal:
                    EvaluateRemovalRule(ruleText, verboseOutput);
                    return;

                case ScriptRule.RuleType.ConditionalDependency:
                case ScriptRule.RuleType.Dependency:
                    EvaluateDependencyRule(ruleText, verboseOutput);
                    return;

                case ScriptRule.RuleType.UserComment:
                    if (ruleText.StartsWith('>')) {
                        _runs.Add(new FormattedRun(ruleText.Substring(1) + "\r\n", RunType.GreenStd));
                    } else {
                        _runs.Add(new FormattedRun(ruleText + "\r\n", RunType.GreenStd));
                    }
                    return;

                case ScriptRule.RuleType.UserCommentHeading:
                    _runs.Add(new FormattedRun("\r\n" + ruleText.Substring(2) + "\r\n", RunType.BlackHeading));
                    return;

                case ScriptRule.RuleType.ScriptComment:
                    return;

                default:
                    _runs.Add(new FormattedRun(ruleText + "\r\n", RunType.GreenStd));
                    return;

            }
        }


        private bool IsTGIDependencyRule(string ruleText) {
            ScriptRule.DependencyRule rule = new ScriptRule.DependencyRule(ruleText);
            return rule.IsConditionalItemTGI || rule.IsSearchItemTGI;
        }


        private void EvaluateRemovalRule(string ruleText, bool verboseOutput) {
            ruleText = ruleText.Replace("*", string.Empty);
            //IEnumerable<string> matchingFiles = ListOfFiles.AsParallel().Where(item => item.Contains(ruleText));
            IEnumerable<string> matchingFiles = ListOfFiles.AsParallel().Where(item => item.Contains("\\" + ruleText));

            if (!matchingFiles.Any() && verboseOutput) {
                _runs.Add(new FormattedRun(ruleText, RunType.BlueStd));
                _runs.Add(new FormattedRun(" not present." + "\r\n", RunType.BlackStd));
            } else {
                string filename;
                foreach (string file in matchingFiles) {
                    filename = Path.GetFileName(file);
                    //Make a special exception for the png images used for the in-game grid (the one that appears in the background when you play city tiles)
                    if (filename == "Background3D0.png" || filename == "Background3D1.png" || filename == "Background3D2.png" || filename == "Background3D3.png" || filename == "Background3D4.png") {
                        break;
                    }
                    _runs.Add(new FormattedRun(ruleText, RunType.BlueStd));
                    _runs.Add(new FormattedRun(" (" + filename + ")", RunType.BlueMono));
                    _runs.Add(new FormattedRun(" found in ", RunType.BlackStd));
                    _runs.Add(new FormattedRun(Path.GetDirectoryName(file) + "\r\n", RunType.RedStd));
                    FilesToRemove.Add(file);
                }
            }
        }

        private void EvaluateDependencyRule(string ruleText, bool verboseOutput) {
            ScriptRule.DependencyRule rule = new ScriptRule.DependencyRule(ruleText);

            if (rule.IsUnchecked) {
                _runs.Add(new FormattedRun("[Unchecked dependency]:", RunType.BlueStd));
                _runs.Add(new FormattedRun(" " + rule.SearchItem, RunType.RedStd));
                _runs.Add(new FormattedRun(". Download from: ", RunType.BlackStd));
                _runs.Add(new FormattedRun(rule.SourceName == "" ? rule.SourceURL : rule.SourceName, RunType.Hyperlink, rule.SourceURL));
                _runs.Add(new FormattedRun("\r\n"));
            }

            bool isConditionalFound = false;
            if (rule.IsConditionalRule) {
                if (rule.IsConditionalItemTGI) {
                    isConditionalFound = ListOfTGIs.BinarySearch(DBPFTGI.ParseTGIString(rule.ConditionalItem)) >= 0;
                } else {
                    //isConditionalFound = ListOfFiles.AsParallel().Any(tgi => tgi.Contains(rule.ConditionalItem));
                    isConditionalFound = ListOfFiles.BinarySearch(rule.SearchItem) >= 0;
                }
            }

            bool isItemFound = false;
            if (isConditionalFound || !rule.IsConditionalRule) {
                if (rule.IsSearchItemTGI) {
                    isItemFound = ListOfTGIs.BinarySearch(DBPFTGI.ParseTGIString(rule.SearchItem)) >= 0;
                } else {
                    //isItemFound = ListOfFiles.AsParallel().Any(r => r.Contains(rule.SearchItem));
                    isItemFound = ListOfFiles.BinarySearch(rule.SearchItem) >= 0;
                }
            }




            if (!rule.IsConditionalRule) {
                if (isItemFound) {
                    CountDepsFound++;
                    if (verboseOutput) {
                        _runs.Add(new FormattedRun(rule.SearchItem, RunType.BlueStd));
                        _runs.Add(new FormattedRun(" was found.\r\n", RunType.BlackStd));
                    }
                } else {
                    _runs.Add(new FormattedRun("Missing: ", RunType.RedMono));
                    _runs.Add(new FormattedRun(rule.SearchItem, RunType.RedStd));
                    _runs.Add(new FormattedRun(" is missing. Download from: ", RunType.BlackStd));
                    _runs.Add(new FormattedRun(rule.SourceName == "" ? rule.SourceURL : rule.SourceName, RunType.Hyperlink, rule.SourceURL));
                    _runs.Add(new FormattedRun("\r\n"));
                    CountDepsMissing++;
                }
            } else {
                if (isConditionalFound && isItemFound) {
                    CountDepsFound++;
                    if (verboseOutput) {
                        _runs.Add(new FormattedRun(rule.SearchItem, RunType.BlueStd));
                        _runs.Add(new FormattedRun(" was found.\r\n", RunType.BlackStd));
                    }
                } else if (isConditionalFound && !isItemFound) {
                    _runs.Add(new FormattedRun("Missing: ", RunType.RedMono));
                    _runs.Add(new FormattedRun(rule.SearchItem, RunType.RedStd));
                    _runs.Add(new FormattedRun(" is missing. Download from: ", RunType.BlackStd));
                    _runs.Add(new FormattedRun(rule.SourceName == "" ? rule.SourceURL : rule.SourceName, RunType.Hyperlink, rule.SourceURL));
                    _runs.Add(new FormattedRun("\r\n"));
                    CountDepsMissing++;
                } else if (!isConditionalFound) {
                    if (verboseOutput) {
                        _runs.Add(new FormattedRun(rule.SearchItem, RunType.BlueStd));
                        _runs.Add(new FormattedRun(" was skipped as ", RunType.BlackStd));
                        _runs.Add(new FormattedRun(rule.ConditionalItem, RunType.BlueStd));
                        _runs.Add(new FormattedRun(" was not found. Item: ", RunType.BlackStd));
                        _runs.Add(new FormattedRun(rule.SourceName == "" ? rule.SourceURL : rule.SourceName, RunType.Hyperlink, rule.SourceURL));
                        _runs.Add(new FormattedRun("\r\n"));

                    }
                }
            }
            CountDepsScanned++;
        }


        /// <summary>
        /// Move the files requested for removal to <see cref="ScriptOutputDirectory"/> and and create <c>undo.bat</c> and <c>CleanupSummary.html</c> files. 
        /// </summary>
        /// <param name="templateText">HTML template text.</param>
        public void BackupFiles(string templateText) {
            if (FilesToRemove.Count == 0) {
                return;
            }
            string outputDir = Path.Combine(_baseOutput, DateTime.Now.ToString("yyyyMMdd HHmmss"));
            StringBuilder batchFile = new StringBuilder();
            Directory.CreateDirectory(outputDir);

            //Write batch undo file
            foreach (string file in FilesToRemove) {
                string fname = Path.GetFileName(file);
                if (File.Exists(file)) {
                    try {
                        File.Move(file, Path.Combine(outputDir, fname));
                    }
                    catch (IOException) {
                        //To catch where there are files with the same name in different folders. Error moving them to the same location → delete additional files with the same name but still record their locations so they can be moved back.
                        File.Delete(file);
                    } finally {
                        batchFile.AppendLine($"copy \"{fname}\" \"{file}\"");
                    }
                }
            }
            File.WriteAllText(Path.Combine(outputDir, "undo.bat"), batchFile.ToString());

            //Write HTML Template summary
            templateText = templateText.Replace("#SCRIPTNAME", Path.GetFileName(_scriptPath));
            templateText = templateText.Replace("#COUNTFILES", FilesToRemove.Count.ToString());
            templateText = templateText.Replace("#FOLDERPATH", outputDir);
            templateText = templateText.Replace("#HELPDOC", "https://github.com/noah-severyn/SC4Cleanitol/wiki");
            templateText = templateText.Replace("#LISTOFFILES", string.Join("<br/>", FilesToRemove));
            templateText = templateText.Replace("#DATETIME", DateTime.Now.ToString("dd MMM yyyy HH:mm"));
            File.WriteAllText(Path.Combine(outputDir, "CleanupSummary.html"), templateText);

            ScriptOutputDirectory = outputDir;
        }



        /// <summary>
        /// Export the scanned TGIs to a CSV document in the assigned Cleanitol folder.
        /// </summary>
        /// <returns>The path of the exported CSV file</returns>
        public string ExportTGIs() {
            StringBuilder list = new StringBuilder("Type,Group,Instance,\r\n");
            foreach (TGI tgi in ListOfTGIs) {
                list.AppendLine(tgi.ToString());
            }
            string filename = "ScannedTGIs " + DateTime.Now.ToString("yyyy-MM-dd HH-mm") + ".csv";

            File.WriteAllText(Path.Combine(ScriptOutputDirectory, filename), list.ToString());
            return Path.Combine(ScriptOutputDirectory, filename);
        }


        /// <summary>
        /// Create a new Cleanitol file containing all files in the chosen folder and its subfolders.
        /// </summary>
        /// <param name="folderPath">Folder containing files to add to the script</param>
        /// <param name="scriptPath">Full path of Cleanitol file to create</param>
        public static void CreateCleanitolList(string folderPath, string scriptPath) {
            List<string> fileNames = new List<string>();
            DirectoryInfo di = new DirectoryInfo(folderPath);
            FileInfo[] files = di.GetFiles("*", SearchOption.AllDirectories);
            foreach (FileInfo file in files) {
                fileNames.Add(file.Name);
            }
            File.WriteAllLines(scriptPath, fileNames);
        }
    }
}