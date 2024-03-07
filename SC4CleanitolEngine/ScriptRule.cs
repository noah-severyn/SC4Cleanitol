using System.Text.RegularExpressions;

namespace SC4Cleanitol {
    /// <summary>
    /// Classes, enums, and functions related to parsing script rules. A rule is a single line read from the script.
    /// </summary>
    internal class ScriptRule {
        private static readonly string[] dbpfExtensions = { ".dat", ".sc4Lot", ".sc4desc", ".sc4Model", ".dll" };

        /// <summary>
        /// A script rule targeted at locating required files or TGIs.
        /// </summary>
        public struct DependencyRule {
            /// <summary>
            /// Whether this rule is a conditional dependency rule or a standard dependency rule.
            /// </summary>
            public bool IsConditionalRule { get; set; }

            /// <summary>
            /// Filename or TGI to search for.
            /// </summary>
            public string SearchItem { get; set; }
            /// <summary>
            /// Scan for the SearchItem if this item is present. Can be a filename or TGI.
            /// </summary>
            public string ConditionalItem { get; set; }
            /// <summary>
            /// Is the SearchItem a TGI?
            /// </summary>
            public bool IsSearchItemTGI { get; set; }
            /// <summary>
            /// Is the ConditionalItem a TGI?
            /// </summary>
            public bool IsConditionalItemTGI { get; set; }
            /// <summary>
            /// Name of the exchange upload containing the SearchItem.
            /// </summary>
            public string SourceName { get; set; }
            /// <summary>
            /// URL of the exchange upload containing the SearchItem.
            /// </summary>
            public string SourceURL { get; set; }

            /// <summary>
            /// Indicates a dependency rule where no valid TGI or file was provided to scan for.
            /// </summary>
            /// <remarks>
            /// Provided for compatibility reasons for very old cleanitol scripts. 
            /// </remarks>
            internal bool IsUnchecked { get; private set; }

            /// <summary>
            /// A script rule targeted at locating required files or TGIs.
            /// </summary>
            /// <param name="ruleText">Raw text for this rule</param>
            public DependencyRule(string ruleText) {
                int semicolonLocn = ruleText.IndexOf(';');
                int conditionalLocn = ruleText.IndexOf("??");
                IsConditionalRule = conditionalLocn != -1;

                //Support "unchecked" dependencies for certain legacy cleanitol files that use these as cascading dependencies. If searchItem does not contain any of the valid file extensions AND is not a TGI (contains '0x' 3 times).
                int cutoff;
                if (conditionalLocn == -1) {
                    cutoff = semicolonLocn;
                } else {
                    cutoff = conditionalLocn;
                }
                if (!dbpfExtensions.Any(x => ruleText.Substring(0, semicolonLocn).EndsWith(x)) && Regex.Matches(ruleText.Substring(0, cutoff), "0x").Count != 3) {
                    IsUnchecked = true;
                }

                
                if (IsConditionalRule) {
                    SearchItem = ruleText.Substring(0, conditionalLocn).Trim();
                    ConditionalItem = ruleText.Substring(conditionalLocn + 2, semicolonLocn - conditionalLocn - 2).Trim();
                    IsConditionalItemTGI = ConditionalItem.Substring(0, 2) == "0x";
                } else {
                    SearchItem = ruleText.Substring(0, semicolonLocn).Trim();
                    ConditionalItem = string.Empty;
                    IsConditionalItemTGI = false;
                }

                IsSearchItemTGI = SearchItem.Substring(0, 2) == "0x";
                int httpLocn = ruleText.IndexOf("http");
                if (httpLocn - semicolonLocn + 1 > 4) {
                    SourceName = ruleText.Substring(semicolonLocn + 1, httpLocn - semicolonLocn - 2).Trim();
                } else {
                    SourceName = SearchItem;
                }
                
                SourceURL = ruleText.Substring(httpLocn).Trim();

                // The script can allow for any separator between TGI numbers, but csDBPF uses comma space ", ". Automatically reformat the script format behind the scenes to allow for equality comparison.
                if (IsSearchItemTGI) {
                    SearchItem = csDBPF.DBPFTGI.CleanTGIFormat(SearchItem);
                }
                if (IsConditionalItemTGI) {
                    ConditionalItem = csDBPF.DBPFTGI.CleanTGIFormat(ConditionalItem);
                }
            }
        }



        /// <summary>
        /// The action to be taken for this rule.
        /// </summary>
        /// <see ref="https://www.sc4devotion.com/forums/index.php?topic=3797.0"/>
        public enum RuleType {
            /// <summary>
            /// Internal comments not shown to the user. Use for script documentation.
            /// </summary>
            ScriptComment,
            /// <summary>
            /// Text shown to the user in the script output window.
            /// </summary>
            UserComment,
            /// <summary>
            /// A heading shown to the user in the script output window. Handy to separate out content.
            /// </summary>
            UserCommentHeading,
            /// <summary>
            /// An item that should be removed from plugins.
            /// </summary>
            Removal,
            /// <summary>
            /// An item that is required for the mod.
            /// </summary>
            Dependency,
            /// <summary>
            /// An item that is required for the mod, but only if another item is present.
            /// </summary>
            ConditionalDependency,
        }



        /// <summary>
        /// Determine the action to be taken for this ruleText.
        /// </summary>
        /// <param name="rule">Rule text</param>
        /// <returns>A <see cref="RuleType"/> informing the action to be taken</returns>
        public static RuleType ParseRuleType(string rule) {
            //Script Comment
            if (rule.IndexOf(';') == 0 || rule.Length == 0) {
                return RuleType.ScriptComment;
            }

            //User Comment + User Comment Heading
            if (rule.IndexOf('>') == 0) {
                if (rule.IndexOf('#') == 1) {
                    return RuleType.UserCommentHeading;
                }
                return RuleType.UserComment;
            }

            //Dependency + Conditional Dependency
            if (rule.Contains("http", StringComparison.OrdinalIgnoreCase)) {
                if (rule.Contains("??")) {
                    return RuleType.ConditionalDependency;
                }
                return RuleType.Dependency;
            }

            //Support "unchecked" dependencies for certain legacy cleanitol files that use these as cascading dependencies.
            if (rule.IndexOf(';') > 0) {
                return RuleType.Dependency;
            } else {
                return RuleType.Removal;
            }
        }


        
    }
}