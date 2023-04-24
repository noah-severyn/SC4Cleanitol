using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows;

namespace SC4CleanitolWPF {
    internal static class RunStyles {
        /// <summary>
        /// Blue porportionally spaced text.
        /// </summary>
        internal static Run BlueStd(string text) {
            Run r = new Run(text);
            r.Foreground = Brushes.Blue;
            return r;
        }
        /// <summary>
        /// Blue mono spaced bold text.
        /// </summary>
        internal static Run BlueMono(string text) {
            Run r = new Run(text);
            r.Foreground = Brushes.Blue;
            r.FontFamily = new FontFamily("Consolas, Courier New");
            r.FontWeight = FontWeights.Bold;
            return r;
        }
        /// <summary>
        /// Red porportionally spaced text.
        /// </summary>
        internal static Run RedStd(string text) {
            Run r = new Run(text);
            r.Foreground = Brushes.Firebrick;
            return r;
        }
        /// <summary>
        /// Red mono spaced bold text.
        /// </summary>
        internal static Run RedMono(string text) {
            Run r = new Run(text);
            r.Foreground = Brushes.Firebrick;
            r.FontFamily = new FontFamily("Consolas, Courier New");
            r.FontWeight = FontWeights.Bold;
            return r;
        }
        /// <summary>
        /// Green porportionally spaced text.
        /// </summary>
        internal static Run GreenStd(string text) {
            Run r = new Run(text);
            r.Foreground = Brushes.DarkGreen;
            return r;
        }
        /// <summary>
        /// Black mono spaced text.
        /// </summary>
        internal static Run BlackMono(string text) {
            Run r = new Run(text);
            r.FontFamily = new FontFamily("Consolas, Courier New");
            return r;
        }
        /// <summary>
        /// Black porportionally spaced text.
        /// </summary>
        internal static Run BlackStd(string text) {
            Run r = new Run(text);
            return r;
        }
    }
}
