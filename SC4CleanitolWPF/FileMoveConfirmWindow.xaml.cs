using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace SC4CleanitolWPF {
    /// <summary>
    /// Interaction logic for FileMoveConfirmWindow.xaml
    /// </summary>
    public partial class FileMoveConfirmWindow : Window {
        public FileMoveConfirmWindow() {
            InitializeComponent();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) {
            this.Close();
        }

        private void OpenFolderButton_Click(object sender, RoutedEventArgs e) {
            Process.Start(Properties.Settings.Default.CleanitolOutputDirectory);
        }
    }
}
