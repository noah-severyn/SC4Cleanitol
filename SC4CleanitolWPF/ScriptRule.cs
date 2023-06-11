using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SC4CleanitolWPF.MainWindow;

namespace SC4CleanitolWPF {
    /// <summary>
    /// Classes, enums, and functions related to parsing script rules. A rule is a single line read from the script.
    /// </summary>
    internal class ScriptRule {

        /// <summary>
        /// A script rule targeted at locating required files or TGIs.
        /// </summary>
        public struct DependencyRule {
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
            /// A script rule targeted at locating required files or TGIs.
            /// </summary>
            /// <param name="ruleText">Raw text for this rule</param>
            public DependencyRule(string ruleText) {
                int semicolonLocn = ruleText.IndexOf(';');
                int conditionalLocn = ruleText.IndexOf("??");
                int httpLocn = ruleText.IndexOf("http");

                if (conditionalLocn == -1) {
                    SearchItem = ruleText.Substring(0, semicolonLocn).Trim();
                    ConditionalItem = string.Empty;
                    IsConditionalItemTGI = false;
                } else {
                    SearchItem = ruleText.Substring(0, conditionalLocn).Trim();
                    ConditionalItem = ruleText.Substring(conditionalLocn + 2, semicolonLocn - conditionalLocn - 2).Trim();
                    IsConditionalItemTGI = ConditionalItem.Substring(0, 2) == "0x";
                }

                IsSearchItemTGI = SearchItem.Substring(0, 2) == "0x";
                SourceName = ruleText.Substring(semicolonLocn + 1, httpLocn - semicolonLocn - 2).Trim();
                SourceURL = ruleText.Substring(httpLocn).Trim();

                CleanTGIFormat();
            }

            /// <summary>
            /// The script can allow for any separator between TGI numbers, but csDBPF uses comma space ", ". Automatically reformat the script format behind the scenes to allow for equality comparison.
            /// </summary>
            private void CleanTGIFormat() {
                int second0x;
                string separator;
                if (IsSearchItemTGI) {
                    second0x = SearchItem.IndexOf("0x", 10);
                    separator = SearchItem.Substring(10, second0x - 10);
                    SearchItem = SearchItem.Replace(separator, ", ");
                }
                if (IsConditionalItemTGI) {
                    second0x = ConditionalItem.IndexOf("0x", 10);
                    separator = ConditionalItem.Substring(10, second0x - 10);
                    ConditionalItem = ConditionalItem.Replace(separator, ", ");
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

            //Removal
            return RuleType.Removal;
        }
    }
}
