using System.Data;
using System.Text;
using csDBPF;

namespace SC4CleanitolEngine {
    /// <summary>
    /// Contains the functions and logic to process Cleanitol rules and operate on a user's plugins folder.
    /// </summary>
    public class CleanitolEngine {
        /// <summary>
        /// Export file type.
        /// </summary>
        public enum ExportType {
            Csv,
            SQLite
        }

        /// <summary>
        /// Stores metrics about the status of the current scan.
        /// </summary>
        /// <param name="filesProcessed">Count of files processed so far.</param>
        /// <param name="filesTotal">Count of all files in the specified plugins folder(s).</param>
        /// <param name="tgisProcessed">Count of TGIs processed so far.</param>
        public record struct CleanitolProgress(int filesProcessed, int filesTotal, int tgisProcessed) {
            public int FilesProcessed = filesProcessed;
            public int FilesTotal = filesTotal;
            public int TgisProcessed = tgisProcessed;
        }
        /// <summary>
        /// Summary metrics describing the outcome resulting from the scan(s) and/or run(s).
        /// </summary>
        public record class CleanitolResult() {
            public int DependenciesFound { get; internal set; }
            public int DependenciesMissing { get; internal set; }
            public int DependenciesSkipped { get; internal set; }
            /// <summary>
            /// A collection of findings as a result of the scan(s) and/or run(s).
            /// </summary>
            public List<LogItem> Log { get; internal set; } = [];

            /// <summary>
            /// Reset the results and clear the log. Typically used when a new script is ran.
            /// </summary>
            public void Reset() {
                DependenciesFound = 0;
                DependenciesMissing = 0;
                DependenciesSkipped = 0;
                Log.Clear();
            }
        }


        /// <summary>
        /// Describes a finding or event as a result of a scan or run.
        /// </summary>
        public struct LogItem {
            public LogLevel Level;
            public string Item;
            public string Message;
            public Exception Error;
            public Link Link;
        }

        public enum LogLevel {
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

        public record struct Link {
            public string Path;
            public string Name;
        }


        /// <summary>
        /// Location files will be removed to and the output summary will be saved to.
        /// </summary>
        public string OutputDirectory { get; set; } = string.Empty;
        /// <summary>
        /// A list of folders to scan, which may include user plugins, system plugins, and/or additional folders.
        /// </summary>
        public List<string> PluginFolders { get; set; } = [];
        public CleanitolResult Results { get; private set; } = new CleanitolResult();

        /// <summary>
        /// Key = file path, Value = file name
        /// </summary>
        private readonly Dictionary<string, string> _allFiles = [];
        private readonly List<FileTgiPair> _allTgis = [];
        private readonly List<string> _filesToRemove = [];
        private string _scriptPath = string.Empty;

        /// <summary>
        /// Parse the folders provided for files and optionally TGIs.
        /// </summary>
        /// <param name="progress">Progress status of the scan.</param>
        /// <param name="parseTGIs">Parse the TGIs out of each file found in <see cref="PluginFolders"/>.</param>
        public void Scan(IProgress<CleanitolProgress>? progress = null, bool parseTGIs = false) {
            foreach (string folder in PluginFolders) {
                var files = Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories);
                foreach (string file in files) {
                    _allFiles.Add(file, Path.GetFileName(file));
                }
            }
            progress?.Report(new CleanitolProgress(0, _allFiles.Count, 0));

            if (parseTGIs) {
                int filesScanned = 0;
                _allTgis.Clear();

                foreach (string filepath in _allFiles.Keys) {
                    filesScanned++;
                    progress?.Report(new CleanitolProgress(filesScanned, _allFiles.Count, 0));

                    try {
                        if (filepath.IsDBPF()) {
                            DBPFFile dbpf = new DBPFFile(filepath);
                            foreach (var tgi in dbpf.ListOfTGIs) {
                                _allTgis.Add(new FileTgiPair(filepath, tgi));
                            }
                            progress?.Report(new CleanitolProgress(filesScanned, _allFiles.Count, _allTgis.Count));
                        }
                    }
                    catch (Exception ex) {
                        Results.Log.Add(new LogItem() {
                            Level = LogLevel.Error,
                            Item = filepath,
                            Message = "Could not open file.",
                            Error = ex
                        });
                    }
                }
            } else {
                progress?.Report(new CleanitolProgress(_allFiles.Count, _allFiles.Count, 0));
            }
        }


        /// <summary>
        /// Run the specified script.
        /// </summary>
        /// <param name="scriptPath">Path of the script to run. May either be a local file path or a Github raw url.</param>
        /// <param name="resetResults">Reset the contents of <see cref="Results"/> before running the script. Default is <see langword="true"/>.</param>
        public void Run(string scriptPath, bool resetResults = true) {
            if (resetResults) {
                Results.Reset();
            };
            _scriptPath = scriptPath;
            List<string> rules = [];
            if (scriptPath.StartsWith("https://raw.githubusercontent.com")) {
                rules = ImportScriptFromGithub(scriptPath);
            } else {
                rules = File.ReadAllLines(scriptPath).ToList();
            }

            //Follow by inserting any rules from the remote script, if applicable
            var urls = rules.AsParallel().Where(item => item.Trim().StartsWith("@https://raw.githubusercontent.com"));
            foreach (string url in urls) {
                rules.InsertRange(rules.IndexOf(url) + 1, ImportScriptFromGithub(url.Trim().Substring(1)));
            }

            //TODO - write TGI list to DB for local storage?
            foreach (var rule in rules) {
                ProcessRule(rule.Trim());
            }
        }

        private List<string> ImportScriptFromGithub(string githubFilePath) {
            try {
                HttpClient client = new HttpClient();
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, githubFilePath);
                HttpResponseMessage response = client.Send(request);
                StreamReader reader = new StreamReader(response.Content.ReadAsStream());
                string content = reader.ReadToEnd();
                return content.Split('\n').ToList();
            }
            catch (Exception ex) {
                Results.Log.Add(new LogItem() {
                    Level = LogLevel.Error,
                    Item = githubFilePath,
                    Message = string.Empty,
                    Error = ex
                });
                return [];
            }
        }


        /// <summary>
        /// Process a Cleanitol rule.
        /// </summary>
        /// <param name="rule">Rule to process.</param>
        /// <param name="resetResults">Reset the contents of <see cref="Results"/> before processing the rule. Default is <see langword="false"/>.</param>
        /// <remarks>
        /// May be used to run a rule in isolation. Otherwise <see cref="Run(string)"/> is recommended to process multiple rules from a script file. Ensure the rule is syntactically correct or unpredictable results can occur.
        /// </remarks>
        public void ProcessRule(string rule, bool resetResults = false) {
            if (resetResults) {
                Results.Reset();
            };
            var type = ScriptRule.Parse(rule);
            switch (type) {
                case ScriptRule.Type.Invalid:
                    Results.Log.Add(new LogItem() {
                        Level = LogLevel.Error,
                        Item = rule,
                        Message = " is an invalid rule. Check syntax."
                    });
                    return;

                case ScriptRule.Type.Removal:
                    ProcessRemovalRule(rule);
                    return;

                case ScriptRule.Type.ConditionalDependency:
                case ScriptRule.Type.Dependency:
                    ProcessDependencyRule(rule);
                    return;

                case ScriptRule.Type.Comment:
                    Results.Log.Add(new LogItem() {
                        Level = LogLevel.Output,
                        Item = rule,
                        Message = rule.Substring(1)
                    });
                    return;

                case ScriptRule.Type.Heading:
                    Results.Log.Add(new LogItem() {
                        Level = LogLevel.Output,
                        Item = rule,
                        Message = rule.TrimStart('#')
                    });
                    return;

                case ScriptRule.Type.HiddenComment:
                case ScriptRule.Type.Import:
                default:
                    return;
            }
        }


        private void ProcessRemovalRule(string rule) {
            rule = rule.Replace("*", string.Empty); //May have a rule like "*.jpg" to remove all files of a given extension.
            var matchingFiles = _allFiles.Keys.AsParallel().Where(item => item.EndsWith(rule));

            if (matchingFiles.Any()) {
                foreach (string file in matchingFiles) {
                    var filename = Path.GetFileName(file);
                    //Make a special exception for the png images used for the in-game grid (the one that appears in the background when you play city tiles)
                    if (filename == "Background3D0.png" || filename == "Background3D1.png" || filename == "Background3D2.png" || filename == "Background3D3.png" || filename == "Background3D4.png") {
                        break;
                    }
                    Results.Log.Add(new LogItem() {
                        Level = LogLevel.Warning,
                        Item = rule,
                        Message = $"was found in {Path.GetDirectoryName(file)}."
                    });
                    _filesToRemove.Add(file);
                }
            } else {
                Results.Log.Add(new LogItem() {
                    Level = LogLevel.Info,
                    Item = rule,
                    Message = "is not present."
                });
            }
        }


        private void ProcessDependencyRule(string rule) {
            var dr = new ScriptRule.DependencyRule(rule);

            if (dr.IsUnchecked) {
                Results.Log.Add(new LogItem() {
                    Level = LogLevel.Error,
                    Item = rule,
                    Message = " is an unchecked dependency (no outputPath provided). Validate manually. Download at: #url#",
                    Link = new Link() { Name = dr.LinkName, Path = dr.LinkUrl },
                });
            }

            bool isConditionalFound = false;
            if (dr.IsConditional) {
                if (dr.ConditionalItem.StartsWith("0x")) {
                    //isConditionalFound = _allTgis.BinarySearch(DBPFTGI.ParseTGIString(dr.ConditionalItem)) >= 0;
                    throw new NotImplementedException("Temporarily removed");
                } else {
                    isConditionalFound = _allFiles.ContainsValue(dr.SearchItem);
                }
            }

            bool isItemFound = false;
            if (isConditionalFound || !dr.IsConditional) {
                if (dr.SearchItem.StartsWith("0x")) {
                    //isItemFound = _allTgis.BinarySearch(DBPFTGI.ParseTGIString(dr.SearchItem)) >= 0;
                    throw new NotImplementedException("Temporarily removed");
                } else {
                    isItemFound = _allFiles.ContainsValue(dr.SearchItem);
                }
            }

            if (!dr.IsConditional) {
                if (isItemFound) {
                    Results.DependenciesFound++;
                    Results.Log.Add(new LogItem() {
                        Level = LogLevel.Info,
                        Item = dr.SearchItem,
                        Message = " was found.",
                    });
                } else {
                    Results.DependenciesMissing++;
                    Results.Log.Add(new LogItem() {
                        Level = LogLevel.Error,
                        Item = dr.SearchItem,
                        Message = " is missing. Download at: #url#",
                        Link = new Link() { Name = dr.LinkName, Path = dr.LinkUrl },
                    });
                }
            } else {
                if (isConditionalFound && isItemFound) {
                    Results.DependenciesFound++;
                    Results.Log.Add(new LogItem() {
                        Level = LogLevel.Info,
                        Item = dr.SearchItem,
                        Message = " was found.",
                    });
                } else if (isConditionalFound && !isItemFound) {
                    Results.DependenciesMissing++;
                    Results.Log.Add(new LogItem() {
                        Level = LogLevel.Error,
                        Item = dr.SearchItem,
                        Message = " is missing. Download at: #url#",
                        Link = new Link() { Name = dr.LinkName, Path = dr.LinkUrl },
                    });
                } else if (!isConditionalFound) {
                    Results.DependenciesSkipped++;
                    Results.Log.Add(new LogItem() {
                        Level = LogLevel.Info,
                        Item = dr.SearchItem,
                        Message = $" was skipped as {dr.ConditionalItem} was not found in {dr.LinkName ?? dr.LinkUrl}",
                    });
                }
            }
        }



        /// <summary>
        /// Move the files requested for removal to <see cref="ScriptOutputDirectory"/> and create the html summary document and undo batch script. 
        /// </summary>
        public void BackupFiles() {
            if (_filesToRemove.Count == 0) {
                return;
            }
            string outputDir = Path.Combine(OutputDirectory, DateTime.Now.ToString("yyyy-MM-dd_HHmmss"));
            Directory.CreateDirectory(outputDir);

            // Write batch undo file
            var isWindowsOS = System.Runtime.InteropServices.RuntimeInformation.OSDescription.Contains("Windows");
            string contents = string.Empty;

            foreach (string file in _filesToRemove) {
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
            templateText = templateText.Replace("#SCRIPTNAME", Path.GetFileName(_scriptPath));
            templateText = templateText.Replace("#COUNTFILES", _filesToRemove.Count.ToString());
            templateText = templateText.Replace("#FOLDERPATH", outputDir);
            templateText = templateText.Replace("#HELPDOC", "https://github.com/noah-severyn/SC4Cleanitol/wiki");
            templateText = templateText.Replace("#LISTOFFILES", string.Join("<br/>", _filesToRemove));
            templateText = templateText.Replace("#DATETIME", DateTime.Now.ToString("dd MMM yyyy HH:mm"));
            File.WriteAllText(Path.Combine(outputDir, "CleanupSummary.html"), templateText);
        }



        /// <summary>
        /// Export the scanned TGIs a document in the specified format.
        /// </summary>
        /// <param name="fileType">Export file type. Default is <see cref="ExportType.Csv"/>.</param>
        /// <returns>The full path of the created export file.</returns>
        public string ExportTGIs(ExportType fileType = ExportType.Csv) {
            string outputPath = Path.Combine(OutputDirectory, "ScannedTGIs " + DateTime.Now.ToString("yyyy-MM-dd_HHmmss"), fileType == ExportType.Csv ? ".csv" : ".db");

            if (fileType == ExportType.Csv) {
                StringBuilder list = new StringBuilder("File,Type,Group,Instance,\r\n");
                foreach (var item in _allTgis) {
                    list.AppendLine(item.FilePath + "," + item.Tgi.ToString().Replace(" ", string.Empty));
                }
                File.WriteAllText(outputPath, list.ToString());
            } else {
                DatabaseBuilder.CreateDb(outputPath, _allTgis);
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