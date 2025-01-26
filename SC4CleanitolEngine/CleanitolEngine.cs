using System.Collections.Generic;
using System.Text;
using csDBPF;
using SC4CleanitolEngine;
using SQLite;

namespace SC4Cleanitol {
    public class CleanitolEngine {
        /// <summary>
        /// Specify which folders to scan.
        /// </summary>
        public enum FolderOptions : byte {
            /// <summary>
            /// Scan Plugins folder only, excluding additional folders.
            /// </summary>
            PluginsOnly = 0,
            /// <summary>
            /// Scan additional folders, including Plugins folders.
            /// </summary>
            AdditionalFoldersIncludingPlugins = 1,
            /// <summary>
            /// Scan additional folders, excluding Plugins folders.
            /// </summary>
            AdditionalFoldersExcludingPlugins = 2
        }

        /// <summary>
        /// TGI export type.
        /// </summary>
        public enum ExportType {
            CSV,
            SQLite
        }



        private string _userPlugins;
        /// <summary>
        /// Location of the plugins folder found in the user's Documents folder.
        /// </summary>
        public string UserPluginsDirectory {
            get { return _userPlugins; }
            set {
                if (Directory.Exists(value)) {
                    _userPlugins = value;
                } else {
                    _userPlugins = string.Empty;
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
                if (Directory.Exists(value)) {
                    _systemPlugins = value;
                } else {
                    _systemPlugins = string.Empty;
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
                if (Directory.Exists(value)) {
                    _baseOutput = value;
                } else {
                    _baseOutput = string.Empty;
                }
            }
        }
        /// <summary>
        /// Location the last script run moved files to. Equivalent to the <see cref="BaseOutputDirectory"/> plus a date time stamp.
        /// </summary>
        public string ScriptOutputDirectory { get; private set; }
        /// <summary>
        /// Whether the current operating system is Windows or Unix-based (Mac/Linux).
        /// </summary>
        public bool IsWindowsOS { get; private set; }
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
        /// All file paths scanned by the script.
        /// </summary>
        public List<string> ListOfFiles { get; private set; }
        /// <summary>
        /// All file names scanned by the script, extracted from <see cref="ListOfFiles"/>.
        /// </summary>
        public List<string> ListOfFileNames { get; private set; }
        /// <summary>
        /// All TGIs scanned by the script in a comma separated format: <c>0x00000000, 0x00000000, 0x00000000</c>.
        /// </summary>
        public List<TGI> ListOfTGIs { get; private set; }
        /// <summary>
        /// All file paths found by the script that should be removed.
        /// </summary>
        public List<string> ListOfFilesToRemove { get; private set; }
        /// <summary>
        /// Path for the created log file.
        /// </summary>
        public string LogPath { get; private set; }



        /// <summary>
        /// A list of additional folders to scan in addition to the current plugins folders.
        /// </summary>
        public List<string> AdditionalFolders { get; set; }



        private List<string> _scriptRules;
        private int _highestPctReached;
        private int _condDepsNotScanned;
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
                UserPluginsDirectory = string.Empty;
            } else {
                UserPluginsDirectory = userPluginsDirectory;
            }
            if (!Directory.Exists(systemPluginsDirectory)) {
                SystemPluginsDirectory = string.Empty;
            } else {
                SystemPluginsDirectory = systemPluginsDirectory;
            }
            if (!Directory.Exists(outputDirectory)) {
                BaseOutputDirectory = string.Empty;
            } else {
                BaseOutputDirectory = outputDirectory;
            }
            ScriptOutputDirectory = BaseOutputDirectory;

            IsWindowsOS = System.Runtime.InteropServices.RuntimeInformation.OSDescription.Contains("Windows");
            AdditionalFolders = new List<string>();
            _scriptRules = new List<string>();
            _runs = new List<FormattedRun>();
            ListOfFiles = new List<string>();
            ListOfFileNames = new List<string>();
            ListOfTGIs = new List<TGI>();
            ListOfFilesToRemove = new List<string>();

            LogPath = Path.Combine(BaseOutputDirectory, "SC4Cleanitol_Error_Log.txt");
            if (BaseOutputDirectory != string.Empty && !Directory.Exists(BaseOutputDirectory)) {
                Directory.CreateDirectory(BaseOutputDirectory);
            }
        }

        

        /// <summary>
        /// Execute the script and return the results of each rule.
        /// </summary>
        /// <param name="scriptPath">Path of the script to be run.</param>
        /// <param name="totalFiles">The Progress tracker for the total number of files.</param>
        /// <param name="progressFiles">The progress tracker for the current progress of scanned files.</param>
        /// <param name="progressTGIs">The progress tracker for the current progress of scanned TGIs.</param>
        /// <param name="updateTGIdatabase">Specify to rebuild the internal index of TGIs.</param>
        /// <param name="includeSystemPlugins">Specify to include the system plugins folder in the TGI scan or only the user plugins (recommended).</param>
        /// <param name="extraFoldersOption">Specify to include the additional folders with the plugins folder in the scan.</param>
        /// <param name="verboseOutput">Specify to return a message for every rule, or only return a message if an action needs to be taken.</param>
        /// <returns>A series of formatted messages detailing the result of each script rule.</returns>
        public List<FormattedRun> RunScript(string scriptPath, IProgress<int> totalFiles, IProgress<int> progressFiles, IProgress<int> progressTGIs, bool updateTGIdatabase, bool includeSystemPlugins, FolderOptions extraFoldersOption, bool verboseOutput = true) {
            CountDepsFound = 0;
            CountDepsMissing = 0;
            CountDepsScanned = 0;
            ListOfFilesToRemove.Clear();
            ListOfFiles.Clear();
            ListOfFileNames.Clear();
            _condDepsNotScanned = 0;
            _runs.Clear();
            List<FormattedRun> fileErrors = new List<FormattedRun>();
            using StreamWriter sw = new StreamWriter(LogPath, false);

            if (!File.Exists(scriptPath) && !scriptPath.StartsWith("https://raw.githubusercontent.com")) {
                _runs.Add(new FormattedRun("The script ", RunType.RedStd));
                _runs.Add(new FormattedRun(scriptPath, RunType.RedMono));
                _runs.Add(new FormattedRun(" does not exist or is pointing to an invalid Url.", RunType.RedMono));
                return _runs;
            }


            //Initially populate the script rules with the file or Url
            if (scriptPath.StartsWith("https://raw.githubusercontent.com")) {
                _scriptRules.AddRange(ImportFromGithub(scriptPath));
            } else {
                _scriptRules = File.ReadAllLines(scriptPath).ToList();
            }

            //Subsequently insert any rules from the remote script, if any
            IEnumerable<string> imports = _scriptRules.AsParallel().Where(item => item.Replace(" ", string.Empty).StartsWith("@https://raw.githubusercontent.com"));
            foreach (string import in imports) {
                _scriptRules.InsertRange(_scriptRules.IndexOf(import) + 1, ImportFromGithub(import.Replace(" ", string.Empty).Substring(1)));
            }


            //Fill File List
            try {
                switch (extraFoldersOption) {
                    case FolderOptions.PluginsOnly: //Plugins Only
                        ListOfFiles.AddRange(Directory.EnumerateFiles(UserPluginsDirectory, "*", SearchOption.AllDirectories).ToList());
                        if (includeSystemPlugins) {
                            ListOfFiles.AddRange(Directory.EnumerateFiles(SystemPluginsDirectory, "*", SearchOption.AllDirectories).ToList());
                        }
                        break;
                    case FolderOptions.AdditionalFoldersIncludingPlugins:
                        ListOfFiles.AddRange(Directory.EnumerateFiles(UserPluginsDirectory, "*", SearchOption.AllDirectories).ToList());
                        if (includeSystemPlugins) {
                            ListOfFiles.AddRange(Directory.EnumerateFiles(SystemPluginsDirectory, "*", SearchOption.AllDirectories).ToList());
                        }
                        foreach (string folder in AdditionalFolders) {
                            if (Path.Exists(folder)) {
                                ListOfFiles.AddRange(Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories).ToList());
                            }
                        }
                        break;
                    case FolderOptions.AdditionalFoldersExcludingPlugins:
                        foreach (string folder in AdditionalFolders) {
                            if (Path.Exists(folder)) {
                                ListOfFiles.AddRange(Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories).ToList());
                            }
                        }
                        break;
                }

                totalFiles.Report(ListOfFiles.Count);

                //Critical to sort because it makes the binary search perform many times quicker
                ListOfFiles.Sort(); 
                ListOfFileNames = ListOfFiles.AsParallel().Select(fileName => Path.GetFileName(fileName)).ToList();
                ListOfFileNames.Sort();
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
                        sw.WriteLine("Script: " + scriptPath);
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
            _runs.Insert(3, new FormattedRun($"{ListOfFilesToRemove.Count} files to remove.\r\n", RunType.BlackMonoBold));
            _runs.Insert(4, new FormattedRun($"{CountDepsFound}/{CountDepsScanned} dependencies found." + (_condDepsNotScanned > 0 ? $" ({_condDepsNotScanned} dependencies not scanned due to their conditional rules not being met)" : "") +  "\r\n", RunType.BlueMono));
            _runs.Insert(5, new FormattedRun($"{CountDepsMissing}/{CountDepsScanned} dependencies missing.\r\n", RunType.RedMono));
            _runs.Insert(6, new FormattedRun("-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-\r\n", RunType.BlackMono));

            if (fileErrors.Count > 0) {
                _runs.Add(new FormattedRun("Consult the ", RunType.RedMono));
                _runs.Add(new FormattedRun("error log", RunType.Hyperlink, LogPath));
                _runs.Add(new FormattedRun(" located in the output directory for detailed troubleshooting information.\r\n\r\n", RunType.RedMono));
                _runs.AddRange(fileErrors);
            }

            return _runs;
        }

        
        /// <summary>
        /// Evaluate whether any of the rules in this script rely on TGI dependencies, either as the search item or as the condition.
        /// </summary>
        /// <param name="scriptPath">Path of the script to examine.</param>
        /// <returns>TRUE if any of the rules requires a TGI; FALSE if all rules involve file names</returns>
        public bool ScriptHasTGIRules(string scriptPath) {
            _scriptRules = File.ReadAllLines(scriptPath).ToList();
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
                case ScriptRule.RuleType.Invalid:
                    _runs.Add(new FormattedRun(ruleText, RunType.RedMono));
                    _runs.Add(new FormattedRun(" is an invalid rule. Check syntax." + "\r\n", RunType.RedStd));
                    return;
                
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
                case ScriptRule.RuleType.Import:
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
                    ListOfFilesToRemove.Add(file);
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
                    isConditionalFound = ListOfFileNames.BinarySearch(rule.SearchItem) >= 0;
                }
            }

            bool isItemFound = false;
            if (isConditionalFound || !rule.IsConditionalRule) {
                if (rule.IsSearchItemTGI) {
                    isItemFound = ListOfTGIs.BinarySearch(DBPFTGI.ParseTGIString(rule.SearchItem)) >= 0;
                } else {
                    isItemFound = ListOfFileNames.BinarySearch(rule.SearchItem) >= 0;
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
                    _condDepsNotScanned ++;
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


        private string[] ImportFromGithub(string githubFilePath) {
            try {
                HttpClient client = new HttpClient();
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, githubFilePath);
                HttpResponseMessage response = client.Send(request);
                StreamReader reader = new StreamReader(response.Content.ReadAsStream());
                string content = reader.ReadToEnd();
                return content.Split('\n');
            }
            catch (Exception ex) {
                _runs.Add(new FormattedRun($"Error: {ex.Message}\r\n", RunType.RedMono));
                _runs.Add(new FormattedRun("Could not import rules from: ", RunType.RedStd));
                _runs.Add(new FormattedRun(githubFilePath + "\r\n", RunType.RedMono));
                return Array.Empty<string>();
            }
            
        }



        /// <summary>
        /// Move the files requested for removal to <see cref="ScriptOutputDirectory"/> and create the html summary document and undo batch script. 
        /// </summary>
        /// <param name="filesToRemove">List of files to remove</param>
        /// <param name="templateText">HTML template text</param>
        /// <param name="scriptPath">Path of the script used to create the backup list. Used in the HTML output file</param>
        /// <returns>The name of the output directory, which is the <see cref="ScriptOutputDirectory"/> plus the current timestamp.</returns>
        public string? BackupFiles(string templateText, string scriptPath = "") {
            if (ListOfFilesToRemove.Count == 0) {
                return null;
            }
            string outputDir = Path.Combine(BaseOutputDirectory, DateTime.Now.ToString("yyyyMMdd HHmmss"));
            string batchPath = Path.Combine(outputDir, "undo");
            SQLiteConnection db = DatabaseBuilder.CreateBackupdb(batchPath + ".db");
            Directory.CreateDirectory(outputDir);


            //Write batch undo file
            StringBuilder batchContents = new StringBuilder();
            foreach (string file in ListOfFilesToRemove) {
                string fname = Path.GetFileName(file);
                string archivePath = Path.Combine(outputDir, fname);
                if (File.Exists(file)) {
                    try {
                        if (!file.Contains("C:\\Windows")) {
                            File.Move(file, archivePath);
                        }
                    }
                    catch (IOException) {
                        //Catches error when trying to overwrite a file that already exists in backup (multiple of the same file were in the user's plugins in different folders. Still record each location so they can be moved back correctly.
                        File.Delete(archivePath);
                        File.Move(file, archivePath);
                    } finally {
                        batchContents.AppendLine((IsWindowsOS ? "copy" : "cp") + $" \"{fname}\" \"{file}\"");
                        db.Insert(new BackupItem(fname, file));
                    }
                }
            }
            if (IsWindowsOS) {
                File.WriteAllText(batchPath + ".bat", batchContents.ToString());
            } else {
                File.WriteAllText(batchPath + ".sh", "#!/bin/bash\n" + batchContents.ToString());
            }


            //Write HTML Template summary
            templateText = templateText.Replace("#SCRIPTNAME", Path.GetFileName(scriptPath));
            templateText = templateText.Replace("#COUNTFILES", ListOfFilesToRemove.Count.ToString());
            templateText = templateText.Replace("#FOLDERPATH", outputDir);
            templateText = templateText.Replace("#HELPDOC", "https://github.com/noah-severyn/SC4Cleanitol/wiki");
            templateText = templateText.Replace("#LISTOFFILES", string.Join("<br/>", ListOfFilesToRemove));
            templateText = templateText.Replace("#DATETIME", DateTime.Now.ToString("dd MMM yyyy HH:mm"));
            File.WriteAllText(Path.Combine(outputDir, "CleanupSummary.html"), templateText);

            return outputDir;
        }



        /// <summary>
        /// Export the scanned TGIs to a CSV document in the assigned Cleanitol folder.
        /// </summary>
        /// <param name="exportFolder">Folder path to create the export file in.</param>
        /// <param name="exportType">Export file type. Determines export file's extension.</param>
        /// <param name="tgisToExport">List of TGIs to export</param>
        /// <returns>The path of the created export file.</returns>
        public string ExportTGIs(string exportFolder, ExportType exportType, List<TGI> tgisToExport) {
            string filename = Path.Combine(exportFolder, "ScannedTGIs", exportType == ExportType.CSV ? ".csv" : ".db");
            try {
                if (File.Exists(exportFolder + ".csv")) {
                    File.Delete(exportFolder + ".csv");
                }
                if (File.Exists(exportFolder + ".db")) {
                    File.Delete(exportFolder + ".db");
                }
            }
            catch (Exception ex) {
                _runs.Add(new FormattedRun($"Error: {ex.Message}\r\n", RunType.RedMono));
                _runs.Add(new FormattedRun("Could not delete the TGI database at: ", RunType.RedStd));
                _runs.Add(new FormattedRun(filename + "\r\n", RunType.RedMono));
            }
            

            if (exportType == ExportType.CSV) {
                StringBuilder list = new StringBuilder("Type,Group,Instance,\r\n");
                foreach (TGI tgi in tgisToExport) {
                    list.AppendLine(tgi.ToString());
                }
                File.WriteAllText(filename, list.ToString());
            } 
            else {
                SQLiteConnection db = DatabaseBuilder.CreateTGIdb(filename);
                List<TGIItem> tgiItems = new List<TGIItem>();
                foreach (TGI tgi in tgisToExport) {
                    tgiItems.Add(new TGIItem(tgi));
                }
                db.InsertAll(tgiItems);
            }
            return filename;
        }



        /// <summary>
        /// Create a new Cleanitol file containing all files in the chosen folder and its subfolders.
        /// </summary>
        /// <param name="folderPath">Folder containing files to add to the script</param>
        /// <param name="scriptPath">Full path of Cleanitol file to create</param>
        public static void CreateCleanitolList(string folderPath, string scriptPath) {
            List<string> newRules = new List<string>();
            DirectoryInfo di = new DirectoryInfo(folderPath);
            FileInfo[] files = di.GetFiles("*", SearchOption.AllDirectories);
            foreach (FileInfo file in files) {
                newRules.Add(">#" + file.Name);
                newRules.AddRange(File.ReadAllLines(file.FullName));
                newRules.Add("");
            }
            File.WriteAllLines(scriptPath, newRules);
        }
    }
}