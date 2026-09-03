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

namespace _8F.Views
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Logs : Window
    {
        public bool IsSaved = false;
        public List<LogData> listOfLog = default!;
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

                using (var con = new NpgsqlConnection(System.Configuration.ConfigurationManager.AppSettings["ConnectionString"]))
                {
                    //string sql = "select coalesce(dt1.\"LogDate\",dt2.\"LogDate\" ) as  \"LogDate\", coalesce(\"PassCount\", 0) as \"PassCount\",coalesce(\"FailCount\",0) as \"FailCount\" from (SELECT  \"TimeStamp\"::date as \"LogDate\", count (1) as \"FailCount\" FROM public.\"Logs\" where \"TimeStamp\" >= '" + clStartDate.Text + "' and \"TimeStamp\" <= '" + Convert.ToDateTime(clToDate.Text).AddDays(1).ToString() + "' AND \"Result\" = 'false' group by \"Result\", \"TimeStamp\"::date) dt1 Full join (SELECT  \"TimeStamp\"::date as \"LogDate\", count (1) as \"PassCount\" FROM public.\"Logs\" where \"TimeStamp\" >= '" + clStartDate.Text + "' and \"TimeStamp\" <= '" + Convert.ToDateTime(clToDate.Text).AddDays(1).ToString() + "' AND \"Result\" = 'true' group by \"Result\", \"TimeStamp\"::date) dt2 on dt1.\"LogDate\" = dt2.\"LogDate\";";

                    string sql = "SELECT \"BatchName\", Min(\"TimeStamp\") as \"StartDate\", Max(\"TimeStamp\") as \"EndDate\", " +
                                 "(select Count(1) from public.\"Logs\" l1 where \"BatchName\" = l.\"BatchName\" and \"Result\" = 'true') as \"PassCount\", " +
                                 "(select Count(1) from public.\"Logs\" l1 where \"BatchName\" = l.\"BatchName\" and \"Result\" = 'false') as \"FailCount\" " +
                                 "FROM public.\"Logs\" l " +
                                 "WHERE \"BatchName\" LIKE @BatchName AND \"TimeStamp\" >= @StartDate AND \"TimeStamp\" <= @EndDate " +
                                 "GROUP BY \"BatchName\"";

                    con.Open();

                    var cmd = new NpgsqlCommand(sql, con);
                    cmd.Parameters.AddWithValue("@BatchName", "%" + (txtBatchName.Text ?? "") + "%");
                    DateTime startDate = clStartDate.SelectedDate ?? (DateTime.TryParse(clStartDate.Text, out var sd) ? sd : DateTime.Now);
                    DateTime endDate = (clToDate.SelectedDate ?? (DateTime.TryParse(clToDate.Text, out var ed) ? ed : DateTime.Now)).AddDays(1);
                    cmd.Parameters.AddWithValue("@StartDate", startDate);
                    cmd.Parameters.AddWithValue("@EndDate", endDate);
                    var dataReader = cmd.ExecuteReader();

                    DataTable dt = new DataTable();
                    dt.Load(dataReader);

                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        LogData _part = new LogData();
                        _part.BatchName = dt.Rows[i]["BatchName"]?.ToString() ?? "";
                        DateTimeOffset dto = DateTimeOffset.Parse(dt.Rows[i]["StartDate"]?.ToString() ?? DateTime.Now.ToString());
                        _part.LogStartDate = dto.ToString("dd/MM/yy HH:mm:ss");

                        DateTimeOffset dto1 = DateTimeOffset.Parse(dt.Rows[i]["EndDate"]?.ToString() ?? DateTime.Now.ToString());
                        _part.LogEndDate = dto1.ToString("dd/MM/yy HH:mm:ss");

                        _part.PassCount = Convert.ToInt32(dt.Rows[i]["PassCount"]);
                        _part.FailCount = Convert.ToInt32(dt.Rows[i]["FailCount"]);
                        _part.TotalCount = _part.PassCount + _part.FailCount;
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
                    string conecnt = "Batch Name,Log Start Date,Log End Date,OK Count,Not OK Count,Total Count";
                    foreach (var log in listOfLog)
                    {
                        conecnt = conecnt + "\n";
                        conecnt = conecnt + log.BatchName + "," + log.LogStartDate + "," + log.LogEndDate + "," + log.PassCount.ToString() + "," + log.FailCount.ToString() + "," + log.TotalCount.ToString();
                    }
                    File.WriteAllText(dlg.FileName, conecnt);
                }
            }
        }
    }
}
