using System.ComponentModel;
using System.Reflection;
using System.Text;
using csDBPF;

namespace SC4Cleanitol {
    public class CleanitolEngine {
        /// <summary>
        /// Location of the plugins folder found in the user's Documents folder.
        /// </summary>
        public string UserPluginsDirectory { get; set; }
        /// <summary>
        /// Location of the plugins folder found in the game install directory.
        /// </summary>
        public string SystemPluginsDirectory { get; set; }
        /// <summary>
        /// Location files will be removed to and the output summary will be saved to.
        /// </summary>
        public string CleanitolOutputDirectory { get; set; }


        /// <summary>
        /// Full path to the cleanitol script.
        /// </summary>
        public string ScriptPath { get; set; }
        /// <summary>
        /// List of script rules. Updated when <see cref="RunScript"/> is called.
        /// </summary>
        public List<string> ScriptRules { get; private set; }


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

        private int highestPctReached;


        //presume that user and system plugin folders exist.
        public CleanitolEngine(string userPluginsDirectory, string systemPluginsDirectory, string cleanitolOutputDirectory) {
            if (!Directory.Exists(userPluginsDirectory)) {
                throw new DirectoryNotFoundException();
            } else {
                UserPluginsDirectory = userPluginsDirectory;
            }
            if (!Directory.Exists(systemPluginsDirectory)) {
                throw new DirectoryNotFoundException();
            } else {
                SystemPluginsDirectory = systemPluginsDirectory;
            }
            if (!Directory.Exists(cleanitolOutputDirectory)) {
                throw new DirectoryNotFoundException();
            } else {
                CleanitolOutputDirectory = cleanitolOutputDirectory;
            }

            ScriptPath = string.Empty;
            ScriptRules = new List<string>();
            ListOfFiles = new List<string>();
            ListOfFileNames = new List<string>();
            ListOfTGIs = new List<string>();
            FilesToRemove = new List<string>();
        }


        //worker example: https://learn.microsoft.com/en-us/previous-versions/visualstudio/visual-studio-2010/waw3xexc(v=vs.100)?redirectedfrom=MSDN
        public List<GenericRun> RunScript(bool updateTGIdatabase, bool includeSystemPlugins, BackgroundWorker worker, DoWorkEventArgs e) {
            Reset();
            List<GenericRun> runs = new List<GenericRun>();
            if (ScriptPath is null) {
                runs.Add(new GenericRun("No script selected.", RunType.BlueMono));
                return runs;
            }
            if (!Directory.Exists(ScriptPath)) {
                runs.Add(new GenericRun($"The script at < {ScriptPath} > cannot be found. Was the file moved or renamed?", RunType.RedMono));
                return runs;
            }

            //Fill File List
            ScriptRules = File.ReadAllLines(ScriptPath).ToList();
            ListOfFiles = Directory.EnumerateFiles(UserPluginsDirectory, "*", SearchOption.AllDirectories);
            if (includeSystemPlugins) {
                ListOfFiles = ListOfFiles.Concat(Directory.EnumerateFiles(SystemPluginsDirectory));
            }
            ListOfFileNames = ListOfFiles.AsParallel().Select(fileName => Path.GetFileName(fileName));

            //Fill TGI list if required
            if (!worker.CancellationPending && updateTGIdatabase) {
                int totalfiles = ListOfFiles.Count();
                double filesScanned = 0;
                ListOfTGIs.Clear();

                foreach (string filepath in ListOfFiles) {
                    filesScanned++;
                    if (DBPFUtil.IsValidDBPF(filepath)) {
                        DBPFFile dbpf = new DBPFFile(filepath);
                        ListOfTGIs.AddRange(dbpf.GetTGIs().AsParallel().Select(tgi => tgi.ToStringShort()));
                    }

                    int pctComplete = (int) filesScanned / totalfiles * 100;
                    if (pctComplete > highestPctReached) {
                        highestPctReached = pctComplete;
                        worker.ReportProgress(pctComplete);
                    }
                }
            } else {
                e.Cancel = true;
            }

            //TODO - write TGI list to DB for local storage?


            //Evaluate script and report results
            runs.Add(new GenericRun("-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-\r\n", RunType.BlackMono));
            runs.Add(new GenericRun("    R E P O R T   S U M M A R Y    \r\n", RunType.BlackMono));
            runs.Add(new GenericRun("-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-\r\n", RunType.BlackMono));
            for (int idx = 0; idx < ScriptRules.Count; idx++) {
                runs.AddRange(EvaluateRule(ScriptRules[idx]));
            }
            runs.Insert(3, new GenericRun($"{FilesToRemove.Count} files to remove.\r\n", RunType.BlackMono));
            runs.Insert(4, new GenericRun($"{CountDepsFound}/{CountDepsScanned} dependencies found.\r\n", RunType.BlueMono));
            runs.Insert(5, new GenericRun($"{CountDepsMissing}/{CountDepsScanned} dependencies missing.\r\n", RunType.RedMono));
            runs.Insert(6, new GenericRun("-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-\r\n\r\n", RunType.BlackMono));

            return runs;
        }


        private void UpdateTGIDatabase() {

        }

        /// <summary>
        /// Reset the script scan results. Run each time before a script is exectuted.
        /// </summary>
        private void Reset() {
            CountDepsFound = 0;
            CountDepsMissing = 0;
            CountDepsScanned = 0;
            FilesToRemove.Clear();
        }



        /// <summary>
        /// Evaluate a rule and return the outcome.
        /// </summary>
        /// <param name="ruleText">Rule to evaluate</param>
        /// <param name="verboseOutput">Show verbose output or not</param>
        /// <returns></returns>
        public List<GenericRun> EvaluateRule(string ruleText, bool verboseOutput = true) {
            ScriptRule.RuleType result = ScriptRule.ParseRuleType(ruleText);
            switch (result) {
                case ScriptRule.RuleType.Removal:
                    return EvaluateRemovalRule(ruleText, verboseOutput);

                case ScriptRule.RuleType.ConditionalDependency:
                case ScriptRule.RuleType.Dependency:
                    return EvaluateDependencyRule(ruleText, verboseOutput);

                case ScriptRule.RuleType.UserComment:
                    return new List<GenericRun> { new GenericRun(ruleText, RunType.GreenStd) };

                case ScriptRule.RuleType.UserCommentHeading:
                    return new List<GenericRun> { new GenericRun(ruleText, RunType.BlackHeading) };

                case ScriptRule.RuleType.ScriptComment:
                default:
                    return new List<GenericRun>();

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

                runs.Add(new GenericRun(rule.SourceName == "" ? rule.SourceURL : rule.SourceName, RunType.Hyperlink));
                runs.Add(new GenericRun("\r\n"));
                CountDepsMissing++;
            } else if (isConditionalFound && isItemFound && verboseOutput) {
                runs.Add(new GenericRun(rule.SearchItem, RunType.BlueStd));
                runs.Add(new GenericRun(" was located." + "\r\n", RunType.BlackStd));
                CountDepsFound++;
            }

            CountDepsScanned++;
            return runs;
        }


        /// <summary>
        /// Move the files requested for removal to an external location and create <c>undo.bat</c> and <c>CleanupSummary.html</c> files in the location.
        /// </summary>
        public void BackupFiles() {
            string outputDir = Path.Combine(CleanitolOutputDirectory, DateTime.Now.ToString("yyyyMMdd HHmmss"));
            StringBuilder batchFile = new StringBuilder();
            Directory.CreateDirectory(outputDir);

            //Write batch undo file
            foreach (string file in FilesToRemove) {
                File.Move(file, Path.Combine(outputDir, Path.GetFileName(file)));
                batchFile.AppendLine("copy \"" + Path.GetFileName(file) + "\" \"..\\..\\Plugins\\" + Path.GetFileName(file));
            }
            File.WriteAllText(Path.Combine(outputDir, "undo.bat"), batchFile.ToString());

            //Write Summary HTML File (https://stackoverflow.com/a/3314213)
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "SC4CleanitolWPF.SummaryTemplate.html";
            Stream? stream = assembly.GetManifestResourceStream(resourceName);
            if (stream is not null) {
                StreamReader reader = new StreamReader(stream);

                string summarytemplate = reader.ReadToEnd();
                summarytemplate = summarytemplate.Replace("#COUNTFILES", FilesToRemove.Count.ToString());
                summarytemplate = summarytemplate.Replace("#FOLDERPATH", outputDir);
                summarytemplate = summarytemplate.Replace("#HELPDOC", "https://github.com/noah-severyn/SC4Cleanitol/wiki"); //TODO - input path to help document
                summarytemplate = summarytemplate.Replace("#DATETIME", DateTime.Now.ToString("dd MMM yyyy HH:mm"));
                File.WriteAllText(Path.Combine(outputDir, "CleanupSummary.html"), summarytemplate);
            }
        }



        /// <summary>
        /// Export the scanned TGIs to a CSV document in the root user plugins folder.
        /// </summary>
        public void ExportTGIs() {
            StringBuilder list = new StringBuilder("Type,Group,Instance");
            foreach (string tgi in ListOfFiles) {
                list.AppendLine(tgi);
            }

            File.WriteAllText(Path.Combine(UserPluginsDirectory, "ScannedTGIs.csv"), list.ToString());
        }
    }
}