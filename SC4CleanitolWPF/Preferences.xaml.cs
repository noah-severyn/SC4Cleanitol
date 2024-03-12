using System;
using System.Diagnostics;
using System.Windows;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace SC4CleanitolWPF {
    /// <summary>
    /// Interaction logic for Preferences.xaml
    /// </summary>
    public partial class Preferences : Window {
        /// <summary>
        /// Initialize the Preferences Window.
        /// </summary>
        public Preferences() {
            InitializeComponent();
            UserPluginsDirectory.Text = Properties.Settings.Default.UserPluginsDirectory;
            SystemPluginsDirectory.Text = Properties.Settings.Default.SystemPluginsDirectory;
            CleanitolOutputDirectory.Text = Properties.Settings.Default.BaseOutputDirectory;
            ScanSystemDirectoryCheckbox.IsChecked = Properties.Settings.Default.ScanSystemPlugins;

            Window window = Application.Current.MainWindow;
            if (window is not null) {
                VersionInfoLabel.Text = "Current Version: " + ((MainWindow) window).releaseVersion + " (" + ((MainWindow) window).releaseDate + ")";
            }
            
        }

        private void UserChooseFolder_Click(object sender, RoutedEventArgs e) {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog {
                InitialDirectory = Properties.Settings.Default.UserPluginsDirectory,
                IsFolderPicker = true
            };
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok) {
                UserPluginsDirectory.Text = dialog.FileName;
            }
        }
        private void SystemChooseFolder_Click(object sender, RoutedEventArgs e) {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog {
                InitialDirectory = Properties.Settings.Default.SystemPluginsDirectory,
                IsFolderPicker = true
            };
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok) {
                SystemPluginsDirectory.Text = dialog.FileName;
            }
        }
        private void CleanitolOutputChooseFolder_Click(object sender, RoutedEventArgs e) {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog {
                InitialDirectory = Properties.Settings.Default.BaseOutputDirectory,
                IsFolderPicker = true
            };
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok) {
                CleanitolOutputDirectory.Text = dialog.FileName;
            }
        }


        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            Properties.Settings.Default.UserPluginsDirectory = UserPluginsDirectory.Text;
            Properties.Settings.Default.SystemPluginsDirectory = SystemPluginsDirectory.Text;
            if (ScanSystemDirectoryCheckbox.IsChecked is not null) {
                Properties.Settings.Default.ScanSystemPlugins = (bool) ScanSystemDirectoryCheckbox.IsChecked;
            }
            Properties.Settings.Default.BaseOutputDirectory = CleanitolOutputDirectory.Text;
            Properties.Settings.Default.Save();
        }


        private void VersionCheckButton_Click(object sender, RoutedEventArgs e) {
            string target = "https://github.com/noah-severyn/SC4Cleanitol/releases/latest";
            var sinfo = new ProcessStartInfo(target);
            sinfo.UseShellExecute = true;
            Process.Start(sinfo);
        }
    }
}
