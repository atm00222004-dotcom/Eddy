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

        //private void btnDownload_MouseDown(object sender, MouseButtonEventArgs e)
        //{
        //    try
        //    {
        //        LoadLogs();
        //        if (listOfLog.Count > 0)
        //        {
        //            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
        //            dlg.FileName = "Report"; // Default file name
        //            dlg.DefaultExt = ".csv"; // Default file extension
        //            dlg.Filter = "CSV Files (.csv)|*.csv"; // Filter files by extension

        //            Nullable<bool> result = dlg.ShowDialog();

        //            if (result == true)
        //            {
        //                string conecnt = "Batch Name,Log Start Date,Log End Date,OK Count,Not OK Count,Total Count";
        //                foreach (var log in listOfLog)
        //                {
        //                    conecnt = conecnt + "\n";
        //                    conecnt = conecnt + log.BatchName + "," + log.LogStartDate + "," + log.LogEndDate + "," + log.PassCount.ToString() + "," + log.FailCount.ToString() + "," + log.TotalCount.ToString();
        //                }
        //                File.WriteAllText(dlg.FileName, conecnt);
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show("Something went wrong. Please try again.");
        //    }
        //}

        private void btnDownload_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                LoadLogs();

                if (listOfLog == null || listOfLog.Count == 0)
                {
                    MessageBox.Show("No data found");
                    return;
                }

                Microsoft.Win32.SaveFileDialog dlg =
                    new Microsoft.Win32.SaveFileDialog();

                dlg.FileName = "BatchSummaryReport";
                dlg.DefaultExt = ".pdf";
                dlg.Filter = "PDF Files (.pdf)|*.pdf";

                if (dlg.ShowDialog() != true)
                    return;

                var summaryDetails = GetBatchSummaryDetails();

                QuestPDF.Settings.License = LicenseType.Community;

                QuestPDF.Fluent.Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Margin(20);
                        page.Size(PageSizes.A4);

                        // HEADER
                        page.Header().ShowOnce()
                            .BorderBottom(2)
                            .BorderColor("#0D3B6E")
                            .PaddingBottom(8)
                            .Row(r =>
                            {
                                r.RelativeItem().Row(left =>
                                {
                                    var imagePath = System.IO.Path.Combine(
                                        AppDomain.CurrentDomain.BaseDirectory,
                                        "Assets",
                                        "Magkraft.jpg");

                                    var imageBytes = File.ReadAllBytes(imagePath);

                                    left.AutoItem()
                                        .Height(25)
                                        .Width(25)
                                        .AlignMiddle()
                                        .Image(imageBytes, ImageScaling.FitHeight);

                                    left.ConstantItem(12);

                                    left.AutoItem().AlignMiddle()
                                        .Text("TUBE EDDY REPORT")
                                        .FontSize(18)
                                        .Bold()
                                        .FontColor("#0D3B6E");
                                });

                                r.ConstantItem(180)
                                    .AlignRight()
                                    .AlignBottom()
                                    .Text($"Generated: {DateTime.Now:dd/MM/yyyy HH:mm}")
                                    .FontSize(8)
                                    .FontColor("#888888");
                            });

                        page.Content().PaddingTop(10).Column(col =>
                        {
                            col.Spacing(10);

                            foreach (var log in listOfLog)
                            {
                                try
                                {
                                    var batchData = summaryDetails
                                        .FirstOrDefault(x =>
                                            x.BatchName == log.BatchName);

                                    if (batchData == default)
                                        continue;

                                    var config =
                                        JsonConvert.DeserializeObject<Configuration>(
                                            batchData.ConfigurationJson);

                                    var part =
                                        JsonConvert.DeserializeObject<Part>(
                                            batchData.PartJson);

                                    col.Item()
                                        .Border(1)
                                        .BorderColor("#CCCCCC")
                                        .Column(batch =>
                                        {
                                            // BATCH HEADER
                                            batch.Item()
                                                .Background("#0D3B6E")
                                                .Padding(7)
                                                .PaddingLeft(12)
                                                .PaddingRight(12)
                                                .Row(r =>
                                                {
                                                    r.RelativeItem()
                                                        .Text($"BatchName: {log.BatchName}")
                                                        .FontSize(8)
                                                        .Bold()
                                                        .FontColor("#FFFFFF");

                                                    r.ConstantItem(420)
                                                        .AlignRight()
                                                        .Row(statsRow =>
                                                        {
                                                            statsRow.AutoItem()
                                                                .AlignMiddle()
                                                                .Text("Start: ")
                                                                .FontSize(8)
                                                                .FontColor("#ccd6e0");

                                                            statsRow.AutoItem()
                                                                .AlignMiddle()
                                                                .Text($"{log.LogStartDate}")
                                                                .FontSize(8)
                                                                .Bold()
                                                                .FontColor("#FFFFFF");

                                                            statsRow.ConstantItem(5);

                                                            statsRow.AutoItem()
                                                                .AlignMiddle()
                                                                .Text("|")
                                                                .FontSize(8)
                                                                .FontColor("#4a6a8a");

                                                            statsRow.ConstantItem(5);

                                                            statsRow.AutoItem()
                                                                .AlignMiddle()
                                                                .Text("End: ")
                                                                .FontSize(8)
                                                                .FontColor("#ccd6e0");

                                                            statsRow.AutoItem()
                                                                .AlignMiddle()
                                                                .Text($"{log.LogEndDate}")
                                                                .FontSize(8)
                                                                .Bold()
                                                                .FontColor("#FFFFFF");

                                                            statsRow.ConstantItem(5);

                                                            statsRow.AutoItem()
                                                                .AlignMiddle()
                                                                .Text("|")
                                                                .FontSize(8)
                                                                .FontColor("#4a6a8a");

                                                            statsRow.ConstantItem(5);

                                                            statsRow.AutoItem()
                                                                .AlignMiddle()
                                                                .Text("Pass: ")
                                                                .FontSize(8)
                                                                .FontColor("#ccd6e0");

                                                            statsRow.AutoItem()
                                                                .AlignMiddle()
                                                                .Text($"{log.PassCount}")
                                                                .FontSize(8)
                                                                .Bold()
                                                                .FontColor("#69F0AE");

                                                            statsRow.ConstantItem(5);

                                                            statsRow.AutoItem()
                                                                .AlignMiddle()
                                                                .Text("|")
                                                                .FontSize(8)
                                                                .FontColor("#4a6a8a");

                                                            statsRow.ConstantItem(5);

                                                            statsRow.AutoItem()
                                                                .AlignMiddle()
                                                                .Text("Fail: ")
                                                                .FontSize(8)
                                                                .FontColor("#ccd6e0");

                                                            statsRow.AutoItem()
                                                                .AlignMiddle()
                                                                .Text($"{log.FailCount}")
                                                                .FontSize(8)
                                                                .Bold()
                                                                .FontColor("#FF5252");

                                                            statsRow.ConstantItem(5);

                                                            statsRow.AutoItem()
                                                                .AlignMiddle()
                                                                .Text("|")
                                                                .FontSize(8)
                                                                .FontColor("#4a6a8a");

                                                            statsRow.ConstantItem(5);

                                                            statsRow.AutoItem()
                                                                .AlignMiddle()
                                                                .Text("Total: ")
                                                                .FontSize(8)
                                                                .FontColor("#ccd6e0");

                                                            statsRow.AutoItem()
                                                                .AlignMiddle()
                                                                .Text($"{log.TotalCount}")
                                                                .FontSize(8)
                                                                .Bold()
                                                                .FontColor("#40C4FF");
                                                        });
                                                });

                                            // COMMON LABEL FUNCTION
                                            void LV(IContainer c,
                                                    string label,
                                                    string value)
                                            {
                                                c.Text(t =>
                                                {
                                                    t.Span($"{label}: ")
                                                        .FontSize(7)
                                                        .FontColor("#888888");

                                                    t.Span(value ?? "-")
                                                        .FontSize(7)
                                                        .Bold()
                                                        .FontColor("#111111");
                                                });
                                            }

                                            // PART DETAILS
                                            if (part != null)
                                            {
                                                batch.Item()
                                                    .Padding(7)
                                                    .PaddingLeft(12)
                                                    .PaddingRight(12)
                                                    .Column(record =>
                                                    {
                                                        record.Item()
                                                            .Text("PART DETAILS")
                                                            .FontSize(6.5f)
                                                            .Bold()
                                                            .FontColor("#888888");

                                                        record.Item()
                                                            .PaddingTop(2)
                                                            .Row(r =>
                                                            {
                                                                r.RelativeItem()
                                                                    .Element(c =>
                                                                        LV(c,
                                                                            "Grade",
                                                                            part.Grade));

                                                                r.RelativeItem()
                                                                    .Element(c =>
                                                                        LV(c,
                                                                            "Place",
                                                                            part.Placce));

                                                                r.RelativeItem()
                                                                    .Element(c =>
                                                                        LV(c,
                                                                            "Checked By",
                                                                            part.CheckedBy));

                                                                r.RelativeItem()
                                                                    .Element(c =>
                                                                        LV(c,
                                                                            "Company",
                                                                            part.CompanyName));
                                                            });
                                                    });
                                            }

                                            // FREQUENCY & FILTER
                                            if (config?.Frequency?.FD != null &&
                                                config?.Filter?.FD != null)
                                            {
                                                var uniqueFD = config.Frequency.FD
                                                    .GroupBy(f => new
                                                    {
                                                        f.F,
                                                        f.G,
                                                        f.UTH,
                                                        f.LTH,
                                                        f.TH,
                                                        f.PP
                                                    })
                                                    .Select(g => g.First())
                                                    .ToList();

                                                foreach (var f in uniqueFD)
                                                {
                                                    try
                                                    {
                                                        var filterRow =
                                                            config.Filter.FD
                                                            .FirstOrDefault(fd =>
                                                                fd.FN == f.FN);

                                                        batch.Item()
                                                            .Padding(7)
                                                            .PaddingLeft(12)
                                                            .PaddingRight(12)
                                                            .Column(record =>
                                                            {
                                                                record.Item()
                                                                    .Text("FREQUENCY & FILTER")
                                                                    .FontSize(6.5f)
                                                                    .Bold()
                                                                    .FontColor("#888888");

                                                                record.Item()
                                                                    .PaddingTop(2)
                                                                    .Row(r =>
                                                                    {
                                                                        r.RelativeItem()
                                                                            .Element(c =>
                                                                                LV(c,
                                                                                    "Frequency(KHz)",
                                                                                    (f.F / 1000).ToString()));

                                                                        r.RelativeItem()
                                                                            .Element(c =>
                                                                                LV(c,
                                                                                    "Pre Gain(dB)",
                                                                                    f.G.ToString()));

                                                                        r.RelativeItem()
                                                                            .Element(c =>
                                                                                LV(c,
                                                                                    "Phase",
                                                                                    f.PP.ToString()));

                                                                        r.RelativeItem()
                                                                            .Element(c =>
                                                                                LV(c,
                                                                                    "High Threshold",
                                                                                    f.UTH.ToString()));
                                                                    });

                                                                record.Item()
                                                                    .PaddingTop(1)
                                                                    .Row(r =>
                                                                    {
                                                                        r.RelativeItem()
                                                                            .Element(c =>
                                                                                LV(c,
                                                                                    "Low Threshold",
                                                                                    f.LTH.ToString()));

                                                                        r.RelativeItem()
                                                                            .Element(c =>
                                                                                LV(c,
                                                                                    "Third Threshold",
                                                                                    f.TH.ToString()));

                                                                        r.RelativeItem()
                                                                            .Element(c =>
                                                                                LV(c,
                                                                                    "High Pass Filter",
                                                                                    filterRow?.H.ToString() ?? "-"));

                                                                        r.RelativeItem()
                                                                            .Element(c =>
                                                                                LV(c,
                                                                                    "Low Pass Filter",
                                                                                    filterRow?.L.ToString() ?? "-"));
                                                                    });
                                                            });
                                                    }
                                                    catch
                                                    {
                                                        MessageBox.Show("Something went wrong. Please try again.");
                                                    }
                                                }
                                            }
                                        });
                                }
                                catch
                                {
                                    MessageBox.Show("Something went wrong. Please try again.");
                                }
                            }
                        });

                        // FOOTER
                        page.Footer()
                            .AlignCenter()
                            .PaddingTop(4)
                            .Text(x =>
                            {
                                x.Span("Page ").FontSize(8);
                                x.CurrentPageNumber().FontSize(8);
                                x.Span(" of ").FontSize(8);
                                x.TotalPages().FontSize(8);
                            });
                    });
                })
                .GeneratePdf(dlg.FileName);

                MessageBox.Show("PDF Generated Successfully");
            }
            catch
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
                //var batchDetails = new Dictionary<string, List<(DateTime TimeStamp,  string ConfigurationJson, bool Result, string BatchName)>>();

                var batchDetails = new Dictionary<string, List<(DateTime TimeStamp,string ConfigurationJson,string GraphDataJson,bool Result,string BatchName)>>();

                // DB Call to get all the records and filter them locally to avoid multiple DB calls while generating PDF. This will improve the performance significantly when there are multiple batches and each batch has multiple records.7
                var data = GetBatchDetails();
                foreach (var log in listOfLog)
                    batchDetails[log.BatchName] = data.Where(d => d.BatchName == log.BatchName).OrderBy(d=> d.TimeStamp).ToList();

                QuestPDF.Settings.License = LicenseType.Community;

                QuestPDF.Fluent.Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Margin(20);
                        page.Size(PageSizes.A4);

                        page.Header().ShowOnce().BorderBottom(2).BorderColor("#0D3B6E").PaddingBottom(8).Row(r =>
                        {
                            r.RelativeItem().Row(left =>
                            {
                                var imagePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Magkraft.jpg");
                                var imageBytes = File.ReadAllBytes(imagePath);

                                left.AutoItem()
                                    .Height(25)                      // Slightly taller for better visibility
                                    .Width(25)                       // Keep square for your logo
                                    .AlignMiddle()                   // Vertically center
                                    .Image(imageBytes, ImageScaling.FitHeight); // Maintain aspect ratio

                                left.ConstantItem(12);              // Space between logo and text

                                left.AutoItem().AlignMiddle()
                                    .Text("TUBE EDDY REPORT")
                                    .FontSize(18)
                                    .Bold()
                                    .FontColor("#0D3B6E");
                            });

                            r.ConstantItem(180).AlignRight().AlignBottom()
                                .Text($"Generated: {DateTime.Now:dd/MM/yyyy HH:mm}")
                                .FontSize(8)
                                .FontColor("#888888");
                        });

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
                                            .Padding(7).PaddingLeft(12).PaddingRight(12)
                                            .Row(r =>
                                            {
                                                r.RelativeItem().Text($"BatchName: {log.BatchName}")
                                                    .FontSize(8).Bold().FontColor("#FFFFFF");

                                                r.ConstantItem(420).AlignRight().Row(statsRow =>
                                                {
                                                    statsRow.AutoItem().AlignMiddle()
                                                        .Text("Start: ").FontSize(8).FontColor("#ccd6e0");
                                                    statsRow.AutoItem().AlignMiddle()
                                                        .Text($"{log.LogStartDate:dd/MM/yy HH:mm}").FontSize(8).Bold().FontColor("#FFFFFF");

                                                    statsRow.ConstantItem(5);
                                                    statsRow.AutoItem().AlignMiddle()
                                                        .Text("|").FontSize(8).FontColor("#4a6a8a");
                                                    statsRow.ConstantItem(5);

                                                    statsRow.AutoItem().AlignMiddle()
                                                        .Text("End: ").FontSize(8).FontColor("#ccd6e0");
                                                    statsRow.AutoItem().AlignMiddle()
                                                        .Text($"{log.LogEndDate:dd/MM/yy HH:mm}").FontSize(8).Bold().FontColor("#FFFFFF");

                                                    statsRow.ConstantItem(5);
                                                    statsRow.AutoItem().AlignMiddle()
                                                        .Text("|").FontSize(8).FontColor("#4a6a8a");
                                                    statsRow.ConstantItem(5);

                                                    statsRow.AutoItem().AlignMiddle()
                                                        .Text("Pass: ").FontSize(8).FontColor("#ccd6e0");
                                                    statsRow.AutoItem().AlignMiddle()
                                                        .Text($"{log.PassCount}").FontSize(8).Bold().FontColor("#69F0AE");

                                                    statsRow.ConstantItem(5);
                                                    statsRow.AutoItem().AlignMiddle()
                                                        .Text("|").FontSize(8).FontColor("#4a6a8a");
                                                    statsRow.ConstantItem(5);

                                                    statsRow.AutoItem().AlignMiddle()
                                                        .Text("Fail: ").FontSize(8).FontColor("#ccd6e0");
                                                    statsRow.AutoItem().AlignMiddle()
                                                        .Text($"{log.FailCount}").FontSize(8).Bold().FontColor("#FF5252");

                                                    statsRow.ConstantItem(5);
                                                    statsRow.AutoItem().AlignMiddle()
                                                        .Text("|").FontSize(8).FontColor("#4a6a8a");
                                                    statsRow.ConstantItem(5);

                                                    statsRow.AutoItem().AlignMiddle()
                                                        .Text("Total: ").FontSize(8).FontColor("#ccd6e0");
                                                    statsRow.AutoItem().AlignMiddle()
                                                        .Text($"{log.TotalCount}").FontSize(8).Bold().FontColor("#40C4FF");
                                                });
                                            });

                                        var details = batchDetails[log.BatchName];
                                        var recordIndex = 0;

                                        foreach (var item in details)
                                        {
                                            try
                                            {
                                                recordIndex++;
                                                //var part = JsonConvert.DeserializeObject<Part>(item.PartJson);
                                                var config = JsonConvert.DeserializeObject<Configuration>(item.ConfigurationJson);
                                                var graph = !string.IsNullOrEmpty(item.GraphDataJson)? JsonConvert.DeserializeObject<GraphData>(item.GraphDataJson): null;
                                                bool passed = item.Result;

                                                string rowBg = (recordIndex % 2 == 0) ? "#FAFAFA" : "#FFFFFF";

                                                batch.Item()
                                                    .BorderTop(1).BorderColor("#E8E8E8")
                                                    .Background(rowBg)
                                                    .Padding(7).PaddingLeft(12).PaddingRight(12)
                                                    .Column(record =>
                                                    {
                                                        // Record header
                                                        record.Item().Row(r =>
                                                        {
                                                            r.RelativeItem().Text($"#{recordIndex}  {item.TimeStamp:dd/MM/yyyy HH:mm:ss}")
                                                                .FontSize(9).Bold().FontColor("#333333");

                                                            r.ConstantItem(50).AlignRight()
                                                                .Background(passed ? "#E8F5E9" : "#FFEBEE")
                                                                .Padding(2)
                                                                .Text(passed ? "PASS" : "FAIL")
                                                                .FontSize(8).Bold()
                                                                .FontColor(passed ? "#2E7D32" : "#C62828");
                                                        });

                                                        void LV(IContainer c, string label, string value)
                                                        {
                                                            c.Text(t =>
                                                            {
                                                                t.Span($"{label}: ").FontSize(7).FontColor("#888888");
                                                                t.Span(value).FontSize(7).Bold().FontColor("#111111");
                                                            });
                                                        }

                                                        // ── FREQUENCY & FILTER 
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

                                                                    record.Item().PaddingTop(5).Text("FREQUENCY & FILTER")
                                                                        .FontSize(6.5f).Bold().FontColor("#888888");
                   
                                                                    record.Item().PaddingTop(2).Row(r =>
                                                                    {
                                                                        r.RelativeItem().Element(c => LV(c, "Frequency(KHz)", (f.F / 1000).ToString()));
                                                                        r.RelativeItem().Element(c => LV(c, "Pre Gain(dB)", f.G.ToString()));
                                                                        r.RelativeItem().Element(c => LV(c, "Phase", f.PP.ToString()));
                                                                        r.RelativeItem().Element(c => LV(c, "High Threshold", f.UTH.ToString()));
                                                                    });

                                                                    record.Item().PaddingTop(1).Row(r =>
                                                                    {
                                                                        r.RelativeItem().Element(c => LV(c, "Low Threshold", f.LTH.ToString()));
                                                                        r.RelativeItem().Element(c => LV(c, "Third Threshold", f.TH.ToString()));
                                                                        r.RelativeItem().Element(c => LV(c, "High Pass Filter", filterRow?.H.ToString() ?? "-"));
                                                                        r.RelativeItem().Element(c => LV(c, "Low Pass Filter", filterRow?.L.ToString() ?? "-"));
                                                                    });

                                                                }
                                                                catch
                                                                {
                                                                    MessageBox.Show("Something went wrong. Please try again.");
                                                                }
                                                            }
                                                        }

                                                        // MARKER 
                                                        if (config?.Marker != null)
                                                        {
                                                            var m = config.Marker;

                                                            record.Item().PaddingTop(5).Text("MARKER")
                                                                .FontSize(6.5f).Bold().FontColor("#888888");

                                                            record.Item().PaddingTop(2).Row(r =>
                                                            {
                                                                r.RelativeItem().Element(c => LV(c, "Marker1(ms)", m.M1.ToString()));
                                                                r.RelativeItem().Element(c => LV(c, "Marker2(ms)", m.M2.ToString()));
                                                                r.RelativeItem().Element(c => LV(c, "Front Delay(ms)", m.FmS.ToString()));
                                                                r.RelativeItem().Element(c => LV(c, "Rear Delay(ms)", m.RmS.ToString()));
                                                            });

                                                            record.Item().PaddingTop(1).Row(r =>
                                                            {
                                                                r.RelativeItem().Element(c => LV(c, "Paint Spray Time(ms)", m.P1mS.ToString()));
                                                                r.RelativeItem().Element(c => LV(c, "C1 to C2 Sensor Distance(mm)", m.C1C2.ToString()));
                                                                r.RelativeItem().Element(c => LV(c, "C2 to Exit Sensor Distance(mm)", m.C2E.ToString()));
                                                                r.RelativeItem().Element(c => LV(c, "C Coil to C2 Distance(mm)", m.CC2.ToString()));
                                                            });
                                                        }

                                                        // Amp Details (with percentage)
                                                        if (!passed && graph?.AmpD1 != null && config?.Frequency?.FD != null)
                                                        {
                                                            const double totalValue = 32768;

                                                            foreach (var freq in config.Frequency.FD)
                                                            {
                                                                int UTH = freq.UTH;
                                                                int LTH = freq.LTH;

                                                                var invalidAmps = graph.AmpD1
                                                                    .Where(a => a.Amp < LTH || a.Amp > UTH)
                                                                    .Select(a => a.Amp)
                                                                    .ToList();

                                                                if (invalidAmps.Any())
                                                                {
                                                                    var maxAmp = invalidAmps.Max();

                                                                    // percentage calculation
                                                                    double percent = (maxAmp * 100.0) / totalValue;

                                                                    record.Item().PaddingTop(5).Text("AMP (Out of Threshold)")
                                                                        .FontSize(6.5f)
                                                                        .Bold()
                                                                        .FontColor("#C62828");

                                                                    record.Item().PaddingTop(2).Text(
                                                                        $"{percent:F2}%"
                                                                    )
                                                                    .FontSize(7)
                                                                    .FontColor("#111111");

                                                                    break;
                                                                }
                                                            }
                                                        }

                                                        //Amp Details
                                                        //if ( 1== 0 && !passed && graph?.AmpD1 != null && config?.Frequency?.FD != null)
                                                        //{
                                                        //    foreach (var freq in config.Frequency.FD)
                                                        //    {
                                                        //        int UTH = freq.UTH;
                                                        //        int LTH = freq.LTH;

                                                        //        var invalidAmps = graph.AmpD1
                                                        //            .Where(a => a.Amp < LTH || a.Amp > UTH)
                                                        //            .Select(a => a.Amp)
                                                        //            .ToList();

                                                        //        if (invalidAmps.Any())
                                                        //        {
                                                        //            record.Item().PaddingTop(5).Text("AMP (Out of Threshold)")
                                                        //                .FontSize(6.5f).Bold().FontColor("#C62828");

                                                        //            record.Item().PaddingTop(2).Text(string.Join(", ", invalidAmps))
                                                        //                .FontSize(7)
                                                        //                .FontColor("#111111");

                                                        //            break; 
                                                        //        }
                                                        //    }
                                                        //}

                                                    });
                                            }
                                            catch
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

        private void DownloadPdf_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                var selectedLog = button.Tag as LogData;

                if (selectedLog == null)
                    return;

                GenerateSingleBatchPdf(selectedLog);
            }
            catch
            {
                MessageBox.Show("Error generating PDF");
            }
        }

        private void GenerateSingleBatchPdf(LogData selectedLog)
        {
            try
            {
                if (selectedLog == null)
                {
                    MessageBox.Show("Invalid batch");
                    return;
                }

                // Backup original list
                var originalList = listOfLog;

                // ✅ Replace with ONLY selected batch
                listOfLog = new List<LogData> { selectedLog };

                // File dialog
                Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
                dlg.FileName = $"{selectedLog.BatchName}_Report";
                dlg.DefaultExt = ".pdf";
                dlg.Filter = "PDF Files (.pdf)|*.pdf";

                if (dlg.ShowDialog() == true)
                {
                    // ✅ Reuse SAME method
                    GeneratePdf(dlg.FileName);
                }

                // Restore original list
                listOfLog = originalList;
            }
            catch (Exception)
            {
                MessageBox.Show("Something went wrong. Please try again.");
            }
        }

        public List<(DateTime TimeStamp,string ConfigurationJson,string GraphDataJson,bool Result,string BatchName)> GetBatchDetails()
        {
            var list = new List<(DateTime,string,string,bool,string)>();

            try
            {
                using (var con = new NpgsqlConnection(DeviceCOM.DBConnection))
                {
                    con.Open();

                    string query = @"
            SELECT 
                ""TimeStamp"",
                ""ConfigurationJson"",
                ""GraphDataJson"",
                ""Result"",
                ""BatchName""
            FROM ""Logs""
            WHERE ""BatchName"" ILIKE '%' || @BatchName || '%'
                AND ""TimeStamp"" >= @StartDate
                AND ""TimeStamp"" < @EndDate
            ORDER BY ""BatchName""
            ";

                    using (var cmd = new NpgsqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@BatchName", txtBatchName.Text ?? "");
                        cmd.Parameters.AddWithValue("@StartDate", clStartDate.SelectedDate.Value);
                        cmd.Parameters.AddWithValue("@EndDate", clToDate.SelectedDate.Value.AddDays(1));

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                list.Add((
                                    reader.GetFieldValue<DateTime>(
                                        reader.GetOrdinal("TimeStamp")).ToLocalTime(),

                                    reader["ConfigurationJson"]?.ToString(),

                                    reader["GraphDataJson"]?.ToString(),

                                    reader.GetBoolean(
                                        reader.GetOrdinal("Result")),

                                    reader["BatchName"]?.ToString()
                                ));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Something went wrong. Please try again.");
            }

            return list;
        }

        public List<(string BatchName,DateTime TimeStamp,string ConfigurationJson,string PartJson)> GetBatchSummaryDetails()
        {
            var list = new List<(string,DateTime,string,string)>();

            try
            {
                using (var con = new NpgsqlConnection(DeviceCOM.DBConnection))
                {
                    con.Open();

                    string query = @"
                        SELECT DISTINCT ON (""BatchName"")
                            ""BatchName"",
                            ""TimeStamp"",
                            ""ConfigurationJson"",
                            ""PartJson""
                        FROM ""Logs""
                        WHERE ""BatchName"" ILIKE '%' || @BatchName || '%'
                            AND ""TimeStamp"" >= @StartDate
                            AND ""TimeStamp"" < @EndDate
                        ORDER BY ""BatchName"", ""TimeStamp"" ASC
                        ";

                    using (var cmd = new NpgsqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue(
                            "@BatchName",
                            txtBatchName.Text ?? "");

                        cmd.Parameters.AddWithValue(
                            "@StartDate",
                            clStartDate.SelectedDate.Value);

                        cmd.Parameters.AddWithValue(
                            "@EndDate",
                            clToDate.SelectedDate.Value.AddDays(1));

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                list.Add((
                                    reader["BatchName"]?.ToString(),

                                    reader.GetFieldValue<DateTime>(
                                        reader.GetOrdinal("TimeStamp"))
                                        .ToLocalTime(),

                                    reader["ConfigurationJson"]?.ToString(),

                                    reader["PartJson"]?.ToString()
                                ));
                            }
                        }
                    }
                }
            }
            catch
            {
                MessageBox.Show("Something went wrong. Please try again.");
            }

            return list;
        }
    }
}
