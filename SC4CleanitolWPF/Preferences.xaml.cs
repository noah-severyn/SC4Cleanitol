﻿using System;
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
            UserPluginsDirectory.Text = Properties.Settings.Default.UserPluginsDirectory;
            SystemPluginsDirectory.Text = Properties.Settings.Default.SystemPluginsDirectory;
            ScanSystemDirectoryCheckbox.IsChecked = Properties.Settings.Default.ScanSystemPlugins;
            LanguageChoice.Text = Properties.Settings.Default.Language;
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

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            Properties.Settings.Default.UserPluginsDirectory = UserPluginsDirectory.Text;
            Properties.Settings.Default.SystemPluginsDirectory = SystemPluginsDirectory.Text;
            if (ScanSystemDirectoryCheckbox.IsChecked is not null) {
                Properties.Settings.Default.ScanSystemPlugins = (bool) ScanSystemDirectoryCheckbox.IsChecked;
            }
            //Properties.Settings.Default.Language = LanguageChoice.SelectedItem.ToString();
            //TODO - fix the language setting.
            Properties.Settings.Default.Save();
        }
    }
}
