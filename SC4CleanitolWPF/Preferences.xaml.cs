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
            switch (Properties.Settings.Default.ScanAdditionalFolders) {
                case 0:
                    ScanPluginsOnly.IsChecked = true;
                    ScanAdditionalFoldersIncludePlugins.IsChecked = false;
                    ScanAdditionalFoldersExcludePlugins.IsChecked = false;
                    break;
                case 1:
                    ScanPluginsOnly.IsChecked = false;
                    ScanAdditionalFoldersIncludePlugins.IsChecked = true;
                    ScanAdditionalFoldersExcludePlugins.IsChecked = false;
                    break;
                case 2:
                    ScanPluginsOnly.IsChecked = false;
                    ScanAdditionalFoldersIncludePlugins.IsChecked = false;
                    ScanAdditionalFoldersExcludePlugins.IsChecked = true;
                    break;
            }
            if (Properties.Settings.Default.AdditionalFolders is null) {
                Properties.Settings.Default.AdditionalFolders = new System.Collections.Specialized.StringCollection();
            }
            
            AdditionalFolders.ItemsSource = Properties.Settings.Default.AdditionalFolders;

            //AdditionalFolders.DataContext = this;

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
                Properties.Settings.Default.UserPluginsDirectory = dialog.FileName;
                UserPluginsDirectory.Text = dialog.FileName;
            }
        }
        private void SystemChooseFolder_Click(object sender, RoutedEventArgs e) {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog {
                InitialDirectory = Properties.Settings.Default.SystemPluginsDirectory,
                IsFolderPicker = true
            };
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok) {
                Properties.Settings.Default.SystemPluginsDirectory = dialog.FileName;
                SystemPluginsDirectory.Text = dialog.FileName;
            }
        }
        private void CleanitolOutputChooseFolder_Click(object sender, RoutedEventArgs e) {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog {
                InitialDirectory = Properties.Settings.Default.BaseOutputDirectory,
                IsFolderPicker = true
            };
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok) {
                Properties.Settings.Default.BaseOutputDirectory = dialog.FileName;
                CleanitolOutputDirectory.Text = dialog.FileName;
            }
        }


        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            if (ScanSystemDirectoryCheckbox.IsChecked is not null) {
                Properties.Settings.Default.ScanSystemPlugins = (bool) ScanSystemDirectoryCheckbox.IsChecked;
            }
            if (ScanPluginsOnly.IsChecked is not null && (bool) ScanPluginsOnly.IsChecked) {
                Properties.Settings.Default.ScanAdditionalFolders = 0;
            } else if (ScanAdditionalFoldersIncludePlugins.IsChecked is not null && (bool) ScanAdditionalFoldersIncludePlugins.IsChecked) {
                Properties.Settings.Default.ScanAdditionalFolders = 1;
            } else if (ScanAdditionalFoldersExcludePlugins.IsChecked is not null && (bool) ScanAdditionalFoldersExcludePlugins.IsChecked) {
                Properties.Settings.Default.ScanAdditionalFolders = 2;
            }
            Properties.Settings.Default.Save();
        }


        private void VersionCheckButton_Click(object sender, RoutedEventArgs e) {
            string target = "https://github.com/noah-severyn/SC4Cleanitol/releases/latest";
            var sinfo = new ProcessStartInfo(target);
            sinfo.UseShellExecute = true;
            Process.Start(sinfo);
        }

        /// <summary>
        /// Adds an additional folder to scan.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddAdditionalFolder_Click(object sender, RoutedEventArgs e) {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog {
                IsFolderPicker = true
            };
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok) {
                Properties.Settings.Default.AdditionalFolders.Add(dialog.FileName);
                AdditionalFolders.ItemsSource = Properties.Settings.Default.AdditionalFolders;
                AdditionalFolders.Items.Refresh();
            }
        }

        /// <summary>
        /// Removes the selected additional folder from the list to scan.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RemoveAdditionalFolder_Click(object sender, RoutedEventArgs e) {
            Properties.Settings.Default.AdditionalFolders.Remove((string) AdditionalFolders.SelectedItem);
            AdditionalFolders.ItemsSource = Properties.Settings.Default.AdditionalFolders;
            AdditionalFolders.Items.Refresh();
        }
    }
}
