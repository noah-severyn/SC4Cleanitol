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
        /// Path to the cleanitol script to run.
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



        /// <summary>
        /// Cound of dependencies scanned.
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
        public IEnumerable<string> ListOfFiles { get; private set; }
        /// <summary>
        /// File names trimmed from <see cref="ListOfFiles"/>.
        /// </summary>
        public IEnumerable<string> ListOfFileNames { get; private set; }
        /// <summary>
        /// All TGIs scanned by the script in a comma separated format: <c>0x00000000, 0x00000000, 0x00000000</c>.
        /// </summary>
        public List<string> ListOfTGIs { get; private set; }
        /// <summary>
        /// Files found to be removed from the script.
        /// </summary>
        public List<string> FilesToRemove { get; private set; }


        private List<string> _scriptRules;
        private int highestPctReached;



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

            _scriptRules = new List<string>();
            ListOfFiles = new List<string>();
            ListOfFileNames = new List<string>();
            ListOfTGIs = new List<string>();
            FilesToRemove = new List<string>();
        }



        /// <summary>
        /// Execute the script and return the results of each rule.
        /// </summary>
        /// <param name="totalFiles">The Progress tracker for the total number of files.</param>
        /// <param name="progressFiles">The progress tracker for the current progress of scanned files.</param>
        /// <param name="progressTGIs">The progress tracker for the current progress of scanned TGIs.</param>
        /// <param name="updateTGIdatabase">Specify to rebuild the internal index of TGIs.</param>
        /// <param name="includeSystemPlugins">Specify to include the system plugins folder in the TGI scan or only the user plugins (recommended).</param>
        /// <param name="verboseOutput">Specify to return a message for every rule, or only return a message if an action needs to be taken.</param>
        /// <returns>A series of messages detailing the result of each script rule. The outer list respresents one line (rule), and the inner list holds one or more runs which are combined to form the message.</returns>
        public List<List<GenericRun>> RunScript(IProgress<int> totalFiles, IProgress<int> progressFiles, IProgress<int> progressTGIs, bool updateTGIdatabase, bool includeSystemPlugins, bool verboseOutput = true) {
            CountDepsFound = 0;
            CountDepsMissing = 0;
            CountDepsScanned = 0;
            FilesToRemove.Clear();
            List<List<GenericRun>> runs = new List<List<GenericRun>>();

            //Fill File List
            _scriptRules = File.ReadAllLines(_scriptPath).ToList();
            try {
                ListOfFiles = Directory.EnumerateFiles(UserPluginsDirectory, "*", SearchOption.AllDirectories);
                if (includeSystemPlugins) {
                    ListOfFiles = ListOfFiles.Concat(Directory.EnumerateFiles(SystemPluginsDirectory));
                }
            }
            catch (IOException) {

                return runs;
            }
            
            totalFiles.Report(ListOfFiles.Count());
            ListOfFileNames = ListOfFiles.AsParallel().Select(fileName => Path.GetFileName(fileName));

            //Fill TGI list if required
            if (updateTGIdatabase) {
                int totalfiles = ListOfFiles.Count();
                int filesScanned = 0;
                ListOfTGIs.Clear();

                foreach (string filepath in ListOfFiles) {
                    filesScanned++;
                    progressFiles.Report(filesScanned);

                    if (DBPFUtil.IsValidDBPF(filepath)) {
                        DBPFFile dbpf = new DBPFFile(filepath);
                        ListOfTGIs.AddRange(dbpf.GetTGIs().AsParallel().Select(tgi => tgi.ToStringShort()));
                        progressTGIs.Report(ListOfTGIs.Count);
                    }

                    int pctComplete = filesScanned / totalfiles * 100;
                    if (pctComplete > highestPctReached) {
                        highestPctReached = pctComplete;
                    }
                }
            }

            //TODO - write TGI list to DB for local storage?


            //Evaluate script and report results
            runs.Add(new List<GenericRun> { new GenericRun("-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-\r\n", RunType.BlackMono) } );
            runs.Add(new List<GenericRun> { new GenericRun("    R E P O R T   S U M M A R Y    \r\n", RunType.BlackMono) } );
            runs.Add(new List<GenericRun> { new GenericRun("-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-\r\n", RunType.BlackMono) });
            for (int idx = 0; idx < _scriptRules.Count; idx++) {
                runs.Add(EvaluateRule(_scriptRules[idx].Trim(), verboseOutput)); //TODO this is problematic when output to console because we are flattening out which runs belong to which rule
            }
            runs.Insert(3, new List<GenericRun> { new GenericRun($"{FilesToRemove.Count} files to remove.\r\n", RunType.BlackMono) });
            runs.Insert(4, new List<GenericRun> { new GenericRun($"{CountDepsFound}/{CountDepsScanned} dependencies found.\r\n", RunType.BlueMono) });
            runs.Insert(5, new List<GenericRun> { new GenericRun($"{CountDepsMissing}/{CountDepsScanned} dependencies missing.\r\n", RunType.RedMono) });
            runs.Insert(6, new List<GenericRun> { new GenericRun("-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-\r\n", RunType.BlackMono) });

            return runs;
        }



        /// <summary>
        /// Evaluate a rule and return the outcome.
        /// </summary>
        /// <remarks>
        /// This function can be used to run individual rules if you do not want to create a script file. Otherwise the <see cref="RunScript"/>  function is recommended to process rules from a script file. Ensure the rule is syntactically correct or unpredectible results can occur.
        /// </remarks>
        /// <param name="ruleText">Rule to evaluate</param>
        /// <param name="verboseOutput">Show verbose output or not</param>
        /// <returns></returns>
        private List<GenericRun> EvaluateRule(string ruleText, bool verboseOutput = true) {
            switch (ScriptRule.ParseRuleType(ruleText)) {
                case ScriptRule.RuleType.Removal:
                    return EvaluateRemovalRule(ruleText, verboseOutput);

                case ScriptRule.RuleType.ConditionalDependency:
                case ScriptRule.RuleType.Dependency:
                    return EvaluateDependencyRule(ruleText, verboseOutput);

                case ScriptRule.RuleType.UserComment:
                    if (ruleText.StartsWith('>')) {
                        return new List<GenericRun> { new GenericRun(ruleText.Substring(1) + "\r\n", RunType.GreenStd) };
                    } else {
                        return new List<GenericRun> { new GenericRun(ruleText + "\r\n", RunType.GreenStd) };
                    }

                case ScriptRule.RuleType.UserCommentHeading:
                    return new List<GenericRun> { new GenericRun("\r\n" + ruleText.Substring(2) + "\r\n", RunType.BlackHeading) };

                case ScriptRule.RuleType.ScriptComment:
                    return new List<GenericRun>();

                default:
                    return new List<GenericRun> { new GenericRun(ruleText + "\r\n", RunType.GreenStd) };

            }
        }



        private List<GenericRun> EvaluateRemovalRule(string ruleText, bool verboseOutput) {
            IEnumerable<string> matchingFiles = Directory.EnumerateFiles(UserPluginsDirectory, ruleText, SearchOption.AllDirectories);
            List<GenericRun> runs = new List<GenericRun>();
            if (!matchingFiles.Any() && verboseOutput) {
                runs.Add(new GenericRun(ruleText, RunType.BlueStd));
                runs.Add(new GenericRun(" not present." + "\r\n", RunType.BlackStd));
            } else {
                string filename;
                foreach (string file in matchingFiles) {
                    filename = Path.GetFileName(file);
                    //Make a special exception for the png images used for the in-game grid (the one that appears in the background when you play city tiles)
                    if (filename == "Background3D0.png" || filename == "Background3D1.png" || filename == "Background3D2.png" || filename == "Background3D3.png" || filename == "Background3D4.png") {
                        break;
                    }
                    runs.Add(new GenericRun(ruleText, RunType.BlueStd));
                    runs.Add(new GenericRun(" (" + filename + ")", RunType.BlueMono));
                    runs.Add(new GenericRun(" found in ", RunType.BlackStd));
                    runs.Add(new GenericRun(Path.GetDirectoryName(file) + "\r\n", RunType.RedStd));
                    FilesToRemove.Add(file);
                }
            }
            return runs;
        }

        private List<GenericRun> EvaluateDependencyRule(string ruleText, bool verboseOutput) {
            ScriptRule.DependencyRule rule = new ScriptRule.DependencyRule(ruleText);
            List<GenericRun> runs = new List<GenericRun>();

            if (rule.IsUnchecked) {
                runs.Add(new GenericRun("[Unchecked dependency]:", RunType.BlueStd));
                runs.Add(new GenericRun(" " + rule.SearchItem, RunType.RedStd));
                runs.Add(new GenericRun(". Download from: ", RunType.BlackStd));
                runs.Add(new GenericRun(rule.SourceName == "" ? rule.SourceURL : rule.SourceName, RunType.Hyperlink, rule.SourceURL));
                return runs;
            }

            bool isConditionalFound = true;
            if (rule.ConditionalItem != "") {
                if (rule.IsConditionalItemTGI) {
                    isConditionalFound = ListOfTGIs.AsParallel().Any(tgi => tgi.Contains(rule.ConditionalItem));
                } else {
                    isConditionalFound = ListOfFiles.AsParallel().Any(tgi => tgi.Contains(rule.ConditionalItem));
                }
            }

            bool isItemFound = false;
            if (isConditionalFound) {
                if (rule.IsSearchItemTGI) {
                    isItemFound = ListOfTGIs.AsParallel().Any(r => r.Contains(rule.SearchItem));
                } else {
                    isItemFound = ListOfFiles.AsParallel().Any(r => r.Contains(rule.SearchItem));
                }
            }

            if (isConditionalFound && !isItemFound) {
                runs.Add(new GenericRun("Missing: ", RunType.RedMono));
                runs.Add(new GenericRun(rule.SearchItem, RunType.RedStd));
                runs.Add(new GenericRun(" is missing. Download from: ", RunType.BlackStd));
                runs.Add(new GenericRun(rule.SourceName == "" ? rule.SourceURL : rule.SourceName, RunType.Hyperlink, rule.SourceURL));
                //runs.Add(new GenericRun("\r\n"));
                CountDepsMissing++;
            } else if (isConditionalFound && isItemFound && verboseOutput) {
                runs.Add(new GenericRun(rule.SearchItem, RunType.BlueStd));
                runs.Add(new GenericRun(" was located.\r\n", RunType.BlackStd));
                CountDepsFound++;
            }

            CountDepsScanned++;
            return runs;
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

            //Write HTML Tempalte summary
            templateText = templateText.Replace("#COUNTFILES", FilesToRemove.Count.ToString());
            templateText = templateText.Replace("#FOLDERPATH", outputDir);
            templateText = templateText.Replace("#HELPDOC", "https://github.com/noah-severyn/SC4Cleanitol/wiki"); //TODO - input path to help document
            templateText = templateText.Replace("#LISTOFFILES", string.Join("<br/>", FilesToRemove));
            templateText = templateText.Replace("#DATETIME", DateTime.Now.ToString("dd MMM yyyy HH:mm"));
            File.WriteAllText(Path.Combine(outputDir, "CleanupSummary.html"), templateText);

            ScriptOutputDirectory = outputDir;
        }



        /// <summary>
        /// Export the scanned TGIs to a CSV document in the assigned Cleanitol folder.
        /// </summary>
        public void ExportTGIs() {
            StringBuilder list = new StringBuilder("Type,Group,Instance");
            foreach (string tgi in ListOfFiles) {
                list.AppendLine(tgi);
            }

            File.WriteAllText(Path.Combine(ScriptOutputDirectory, "ScannedTGIs.csv"), list.ToString());
        }


        /// <summary>
        /// Create a new Cleanitol file containing all files in the chosen folder and its subfolders.
        /// </summary>
        /// <param name="folderPath">Folder containing files to add to the script</param>
        /// <param name="scriptPath">Full path of Cleanitol file to create</param>
        public static void CreateCleanitolList(string folderPath, string scriptPath) {
            List<string> fileNames = new List<string>();
            DirectoryInfo di = new DirectoryInfo(folderPath);
            FileInfo[] files = di.GetFiles();
            foreach (FileInfo file in files) {
                fileNames.Add(file.Name + file.Extension);
            }
            File.WriteAllLines(scriptPath, fileNames);
        }
    }
}