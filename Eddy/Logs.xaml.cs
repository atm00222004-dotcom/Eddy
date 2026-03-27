using Newtonsoft.Json;
using Npgsql;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
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

namespace Eddy
{
    /// <summary>
    /// Interaction logic for Logs.xaml
    /// </summary>
    public partial class Logs : Window
    {
        public bool IsSaved = false;
        public List<LogData> listOfLog;
        public Logs()
        {
            InitializeComponent();
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
                if (clStartDate.SelectedDate == null || clToDate.SelectedDate == null)
                {
                    MessageBox.Show("Please select date range");
                    return;
                }

                listOfLog = new List<LogData>();
               
                using (var con = new NpgsqlConnection(DeviceCOM.DBConnection))
                {
                    string sql = @"
                        SELECT 
                            ""BatchName"" AS ""BatchName"",
                            MIN(""TimeStamp"") AS ""StartDate"",
                            MAX(""TimeStamp"") AS ""EndDate"",
                            COUNT(*) FILTER (WHERE ""Result"" = true) AS ""PassCount"",
                            COUNT(*) FILTER (WHERE ""Result"" = false) AS ""FailCount""
                        FROM ""Logs""
                        WHERE 
                            ""BatchName"" ILIKE '%' || @BatchName || '%'
                            AND ""TimeStamp"" >= @StartDate
                            AND ""TimeStamp"" < @EndDate
                        GROUP BY ""BatchName""
                        ORDER BY ""StartDate"";
                    ";

                    con.Open();

                    var cmd = new NpgsqlCommand(sql, con);
                    cmd.Parameters.AddWithValue("@BatchName", txtBatchName.Text ?? "");
                    cmd.Parameters.AddWithValue("@StartDate", clStartDate.SelectedDate.Value);
                    cmd.Parameters.AddWithValue("@EndDate", clToDate.SelectedDate.Value.AddDays(1));

                    var reader = cmd.ExecuteReader();

                    DataTable dt = new DataTable();
                    dt.Load(reader);

                    foreach (DataRow row in dt.Rows)
                    {
                        try
                        {
                            LogData log = new LogData
                            {
                                BatchName = row["BatchName"].ToString(),
                                LogStartDate = Convert.ToDateTime(row["StartDate"]).ToString("dd/MM/yy HH:mm:ss"),
                                LogEndDate = Convert.ToDateTime(row["EndDate"]).ToString("dd/MM/yy HH:mm:ss"),
                                PassCount = Convert.ToInt32(row["PassCount"]),
                                FailCount = Convert.ToInt32(row["FailCount"])
                            };

                            log.TotalCount = log.PassCount + log.FailCount;

                            listOfLog.Add(log);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Something went wrong. Please try again.");
                        }
                    }

                    grdlogs.ItemsSource = listOfLog;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Something went wrong. Please try again");
            }
        }

        private void btnDownload_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
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
            catch (Exception ex)
            {
                MessageBox.Show("Something went wrong. Please try again.");
            }
        }

        private void btnDownloadPdf_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                LoadLogs();

                if (listOfLog == null || listOfLog.Count == 0)
                {
                    MessageBox.Show("No data found");
                    return;
                }

                if (clStartDate.SelectedDate == null || clToDate.SelectedDate == null)
                {
                    MessageBox.Show("Please select date range");
                    return;
                }

                var startDate = clStartDate.SelectedDate.Value;
                var endDate = clToDate.SelectedDate.Value;

                string fileName = startDate == endDate
                    ? $"ProductionReport_{startDate:ddMMM-yyyy}"
                    : $"ProductionReport_{startDate:ddMMMyyyy}_to_{endDate:ddMMMyyyy}";

                Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
                dlg.FileName = fileName;
                dlg.DefaultExt = ".pdf";
                dlg.Filter = "PDF Files (.pdf)|*.pdf";

                if (dlg.ShowDialog() == true)
                {
                    GeneratePdf(dlg.FileName);
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Unable to generate report.");
            }
        }
        public void GeneratePdf(string filePath)
        {
            try
            {
                var batchDetails = new Dictionary<string, List<(DateTime TimeStamp, string PartJson, string ConfigurationJson, bool Result)>>();

                foreach (var log in listOfLog)
                    batchDetails[log.BatchName] = GetBatchDetails(log.BatchName);

                QuestPDF.Settings.License = LicenseType.Community;

                QuestPDF.Fluent.Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Margin(20);
                        page.Size(PageSizes.A4);

                        // Header
                        page.Header().ShowOnce().BorderBottom(1).PaddingBottom(6).Row(r =>
                        {
                            r.RelativeItem().AlignLeft().Text("PRODUCTION REPORT")
                                .FontSize(16).Bold();
                            r.ConstantItem(160).AlignRight()
                                .Text($"Generated: {DateTime.Now:dd/MM/yyyy HH:mm}")
                                .FontSize(8).FontColor("#666666");
                        });

                        // Content
                        page.Content().PaddingTop(10).Column(col =>
                        {
                            col.Spacing(10);

                            foreach (var log in listOfLog)
                            {
                                try
                                {
                                    col.Item().Border(1).BorderColor("#CCCCCC").Column(batch =>
                                    {
                                        // Batch header
                                        batch.Item()
                                            .Background("#0D3B6E")
                                            .Padding(6).Row(r =>
                                            {
                                                r.RelativeItem().Text($"BatchName: {log.BatchName}")
                                                    .FontSize(11).Bold().FontColor("#FFFFFF");

                                                r.ConstantItem(420).AlignRight().Row(statsRow =>
                                                {
                                                    statsRow.AutoItem().AlignMiddle()
                                                        .Text("Start: ").FontSize(8).FontColor("#FFFFFF");
                                                    statsRow.AutoItem().AlignMiddle()
                                                        .Text($"{log.LogStartDate:dd/MM/yy HH:mm}").FontSize(8).FontColor("#FFFFFF");

                                                    statsRow.ConstantItem(6);
                                                    statsRow.AutoItem().AlignMiddle()
                                                        .Text("|").FontSize(8).FontColor("#AAAAAA");
                                                    statsRow.ConstantItem(6);

                                                    statsRow.AutoItem().AlignMiddle()
                                                        .Text("End: ").FontSize(8).FontColor("#FFFFFF");
                                                    statsRow.AutoItem().AlignMiddle()
                                                        .Text($"{log.LogEndDate:dd/MM/yy HH:mm}").FontSize(8).FontColor("#FFFFFF");

                                                    statsRow.ConstantItem(6);
                                                    statsRow.AutoItem().AlignMiddle()
                                                        .Text("|").FontSize(8).FontColor("#AAAAAA");
                                                    statsRow.ConstantItem(6);

                                                    statsRow.AutoItem().AlignMiddle()
                                                        .Text("Pass: ").FontSize(8).Bold().FontColor("#69F0AE");
                                                    statsRow.AutoItem().AlignMiddle()
                                                        .Text($"{log.PassCount}").FontSize(8).Bold().FontColor("#69F0AE");

                                                    statsRow.ConstantItem(6);
                                                    statsRow.AutoItem().AlignMiddle()
                                                        .Text("|").FontSize(8).FontColor("#AAAAAA");
                                                    statsRow.ConstantItem(6);

                                                    statsRow.AutoItem().AlignMiddle()
                                                        .Text("Fail: ").FontSize(8).Bold().FontColor("#FF5252");
                                                    statsRow.AutoItem().AlignMiddle()
                                                        .Text($"{log.FailCount}").FontSize(8).Bold().FontColor("#FF5252");

                                                    statsRow.ConstantItem(6);
                                                    statsRow.AutoItem().AlignMiddle()
                                                        .Text("|").FontSize(8).FontColor("#AAAAAA");
                                                    statsRow.ConstantItem(6);

                                                    statsRow.AutoItem().AlignMiddle()
                                                        .Text("Total: ").FontSize(8).Bold().FontColor("#40C4FF");
                                                    statsRow.AutoItem().AlignMiddle()
                                                        .Text($"{log.TotalCount}").FontSize(8).Bold().FontColor("#40C4FF");
                                                });
                                            });

                                        var details = batchDetails[log.BatchName];
                                        var recordIndex = 0;

                                        // Individual records
                                        foreach (var item in details)
                                        {
                                            try
                                            {
                                                recordIndex++;
                                                var part = JsonConvert.DeserializeObject<Part>(item.PartJson);
                                                var config = JsonConvert.DeserializeObject<Configuration>(item.ConfigurationJson);
                                                bool passed = item.Result;

                                                batch.Item().BorderTop(1).BorderColor("#DDDDDD")
                                                    .Padding(6).Column(record =>
                                                    {
                                                        // Record header
                                                        record.Item().Row(r =>
                                                        {
                                                            r.RelativeItem().Text($"#{recordIndex}  {item.TimeStamp:dd/MM/yyyy HH:mm:ss}")
                                                                .FontSize(9).Bold();
                                                            r.ConstantItem(50).AlignRight()
                                                                .Background(passed ? "#E8F5E9" : "#FFEBEE")
                                                                .Padding(2)
                                                                .Text(passed ? "PASS" : "FAIL")
                                                                .FontSize(9).Bold()
                                                                .FontColor(passed ? "#2E7D32" : "#C62828");
                                                        });

                                                        // Frequency table
                                                        if (config?.Frequency?.FD != null && config?.Filter?.FD != null)
                                                        {
                                                            var uniqueFD = config.Frequency.FD
                                                                .GroupBy(f => new { f.F, f.G, f.UTH, f.LTH, f.TH, f.PP })
                                                                .Select(g => g.First())
                                                                .ToList();

                                                            foreach (var f in uniqueFD)
                                                            {
                                                                try
                                                                {
                                                                    var filterRow = config.Filter.FD.FirstOrDefault(fd => fd.FN == f.FN);

                                                                    record.Item().PaddingTop(4).Row(r =>
                                                                    {
                                                                        void LabelValue(string label, string value)
                                                                        {
                                                                            r.AutoItem().PaddingRight(8).Row(inner =>
                                                                            {
                                                                                inner.AutoItem()
                                                                                    .Text(label + ": ")
                                                                                    .FontSize(8)
                                                                                    .FontColor("#333333");
                                                                                inner.AutoItem()
                                                                                    .Text(value)
                                                                                    .FontSize(8)
                                                                                    .FontColor("#333333");
                                                                            });
                                                                        }

                                                                        LabelValue("Freq", f.F.ToString());
                                                                        LabelValue("Gain", f.G.ToString());
                                                                        LabelValue("UTH", f.UTH.ToString());
                                                                        LabelValue("LTH", f.LTH.ToString());
                                                                        LabelValue("TH", f.TH.ToString());
                                                                        LabelValue("PP", f.PP.ToString());
                                                                        LabelValue("Low", filterRow?.L.ToString() ?? "-");
                                                                        LabelValue("High", filterRow?.H.ToString() ?? "-");
                                                                    });
                                                                }
                                                                catch (Exception ex)
                                                                {
                                                                    MessageBox.Show("Something went wrong. Please try again.");
                                                                }
                                                            }
                                                        }
                                                    });
                                            }
                                            catch (Exception ex)
                                            {
                                                MessageBox.Show("Something went wrong. Please try again.");
                                            }
                                        }
                                    });
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show("Something went wrong. Please try again.");
                                }
                            }
                        });

                        // Footer
                        page.Footer().AlignCenter().PaddingTop(4).Text(x =>
                        {
                            x.Span("Page ").FontSize(8);
                            x.CurrentPageNumber().FontSize(8);
                            x.Span(" of ").FontSize(8);
                            x.TotalPages().FontSize(8);
                        });
                    });
                })
                .GeneratePdf(filePath);

                MessageBox.Show("PDF Generated ✅");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Something went wrong. Please try again.");
            }
        }

        public List<(DateTime TimeStamp, string PartJson, string ConfigurationJson, bool Result)> GetBatchDetails(string BatchName)
        {
            try
            {
                var list = new List<(DateTime, string, string, bool)>();

                using (var con = new NpgsqlConnection(DeviceCOM.DBConnection))
                {
                    con.Open();

                    string sql = @"
                        SELECT ""TimeStamp"", ""PartJson"", ""ConfigurationJson"", ""Result""
                        FROM ""Logs""
                        WHERE 
                            ""BatchName"" = @batch
                            AND ""TimeStamp"" >= @StartDate
                            AND ""TimeStamp"" < @EndDate
                        ORDER BY ""TimeStamp"";
                    ";

                    var cmd = new NpgsqlCommand(sql, con);
                    cmd.Parameters.AddWithValue("@batch", BatchName);
                    cmd.Parameters.AddWithValue("@StartDate", clStartDate.SelectedDate.Value);
                    cmd.Parameters.AddWithValue("@EndDate", clToDate.SelectedDate.Value.AddDays(1));

                    var reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        list.Add((
                            reader.GetFieldValue<DateTime>(reader.GetOrdinal("TimeStamp")).ToLocalTime(),
                            reader["PartJson"].ToString(),
                            reader["ConfigurationJson"].ToString(),
                            reader.GetBoolean(reader.GetOrdinal("Result")) 
                        ));
                    }
                }

                return list;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Something went wrong. Please try again.");
                return new List<(DateTime, string, string, bool)>();
            }
        }
    }
}
