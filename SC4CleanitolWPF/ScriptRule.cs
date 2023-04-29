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
            /// Search for the SearchItem if this filename or TGI is present.
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
                } else {
                    SearchItem = ruleText.Substring(0, conditionalLocn).Trim();
                    ConditionalItem = ruleText.Substring(conditionalLocn + 2, semicolonLocn - conditionalLocn - 2).Trim();
                }

                IsSearchItemTGI = SearchItem.Substring(0, 2) == "0x";
                IsConditionalItemTGI = ConditionalItem.Substring(0, 2) == "0x";

                if (httpLocn > semicolonLocn + 2) { //if there's no source file name specified.
                    SourceName = ruleText.Substring(semicolonLocn + 1, httpLocn - semicolonLocn - 2).Trim();
                } else {
                    SourceName = string.Empty;
                }

                SourceURL = ruleText.Substring(httpLocn).Trim();
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
