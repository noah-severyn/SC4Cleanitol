﻿namespace SC4Cleanitol {
    /// <summary>
    /// Abstract description of a run of richly formatted text describing the text and the style it should be formatted as.
    /// </summary>
    /// <remarks>
    /// Platform-specific implementations should convert this to the appropriate run type for the UI element, e.g. <c>System.Windows.Documents.Run</c> (WPF) or <c>Microsoft.UI.Xaml.Documents.Run</c> (WinUI).
    /// </remarks>
    public readonly struct FormattedRun {
        /// <summary>
        /// Text to display.
        /// </summary>
        public readonly string Text { get; }
        /// <summary>
        /// Format style of text
        /// </summary>
        public readonly RunType Type { get; }

        public readonly string URL { get; }

        /// <summary>
        /// Instantiate a new run of text.
        /// </summary>
        /// <param name="text">Text to display</param>
        /// <param name="type">Format type</param>
        /// <param name="url">URL if the type is Hyperlink. Default is blank string</param>
        public FormattedRun(string text, RunType type = RunType.BlackStd, string url = "") {
            Type = type;
            Text = text;
            if (type is RunType.Hyperlink) {
                URL = url;
            } else {
                URL = string.Empty;
            }
        }
    }

    /// <summary>
    /// Formatting type for a run of text.
    /// </summary>
    public enum RunType {
        /// <summary>
        /// Blue proportionally spaced text.
        /// </summary>
        BlueStd,
        /// <summary>
        /// Blue mono spaced bold text.
        /// </summary>
        BlueMono,
        /// <summary>
        /// Red proportionally spaced text.
        /// </summary>
        RedStd,
        /// <summary>
        /// Red mono spaced bold text.
        /// </summary>
        RedMono,
        /// <summary>
        /// Green proportionally spaced text.
        /// </summary>
        GreenStd,
        /// <summary>
        /// Black mono spaced text.
        /// </summary>
        BlackMono,
        /// <summary>
        /// Black mono spaced bold text.
        /// </summary>
        BlackMonoBold,
        /// <summary>
        /// Black proportionally spaced text.
        /// </summary>
        BlackStd,
        /// <summary>
        /// Black large, underlined, proportionally spaced text.
        /// </summary>
        BlackHeading,
        /// <summary>
        /// Hyperlink text with the default hyperlink formatting.
        /// </summary>
        Hyperlink,
        /// <summary>
        /// Hyperlink text with mono spaced text.
        /// </summary>
        HyperlinkMono
    }
}