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
            try
            {
                listOfLog = new List<LogData1>();

                using (var con = new NpgsqlConnection(System.Configuration.ConfigurationManager.AppSettings["ConnectionString"]))
                {
                    //string sql = "select coalesce(dt1.\"LogDate\",dt2.\"LogDate\" ) as  \"LogDate\", coalesce(\"PassCount\", 0) as \"PassCount\",coalesce(\"FailCount\",0) as \"FailCount\" from (SELECT  \"TimeStamp\"::date as \"LogDate\", count (1) as \"FailCount\" FROM public.\"Logs\" where \"TimeStamp\" >= '" + clStartDate.Text + "' and \"TimeStamp\" <= '" + Convert.ToDateTime(clToDate.Text).AddDays(1).ToString() + "' AND \"Result\" = 'false' group by \"Result\", \"TimeStamp\"::date) dt1 Full join (SELECT  \"TimeStamp\"::date as \"LogDate\", count (1) as \"PassCount\" FROM public.\"Logs\" where \"TimeStamp\" >= '" + clStartDate.Text + "' and \"TimeStamp\" <= '" + Convert.ToDateTime(clToDate.Text).AddDays(1).ToString() + "' AND \"Result\" = 'true' group by \"Result\", \"TimeStamp\"::date) dt2 on dt1.\"LogDate\" = dt2.\"LogDate\";";

                    string sql = "SELECT \"BatchName\", \"PartName\", \"SrNo\", \"TimeStamp\",  CASE   WHEN \"Result\" = TRUE THEN 'OK'  ELSE 'Not OK'  END AS \"ResultStatus\", CASE   WHEN \"Ch1Result\" = TRUE THEN 'OK' WHEN Ch1Result IS NULL THEN 'NA'  ELSE 'Not OK'  END AS \"Ch1Result\", CASE   WHEN \"Ch2Result\" = TRUE THEN 'OK' WHEN Ch2Result IS NULL THEN 'NA'  ELSE 'Not OK'  END AS \"Ch2Result\", CASE   WHEN \"Ch3Result\" = TRUE THEN 'OK' WHEN Ch3Result IS NULL THEN 'NA'  ELSE 'Not OK'  END AS \"Ch3Result\", CASE   WHEN \"Ch4Result\" = TRUE THEN 'OK' WHEN Ch4Result IS NULL THEN 'NA'  ELSE 'Not OK'  END AS \"Ch4Result\" FROM public.\"Logs\" l\r\n\tWhere \"BatchName\" like '%" + txtBatchName.Text+ "%' AND \"SrNo\" like '%" + txtSrNo.Text+ "%' AND \"TimeStamp\" >= '" + clStartDate.Text + "' and \"TimeStamp\" <= '" + Convert.ToDateTime(clToDate.Text).AddDays(1).ToString() + "' ";

                    con.Open();

                    var cmd = new NpgsqlCommand(sql, con);
                    var dataReader = cmd.ExecuteReader();

                    DataTable dt = new DataTable();
                    dt.Load(dataReader);

                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        LogData1 _part = new LogData1();
                        _part.BatchName = dt.Rows[i]["BatchName"]?.ToString() ?? string.Empty;
                        _part.PartName =  dt.Rows[i]["PartName"]?.ToString() ?? string.Empty;
                        string ts = dt.Rows[i]["TimeStamp"]?.ToString() ?? string.Empty;
                        if (!string.IsNullOrEmpty(ts) && DateTimeOffset.TryParse(ts, out DateTimeOffset dto))
                        {
                            _part.TimeStamp = dto.ToString("dd/MM/yy HH:mm:ss");
                        }

                        _part.ResultStatus = dt.Rows[i]["ResultStatus"]?.ToString() ?? string.Empty;
                        _part.SrNo = dt.Rows[i]["SrNo"]?.ToString() ?? string.Empty;

                        _part.Ch1Result = dt.Rows[i]["Ch1Result"]?.ToString() ?? string.Empty;
                        _part.Ch2Result = dt.Rows[i]["Ch2Result"]?.ToString() ?? string.Empty;
                        _part.Ch3Result = dt.Rows[i]["Ch3Result"]?.ToString() ?? string.Empty;
                        _part.Ch4Result = dt.Rows[i]["Ch4Result"]?.ToString() ?? string.Empty;


                        listOfLog.Add(_part);
                    }

                    grdlogs.ItemsSource = listOfLog; // .OrderBy(t => Convert.ToDateTime(t.LogStartDate)); ;
                }
            }
            catch (Exception)
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
