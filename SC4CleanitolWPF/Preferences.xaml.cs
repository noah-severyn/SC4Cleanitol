using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace SC4CleanitolWPF {
    /// <summary>
    /// Interaction logic for Preferences.xaml
    /// </summary>
    public partial class Preferences : Window {
        public Preferences() {
            InitializeComponent();
            PluginsDirectory.Text = Properties.Settings.Default.PluginsDirectory;
            LanguageChoice.Text = Properties.Settings.Default.Language;
        }

        private void Preferences_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            
        }

        private void ChooseFolder_Click(object sender, RoutedEventArgs e) {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog {
                InitialDirectory = Properties.Settings.Default.PluginsDirectory,
                IsFolderPicker = true
            };
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok) {
                PluginsDirectory.Text = dialog.FileName;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            Properties.Settings.Default.PluginsDirectory = PluginsDirectory.Text;
            Properties.Settings.Default.Language = LanguageChoice.SelectedItem.ToString();
            Properties.Settings.Default.Save();
        }
    }
}
