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
            //https://stackoverflow.com/questions/47576074/get-releases-github-api-v3
            //const string API_URL = "https://api.github.com/repos/noah-severyn/SC4Cleanitol/releases/";
            //HttpClient client = new HttpClient();
            //HttpRequestMessage request = new HttpRequestMessage() {
            //    RequestUri = new Uri("https://api.github.com/noah-severyn/SC4Cleanitol/releases/"),
            //    Method = HttpMethod.Get
            //};

            //// Added user agent
            //client.DefaultRequestHeaders.Add("User-Agent", "SC4Cleanitol");
            //Uri uri = new Uri(API_URL);
            //var releases = await client.SendAsync(request);
            ////return releases;

            //TODO - fix this to pull release and only open webpage if out of date
            string target = "https://github.com/noah-severyn/SC4Cleanitol/releases/latest";
            //System.Diagnostics.Process.Start(target);

            var sinfo = new ProcessStartInfo(target);
            sinfo.UseShellExecute = true;
            Process.Start(sinfo);
        }
    }
}
