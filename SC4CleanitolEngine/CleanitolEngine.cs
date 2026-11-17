using System.Data;
using System.Text;
using csDBPF;

namespace SC4CleanitolEngine {
    /// <summary>
    /// Contains the functions and logic to process Cleanitol rules and operate on a user's plugins folder.
    /// </summary>
    public class CleanitolEngine {

        /// <summary>
        /// Stores metrics about the status of the current scan.
        /// </summary>
        /// <param name="filesProcessed">Count of files processed so far.</param>
        /// <param name="filesTotal">Count of all files in the specified plugins folder(s).</param>
        /// <param name="tgisProcessed">Count of TGIs processed so far.</param>
        public record class CleanitolProgress(int filesProcessed, int filesTotal, int tgisProcessed) {
            public int FilesProcessed { get; internal set; } = filesProcessed;
            public int FilesTotal { get; internal set; } = filesTotal;
            public int TgisProcessed { get; internal set; } = tgisProcessed;
        }
        /// <summary>
        /// Summary metrics describing the outcome of the Cleanitol scan.
        /// </summary>
        public record ScanResult {
            public ScanResult(Dictionary<string, string> scannedFiles, List<KeyValuePair<string, string>> scannedTgis, List<Finding> findings) {
                ScannedFiles = scannedFiles;
                ScannedTGIs = scannedTgis;
                Findings = findings;
            }

            /// <summary>
            /// Files found from the scan. Key = file path, Value = file name.
            /// </summary>
            public IReadOnlyDictionary<string, string> ScannedFiles { get; private set; }
            /// <summary>
            /// File and TGI pairs discovered during scanning.
            /// </summary>
            public IReadOnlyList<KeyValuePair<string, string>> ScannedTGIs { get; private set; }
            /// <summary>
            /// Findings identified during the scan.
            /// </summary>
            public IReadOnlyList<Finding> Findings { get; private set; } = [];
        }

        public record RunResult {
            public RunResult(string scriptName, List<Finding> findings, List<string>? filesToRemove = null) {
                ScriptName = scriptName;
                Findings = findings;
                FilesToRemove = filesToRemove ?? [];
            }

            /// <summary>
            /// Gets the Cleanitol script name.
            /// </summary>
            public string ScriptName { get; private set; } = string.Empty;
            /// <summary>
            /// Findings identified during the run.
            /// </summary>
            public IReadOnlyList<Finding> Findings { get; private set; } = [];
            /// <summary>
            /// Count of dependencies found during the run.
            /// </summary>
            //public int DependenciesFound { get; private set; }
            /// <summary>
            /// Count of dependencies missing during the run.
            /// </summary>
            //public int DependenciesMissing { get; private set; }
            /// <summary>
            /// Count of dependencies skipped during the run.
            /// </summary>
            //public int DependenciesSkipped { get; private set; }

            public IReadOnlyList<string> FilesToRemove { get; private set; } = [];
        }


        /// <summary>
        /// Describes a finding or event as a result of a scan or run.
        /// </summary>
        public record Finding {
            public FindingSeverity Severity { get; internal set; }
            public string Script { get; internal set; }
            public string Item { get; internal set; }
            public string Message { get; internal set; }
            public Exception Error { get; internal set; }
            public Link Link { get; internal set; }
        }

        public enum FindingSeverity {
            /// <summary>
            /// A comment or heading always shown to the user.
            /// </summary>
            Output,
            /// <summary>
            /// Describes the evaluation if no action was taken.
            /// </summary>
            Info,
            /// <summary>
            /// Describes the evaluation of a recommended or important user action.
            /// </summary>
            Warning,
            /// <summary>
            /// Describes the evaluation of a user action that *must* be taken.
            /// </summary>
            Error,
        }

        public record Link {
            public string Path { get; internal set; }
            public string Name { get; internal set; }
        }


        

        /// <summary>
        /// Location files will be removed to and the output summary will be saved to.
        /// </summary>
        public string OutputDirectory { get; set; } = string.Empty;
        /// <summary>
        /// A list of folders to scan, which may include user plugins, system plugins, and/or additional folders.
        /// </summary>
        public List<string> PluginFolders { get; set; } = [];

        private ScanResult? _scan;

        /// <summary>
        /// Parse the specified folders to generate an internal index of files and optionally, TGIs. Required before any other actions may be taken.
        /// </summary>
        /// <param name="progress">Progress status of the scan.</param>
        /// <param name="parseTGIs">Parse the TGIs out of each file found in <see cref="PluginFolders"/>.</param>
        public ScanResult Scan(IProgress<CleanitolProgress>? progress = null, bool parseTGIs = false) {
            var scannedFiles = new Dictionary<string, string>();
            var scannedTgis = new List<KeyValuePair<string, string>>();
            var findings = new List<Finding>();

            foreach (string folder in PluginFolders) {
                var files = Directory.EnumerateFiles(folder, "*", new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = true });
                foreach (string file in files) {
                    if (scannedFiles.TryAdd(file, Path.GetFileName(file))) {
                        progress?.Report(new CleanitolProgress(scannedFiles.Count, scannedFiles.Count, 0));
                    }
                }
            }

            if (parseTGIs) {
                int filesScanned = 0;

                foreach (string filepath in scannedFiles.Keys) {
                    filesScanned++;
                    progress?.Report(new CleanitolProgress(filesScanned, scannedFiles.Count, 0));

                    try {
                        if (filepath.IsDBPF()) {
                            DBPFFile dbpf = new DBPFFile(filepath);
                            foreach (var tgi in dbpf.ListOfTGIs) {
                                scannedTgis.Add(new KeyValuePair<string, string>(filepath, tgi.ToString()));
                            }
                            progress?.Report(new CleanitolProgress(filesScanned, scannedFiles.Count, scannedTgis.Count));
                        }
                    }
                    catch (Exception ex) {
                        findings.Add(new Finding() {
                            Severity = FindingSeverity.Error,
                            Script = "N/A",
                            Item = filepath,
                            Message = "Could not open file.",
                            Error = ex
                        });
                    }
                }
            } else {
                progress?.Report(new CleanitolProgress(scannedFiles.Count, scannedFiles.Count, 0));
            }

            _scan = new ScanResult(scannedFiles, scannedTgis, findings);
            return _scan;
        }

        /// <summary>
        /// Process a collection of rules as if they were a script.
        /// </summary>
        /// <param name="scriptName">Name of the script.</param>
        /// <param name="rules">A collection of rules to run.</param>
        /// <param name="ignoreSc4pac">Ignore files found in an sc4pac folder. Default is <see langword="true"/>.</param>
        /// <returns>A new <see cref="CleanitolResult"/> containing only the findings of this run. Also available as <see cref="Results"/> until the next run.</returns>
        public RunResult Run(string scriptName, IEnumerable<string> rules, bool ignoreSc4pac = true) {
            if (_scan is null) {
                throw new InvalidOperationException("Scan must be completed before running a script.");
            }
            var findings = new List<Finding>();
            var filesToRemove = new List<string>();
            foreach (var rule in rules) {
                var res = ProcessRule(rule.Trim(), scriptName, ignoreSc4pac);
                findings.AddRange(res.findings);
                filesToRemove.AddRange(res.filesToRemove);
            }
            return new RunResult(scriptName, findings, filesToRemove);
        }


        /// <summary>
        /// Run the specified script.
        /// </summary>
        /// <param name="scriptPath">Path of the script to run. May either be a local file path or a GitHub raw url.</param>
        /// <param name="ignoreSc4pac">Ignore files found in an sc4pac folder. Default is <see langword="true"/>.</param>
        /// <returns>A new <see cref="CleanitolResult"/> containing only the findings of this run. Also available as <see cref="Results"/> until the next run.</returns>
        public RunResult Run(string scriptPath, bool ignoreSc4pac = true) {
            if (_scan is null) {
                throw new InvalidOperationException("Scan must be completed before running a script.");
            }
            var scriptName = Path.GetFileName(scriptPath);
            var findings = new List<Finding>();
            List<string> rawRules;
            if (scriptPath.StartsWith("https://raw.githubusercontent.com")) {
                rawRules = ImportScriptFromGithub(scriptPath, ref findings);
            } else {
                rawRules = File.ReadAllLines(scriptPath).ToList();
            }

            //Expand remote script imports in place, in order, guarding against cycles
            var rules = ExpandImports(rawRules, ref findings, [scriptPath]);

            var filesToRemove = new List<string>();
            foreach (var rule in rules) {
                var res = ProcessRule(rule.Trim(), scriptName, ignoreSc4pac);
                findings.AddRange(res.findings);
                filesToRemove.AddRange(res.filesToRemove);
            }
            return new RunResult(scriptName, findings, filesToRemove);
        }

        private static List<string> ExpandImports(List<string> rules, ref List<Finding> findings, HashSet<string> visited) {
            var expanded = new List<string>();
            foreach (var rule in rules) {
                expanded.Add(rule);
                var trimmed = rule.Trim();
                if (trimmed.StartsWith("@https://raw.githubusercontent.com")) {
                    var url = trimmed.Substring(1);
                    if (visited.Add(url)) {
                        expanded.AddRange(ExpandImports(ImportScriptFromGithub(url, ref findings), ref findings, visited));
                    }
                }
            }
            return expanded;
        }

        private static List<string> ImportScriptFromGithub(string githubFilePath, ref List<Finding> findings) {
            try {
                HttpClient client = new HttpClient();
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, githubFilePath);
                HttpResponseMessage response = client.Send(request);
                StreamReader reader = new StreamReader(response.Content.ReadAsStream());
                string content = reader.ReadToEnd();
                return content.Split('\n').ToList();
            }
            catch (Exception ex) {
                findings.Add(new Finding() {
                    Severity = FindingSeverity.Error,
                    Script = Path.GetFileName(githubFilePath),
                    Item = githubFilePath,
                    Message = string.Empty,
                    Error = ex
                });
                return [];
            }
        }


        /// <summary>
        /// Process a Cleanitol rule, optionally ignoring any files found in an sc4pac folder.
        /// </summary>
        private (List<Finding> findings, List<string> filesToRemove) ProcessRule(string rule, string scriptName, bool ignoreSc4pac) {
            var findings = new List<Finding>();
            var filesToRemove = new List<string>();
            var type = ScriptRule.Parse(rule);
            switch (type) {
                case ScriptRule.Type.Invalid:
                    findings.Add(new Finding() {
                        Severity = FindingSeverity.Error,
                        Script = scriptName,
                        Item = rule,
                        Message = "is an invalid rule. Check syntax."
                    });
                    break;

                case ScriptRule.Type.Removal:
                    var res = ProcessRemovalRule(rule, scriptName, ignoreSc4pac);
                    findings.AddRange(res.findings);
                    filesToRemove.AddRange(res.filesToRemove);
                    break;

                case ScriptRule.Type.ConditionalDependency:
                case ScriptRule.Type.Dependency:
                    findings.Add(ProcessDependencyRule(rule, scriptName));
                    break;

                case ScriptRule.Type.Comment:
                    findings.Add(new Finding() {
                        Severity = FindingSeverity.Output,
                        Script = scriptName,
                        Item = rule,
                        Message = rule.Substring(1)
                    });
                    break;

                case ScriptRule.Type.Heading:
                    findings.Add(new Finding() {
                        Severity = FindingSeverity.Output,
                        Script = scriptName,
                        Item = rule,
                        Message = rule.TrimStart('#')
                    });
                    break;

                case ScriptRule.Type.HiddenComment:
                case ScriptRule.Type.Import:
                default:
                    return ([], []);
            }
            return (findings, filesToRemove);
        }


        private (List<Finding> findings, List<string> filesToRemove) ProcessRemovalRule(string rule, string scriptName, bool ignoreSc4pac) {
            var findings = new List<Finding>();
            var filesToRemove = new List<string>();
            ScanResult scan = _scan ?? throw new InvalidOperationException("Scan must be completed before running a script.");

            rule = rule.Replace("*", string.Empty);
            List<string> matchingFiles = scan.ScannedFiles.Keys
                .Where(item => item.EndsWith(rule) && (!ignoreSc4pac || !IsSc4pacFile(item)))
                .AsParallel()
                .ToList();

            if (matchingFiles.Count != 0) {
                foreach (string file in matchingFiles) {
                    var filename = Path.GetFileName(file);
                    //Make a special exception for the PNG images used for the in-game grid (the one that appears in the background when you play city tiles)
                    if (filename == "Background3D0.png" || filename == "Background3D1.png" || filename == "Background3D2.png" || filename == "Background3D3.png" || filename == "Background3D4.png") {
                        continue;
                    }
                    filesToRemove.Add(file);
                    findings.Add(new Finding() {
                        Severity = FindingSeverity.Warning,
                        Script = scriptName,
                        Item = rule,
                        Message = $"was found in {Path.GetDirectoryName(file)}."
                    });
                }
            } else {
                findings.Add(new Finding() {
                    Severity = FindingSeverity.Info,
                    Script = scriptName,
                    Item = rule,
                    Message = "is not present."
                });
            }
            return (findings, filesToRemove);
        }


        private Finding ProcessDependencyRule(string rule, string scriptName) {
            ScanResult scan = _scan ?? throw new InvalidOperationException("Scan must be completed before running a script.");
            var dr = new ScriptRule.DependencyRule(rule);

            if (dr.IsUnchecked) {
                return new Finding() {
                    Severity = FindingSeverity.Error,
                    Script = scriptName,
                    Item = rule,
                    Message = " is an unchecked dependency (no outputPath provided). Validate manually. Download at: #url#",
                    Link = new Link() { Name = dr.LinkName, Path = dr.LinkUrl },
                };
            }

            bool isConditionalFound = false;
            if (dr.IsConditional) {
                if (dr.ConditionalItem.StartsWith("0x")) {
                    //isConditionalFound = _allTgis.BinarySearch(DBPFTGI.ParseTGIString(dr.ConditionalItem)) >= 0;
                    throw new NotImplementedException("Temporarily removed");
                } else {
                    isConditionalFound = scan.ScannedFiles.Values.Contains(dr.SearchItem);
                }
            }

            bool isItemFound = false;
            if (isConditionalFound || !dr.IsConditional) {
                if (dr.SearchItem.StartsWith("0x")) {
                    //isItemFound = _allTgis.BinarySearch(DBPFTGI.ParseTGIString(dr.SearchItem)) >= 0;
                    throw new NotImplementedException("Temporarily removed");
                } else {
                    isItemFound = scan.ScannedFiles.Values.Contains(dr.SearchItem);
                }
            }

            if (!dr.IsConditional) {
                if (isItemFound) {
                    //_dependenciesFound++;
                    return new Finding() {
                        Severity = FindingSeverity.Info,
                        Script = scriptName,
                        Item = dr.SearchItem,
                        Message = " was found.",
                    };
                } else {
                    //_dependenciesMissing++;
                    return new Finding() {
                        Severity = FindingSeverity.Error,
                        Script = scriptName,
                        Item = dr.SearchItem,
                        Message = " is missing. Download at: #url#",
                        Link = new Link() { Name = dr.LinkName, Path = dr.LinkUrl },
                    };
                }
            } else {
                if (isConditionalFound && isItemFound) {
                    //_dependenciesFound++;
                    return new Finding() {
                        Severity = FindingSeverity.Info,
                        Script = scriptName,
                        Item = dr.SearchItem,
                        Message = " was found.",
                    };
                } else if (isConditionalFound && !isItemFound) {
                    //_dependenciesMissing++;
                    return new Finding() {
                        Severity = FindingSeverity.Error,
                        Script = scriptName,
                        Item = dr.SearchItem,
                        Message = " is missing. Download at: #url#",
                        Link = new Link() { Name = dr.LinkName, Path = dr.LinkUrl },
                    };
                } else if (!isConditionalFound) {
                    //_dependenciesSkipped++;
                    return new Finding() {
                        Severity = FindingSeverity.Info,
                        Script = scriptName,
                        Item = dr.SearchItem,
                        Message = $" was skipped as {dr.ConditionalItem} was not found in {dr.LinkName ?? dr.LinkUrl}",
                    };
                } else {
                    return new Finding() {
                        Severity = FindingSeverity.Error,
                        Script = scriptName,
                        Item = dr.SearchItem,
                        Message = "Unexpected conditional dependency evaluation.",
                    };
                }
            }
        }

        private static bool IsSc4pacFile(string path) {
            //sc4pac installs each package into a folder whose name ends in ".sc4pac". Files in 075-my-plugins and 895-my-overrides do not contain this ans as such are treated like any other non-sc4pac file.
            return path.Split(Path.DirectorySeparatorChar).Any(segment => segment.EndsWith(".sc4pac", StringComparison.OrdinalIgnoreCase));
        }



        /// <summary>
        /// Move the files requested for removal to <see cref="ScriptOutputDirectory"/> and create the html summary document and undo batch script. 
        /// </summary>
        public static void BackupFiles(RunResult result, string outputDirectory) {
            if (result.FilesToRemove.Count == 0) {
                return;
            }
            string outputDir = Path.Combine(outputDirectory, DateTime.Now.ToString("yyyy-MM-dd_HHmmss"));
            Directory.CreateDirectory(outputDir);

            // Write batch undo file
            var isWindowsOS = System.Runtime.InteropServices.RuntimeInformation.OSDescription.Contains("Windows");
            string contents = string.Empty;

            foreach (string file in result.FilesToRemove) {
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
                    }
                    finally {
                        contents += (isWindowsOS ? "copy" : "cp") + $" \"{fname}\" \"{file}\"" + Environment.NewLine;
                    }
                }
            }
            if (isWindowsOS) {
                File.WriteAllText(Path.Combine(outputDir, "undo.bat"), contents.ToString());
            } else {
                File.WriteAllText(Path.Combine(outputDir, "undo.sh"), "#!/bin/bash" + Environment.NewLine + contents.ToString());
            }


            //Write HTML Template summary
            var assembly = typeof(CleanitolEngine).Assembly;
            using Stream stream = assembly.GetManifestResourceStream("SC4Cleanitol.Resources.SummaryTemplate.html")!;
            using StreamReader reader = new StreamReader(stream);
            string templateText = reader.ReadToEnd();
            templateText = templateText.Replace("#SCRIPTNAME", result.ScriptName);
            templateText = templateText.Replace("#COUNTFILES", result.FilesToRemove.Count.ToString());
            templateText = templateText.Replace("#FOLDERPATH", outputDir);
            templateText = templateText.Replace("#HELPDOC", "https://github.com/noah-severyn/SC4Cleanitol/wiki");
            templateText = templateText.Replace("#LISTOFFILES", string.Join("<br/>", result.FilesToRemove));
            templateText = templateText.Replace("#DATETIME", DateTime.Now.ToString("dd MMM yyyy HH:mm"));
            File.WriteAllText(Path.Combine(outputDir, "CleanupSummary.html"), templateText);
        }



        /// <summary>
        /// Export file type.
        /// </summary>
        public enum ExportType {
            Csv,
            SQLite
        }

        /// <summary>
        /// Export the scanned TGIs a document in the specified format.
        /// </summary>
        /// <param name="fileType">Export file type. Default is <see cref="ExportType.Csv"/>.</param>
        /// <returns>The full path of the created export file.</returns>
        public string ExportTGIs(ScanResult result, ExportType fileType = ExportType.Csv) {
            string outputPath = Path.Combine(OutputDirectory, "ScannedTGIs " + DateTime.Now.ToString("yyyy-MM-dd_HHmmss"), fileType == ExportType.Csv ? ".csv" : ".db");

            if (fileType == ExportType.Csv) {
                StringBuilder list = new StringBuilder("File,Type,Group,Instance,\r\n");
                foreach (var item in result.ScannedTGIs) {
                    list.AppendLine(item.Key + "," + item.Value.ToString().Replace(" ", string.Empty));
                }
                File.WriteAllText(outputPath, list.ToString());
            } else {
                DatabaseBuilder.CreateDb(outputPath, result.ScannedTGIs);
            }
            return outputPath;
        }



        /// <summary>
        /// Create a new Cleanitol file containing all files in the chosen folder and its subfolders.
        /// </summary>
        /// <param name="folderPath">Folder containing files to add to the script</param>
        /// <param name="scriptPath">Full path of Cleanitol file to create</param>
        public static void GenerateCleanitol(string folderPath, string scriptPath) {
            List<string> newRules = [];
            var di = new DirectoryInfo(folderPath);
            var files = di.GetFiles("*", SearchOption.AllDirectories);
            foreach (FileInfo file in files) {
                newRules.Add("#" + file.Name);
                newRules.AddRange(File.ReadAllLines(file.FullName));
            }
            File.WriteAllLines(scriptPath, newRules);
        }

        public static void CombineCleanitols(string folderPath) {
            throw new NotImplementedException();
        }
    }
}