using System.Text.RegularExpressions;
using csDBPF;

namespace SC4CleanitolEngine {
    /// <summary>
    /// Classes, enums, and functions related to parsing script rules. A rule is a single line read from the script.
    /// </summary>
    internal static class ScriptRule {
        /// <summary>
        /// Defines an action that should be taken when the rule is processed.
        /// </summary>
        public enum Type {
            /// <summary>
            /// Internal comments not shown to the user. Use for script documentation.
            /// </summary>
            HiddenComment,
            /// <summary>
            /// Text shown to the user in the script output window.
            /// </summary>
            Comment,
            /// <summary>
            /// A heading shown to the user in the script output window. Handy to separate out content.
            /// </summary>
            Heading,
            /// <summary>
            /// A web url to import rules from. Must point to a <c>.txt</c> file.
            /// </summary>
            Import,
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
            /// <summary>
            /// A rule with invalid syntax.
            /// </summary>
            Invalid
        }

        /// <summary>
        /// Parse the syntax of the specified rule to determine its type.
        /// </summary>
        /// <param name="rule">Rule text</param>
        /// <returns>A <see cref="Type"/> informing the action to be taken.</returns>
        internal static Type Parse(string rule) {
            //Previous headings started with >#. This was a mistake. Should have used regular markdown-style syntax instead. Though the `>` is fine for comments because there'd otherwise be no way to differentiate them from rules.
            if (rule.StartsWith(">#")) {
                rule = rule.Replace(">#", "#");
            }

            //Historic cleanitol schema: https://www.sc4devotion.com/forums/index.php?topic=3797.0. The New format is backwards compatible with this schema.
            if (rule.StartsWith(';') || rule.Length == 0) {
                return Type.HiddenComment;
            } else if (rule.StartsWith('#')) {
                return Type.Heading;
            } else if (rule.StartsWith('>')) {
                return Type.Comment;
            } else if (rule.Contains("http", StringComparison.OrdinalIgnoreCase)) {
                if (rule.StartsWith('@')) {
                    return Type.Import;
                } else if (rule.Contains("??")) {
                    return Type.ConditionalDependency;
                } else if (rule.Contains(';')) {
                    return Type.Dependency;
                } else {
                    return Type.Invalid;
                }
            } else if (rule.IndexOf(';') > 0) { //Support "unchecked" dependencies for certain legacy cleanitol files that use these as cascading dependencies.
                return Type.Dependency;
            } else {
                return Type.Removal;
            }
        }

        /// <summary>
        /// A script rule targeted at locating required files or TGIs.
        /// </summary>
        internal class DependencyRule {
            /// <summary>
            /// Whether this rule is a conditional dependency rule or a standard dependency rule.
            /// </summary>
            public bool IsConditional { get; private set; }
            /// <summary>
            /// Filename or TGI to search for.
            /// </summary>
            public string SearchItem { get; private set; }
            /// <summary>
            /// Scan for the SearchItem if this item is present. Can be a filename or TGI.
            /// </summary>
            public string ConditionalItem { get; private set; }
            /// <summary>
            /// Name of the exchange upload the <see cref="SearchItem"/> is found at.
            /// </summary>
            public string LinkName { get; private set; }
            /// <summary>
            /// URL of the exchange upload containing the <see cref="SearchItem"/>.
            /// </summary>
            public string LinkUrl { get; private set; }

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
            internal DependencyRule(string ruleText) {
                int semicolonLocn = ruleText.IndexOf(';');
                int conditionalLocn = ruleText.IndexOf("??");
                IsConditional = conditionalLocn != -1;

                //Support "unchecked" dependencies for certain legacy cleanitol files that use these as cascading dependencies. If searchItem does not contain any of the valid file extensions AND is not a TGI (contains '0x' 3 times).
                int cutoff;
                if (conditionalLocn == -1) {
                    cutoff = semicolonLocn;
                } else {
                    cutoff = conditionalLocn;
                }
                if (!DBPFUtil.SC4Extensions.Any(ext => ruleText.Contains(ext, StringComparison.CurrentCultureIgnoreCase) && Regex.Matches(ruleText.Substring(0, cutoff), "0x").Count != 3)) {
                    IsUnchecked = true;
                }

                if (IsConditional) {
                    SearchItem = ruleText.Substring(0, conditionalLocn).Trim();
                    ConditionalItem = ruleText.Substring(conditionalLocn + 2, semicolonLocn - conditionalLocn - 2).Trim();
                } else {
                    SearchItem = ruleText.Substring(0, semicolonLocn).Trim();
                    ConditionalItem = string.Empty;
                }

                int httpLocn = ruleText.IndexOf("http");
                if (httpLocn - semicolonLocn + 1 > 4) {
                    LinkName = ruleText.Substring(semicolonLocn + 1, httpLocn - semicolonLocn - 2).Trim();
                } else {
                    LinkName = SearchItem;
                }
                if (httpLocn == -1) {
                    LinkUrl = "Unspecified URL";
                } else {
                    LinkUrl = ruleText.Substring(httpLocn).Trim();
                }


                // The script can allow for any separator between TGI numbers, but csDBPF uses comma space ", ". Automatically reformat the script format behind the scenes to allow for equality comparison.
                if (SearchItem.StartsWith("0x")) {
                    SearchItem = DBPFUtil.FormatTgiString(SearchItem);
                }
                if (ConditionalItem.StartsWith("0x")) {
                    ConditionalItem = DBPFUtil.FormatTgiString(ConditionalItem);
                }
            }
        }
    }
}