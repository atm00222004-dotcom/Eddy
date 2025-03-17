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
    public partial class Logs : Window
    {
        public bool IsSaved = false;
        public List<LogData> listOfLog;
        public Logs()
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
            try
            {
                listOfLog = new List<LogData>();

                using (var con = new NpgsqlConnection(System.Configuration.ConfigurationSettings.AppSettings["ConnectionString"]))
                {
                    string sql = "select coalesce(dt1.\"LogDate\",dt2.\"LogDate\" ) as  \"LogDate\", coalesce(\"PassCount\", 0) as \"PassCount\",coalesce(\"FailCount\",0) as \"FailCount\" from (SELECT  \"TimeStamp\"::date as \"LogDate\", count (1) as \"FailCount\" FROM public.\"Logs\" where \"TimeStamp\" >= '" + clStartDate.Text + "' and \"TimeStamp\" <= '" + Convert.ToDateTime(clToDate.Text).AddDays(1).ToString() + "' AND \"Result\" = 'false' group by \"Result\", \"TimeStamp\"::date) dt1 Full join (SELECT  \"TimeStamp\"::date as \"LogDate\", count (1) as \"PassCount\" FROM public.\"Logs\" where \"TimeStamp\" >= '" + clStartDate.Text + "' and \"TimeStamp\" <= '" + Convert.ToDateTime(clToDate.Text).AddDays(1).ToString() + "' AND \"Result\" = 'true' group by \"Result\", \"TimeStamp\"::date) dt2 on dt1.\"LogDate\" = dt2.\"LogDate\";";

                    con.Open();

                    var cmd = new NpgsqlCommand(sql, con);
                    var dataReader = cmd.ExecuteReader();

                    DataTable dt = new DataTable();
                    dt.Load(dataReader);

                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        LogData _part = new LogData();
                        _part.LogDate = Convert.ToDateTime(dt.Rows[i]["LogDate"]).ToShortDateString();
                        _part.PassCount = Convert.ToInt32(dt.Rows[i]["PassCount"]);
                        _part.FailCount = Convert.ToInt32(dt.Rows[i]["FailCount"]);
                        _part.TotalCount = _part.PassCount + _part.FailCount;
                        listOfLog.Add(_part);
                    }
                    
                    grdlogs.ItemsSource = listOfLog.OrderBy(t => Convert.ToDateTime(t.LogDate)); ;
                }
            }
            catch (Exception ex)
            {

            }
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
                    string conecnt = "Log Date,OK Count,Not OK Count,Total Count";
                    foreach (var log in listOfLog)
                    {
                        conecnt = conecnt + "\n";
                        conecnt = conecnt + log.LogDate + "," + log.PassCount.ToString() + "," + log.FailCount.ToString() + "," + log.TotalCount.ToString();
                    }
                    File.WriteAllText(dlg.FileName, conecnt);
                }
            }
        }
    }
}
