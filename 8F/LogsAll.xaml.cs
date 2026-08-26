using Newtonsoft.Json;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace _8F
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class LogAll : Window
    {
        public bool IsSaved = false;
        public List<LogData1> listOfLog = new();
        public LogAll()
        {
            InitializeComponent();

            clStartDate.SelectedDate = DateTime.Now;
            clToDate.SelectedDate = DateTime.Now;

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnSearch_MouseDown(object sender, MouseButtonEventArgs e)
        {
            LoadLogs();
        }

        public void LoadLogs()
        {
            var vm = new _8F.ViewModels.LogsAllViewModel
            {
                BatchNameFilter = txtBatchName.Text,
                SerialNoFilter = txtSrNo.Text,
                StartDate = clStartDate.SelectedDate,
                EndDate = clToDate.SelectedDate
            };
            vm.LoadLogs();
            listOfLog = vm.Logs.ToList();
            grdlogs.ItemsSource = listOfLog;
        }

        private void btnDownload_MouseDown(object sender, MouseButtonEventArgs e)
        {
            LoadLogs();
            if (listOfLog.Count > 0)
            {
                Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
                dlg.FileName = "Report"; // Default file name
                dlg.DefaultExt = ".csv"; // Default file extension
                dlg.Filter = "CSV Files (.csv)|*.csv"; // Filter files by extension

                Nullable<bool> result = dlg.ShowDialog();

                if (result == true)
                {
                    string conecnt = "Batch Name,Part Name,Serial Number,Date/Time,Result,Ch1Result,Ch2Result,Ch3Result,Ch4Result";
                    foreach (var log in listOfLog)
                    {
                        conecnt = conecnt + "\n";
                        conecnt = conecnt + log.BatchName + "," + log.PartName + "," + log.SrNo.Replace("\r", "") + "," + log.TimeStamp + "," + log.ResultStatus + "," + log.Ch1Result + "," + log.Ch2Result + "," + log.Ch3Result + "," + log.Ch4Result;
                    }
                    File.WriteAllText(dlg.FileName, conecnt);
                }
            }
        }
    }
}
