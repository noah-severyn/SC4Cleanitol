using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
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
    /// Interaction logic for ScanTGIWindow.xaml
    /// </summary>
    public partial class ScanTGIWindow : Window {
        public int FilesScanned { get; set; }
        public int TotalFiles { get; set; }
        public int TGIsDiscovered { get; set; }


        public ScanTGIWindow() {
            InitializeComponent();
        }

        //https://wpf-tutorial.com/misc-controls/the-progressbar-control/
        private void Window_ContentRendered(object sender, EventArgs e) {
            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += Worker_DoWork;
            worker.ProgressChanged += Worker_ProgressChanged;

            worker.RunWorkerAsync();
        }

        void Worker_DoWork(object sender, DoWorkEventArgs e) {
            for (int i = 0; i < 100; i++) {
                (sender as BackgroundWorker).ReportProgress(i);
                Thread.Sleep(100);
            }
        }

        void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e) {
            FilesScannedProgress.Value = e.ProgressPercentage;
        }
    }
}
