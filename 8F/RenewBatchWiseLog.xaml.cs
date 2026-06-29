using Newtonsoft.Json;
using Npgsql;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Data;
using System.IO;
using System.Reflection.Metadata;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace _8F
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class RenewBatchWiseLog : Window
    {
        public bool IsSaved = false;

        public List<LogData> listOfLog;

        private bool IsReNewConfig =>Convert.ToBoolean(System.Configuration.ConfigurationSettings.AppSettings["isrenewconfig"]);

        private string BatchCaption => IsReNewConfig ? "Shift" : "Batch Name";

        public RenewBatchWiseLog()
        {
            InitializeComponent();

            ((DataGridTextColumn)grdlogs.Columns[2]).Header = BatchCaption;  // 0→2
            lblBatchName.Content = BatchCaption;

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
                    string sql = @"
                        SELECT 
                            ""BatchName"",
                            ""TimeStamp""::date AS ""LogDate"",
                            ""PartData"" ->> 'ProductionOrder' AS ""ProductionOrder"",
                            COUNT(1) FILTER (WHERE ""Result"" = 'true')  AS ""PassCount"",
                            COUNT(1) FILTER (WHERE ""Result"" = 'false') AS ""FailCount""
                        FROM public.""Logs""
                        WHERE ""BatchName"" LIKE '%" + txtBatchName.Text + @"%'
                          AND ""TimeStamp"" >= '" + clStartDate.Text + @"'
                          AND ""TimeStamp"" <= '" + Convert.ToDateTime(clToDate.Text).AddDays(1).ToString() + @"'
                        GROUP BY ""BatchName"", ""TimeStamp""::date, ""PartData"" ->> 'ProductionOrder'
                        ORDER BY ""TimeStamp""::date, ""PartData"" ->> 'ProductionOrder', ""BatchName""";

                    con.Open();
                    var cmd = new NpgsqlCommand(sql, con);
                    var dataReader = cmd.ExecuteReader();
                    DataTable dt = new DataTable();
                    dt.Load(dataReader);
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        LogData _part = new LogData();
                        _part.BatchName = dt.Rows[i]["BatchName"].ToString();
                        _part.ProductionOrder = dt.Rows[i]["ProductionOrder"] == DBNull.Value
                            ? null
                            : dt.Rows[i]["ProductionOrder"].ToString();
                        DateTime logDate = Convert.ToDateTime(dt.Rows[i]["LogDate"]);
                        _part.LogDateRaw = logDate;
                        _part.Date = logDate.ToString("dd/MM/yyyy");
                        _part.PassCount = Convert.ToInt32(dt.Rows[i]["PassCount"]);
                        _part.FailCount = Convert.ToInt32(dt.Rows[i]["FailCount"]);
                        _part.TotalCount = _part.PassCount + _part.FailCount;
                        listOfLog.Add(_part);
                    }
                    grdlogs.ItemsSource = listOfLog;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Something went wrong. Please try again.");
            }
        }

        private void btnDownload_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                LoadLogs();
                if (listOfLog.Count > 0)
                {
                    string from = Convert.ToDateTime(clStartDate.Text).ToString("dd-MM-yyyy");
                    string to = Convert.ToDateTime(clToDate.Text).ToString("dd-MM-yyyy");

                    Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
                    dlg.FileName = SafeFileName($"SummaryReport_{from}_to_{to}");
                    dlg.DefaultExt = ".pdf";
                    dlg.Filter = "PDF Files (*.pdf)|*.pdf";

                    Nullable<bool> result = dlg.ShowDialog();

                    if (result == true)
                    {
                        GenerateSummaryPdf(dlg.FileName);
                        MessageBox.Show("PDF Generated Successfully");
                    }
                }
                else
                {
                    MessageBox.Show("No data found.");
                }
            }
            catch (Exception)
            {
                MessageBox.Show("PDF generation failed.");
            }
        }

        public void GenerateSummaryPdf(string filePath)
        {
            try
            {
                var logs = listOfLog;

                QuestPDF.Settings.License = LicenseType.Community;

                QuestPDF.Fluent.Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Margin(20);
                        page.Size(PageSizes.A4);

                        // ----- HEADER (logo + title) -----
                        page.Header()
                            .ShowOnce()
                            .BorderBottom(2)
                            .BorderColor("#0D3B6E")
                            .PaddingBottom(8)
                            .Row(r =>
                            {
                                r.RelativeItem().Row(left =>
                                {
                                    var imagePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Magkraft.jpg");
                                    var imageBytes = File.ReadAllBytes(imagePath);

                                    left.AutoItem().Height(25).Width(25).AlignMiddle().Image(imageBytes, ImageScaling.FitHeight);
                                    left.ConstantItem(10);
                                    left.AutoItem().AlignMiddle().Text("SHORTER EDDY REPORT").FontSize(18).Bold().FontColor("#0D3B6E");
                                });

                                r.ConstantItem(180).AlignRight().AlignBottom()
                                    .Text($"Generated: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8).FontColor("#888888");
                            });

                        // ----- CONTENT (summary table) -----
                        page.Content()
                            .PaddingTop(10)
                            .Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(2);   // Production Order
                                    columns.RelativeColumn(2);   // Date
                                    columns.RelativeColumn(2);   // Shift / Batch
                                    columns.RelativeColumn(1);   // OK
                                    columns.RelativeColumn(1);   // Not OK
                                    columns.RelativeColumn(1);   // Total
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Background("#0D3B6E").Padding(5).Text("Production Order").FontSize(9).Bold().FontColor(Colors.White);
                                    header.Cell().Background("#0D3B6E").Padding(5).Text("Date").FontSize(9).Bold().FontColor(Colors.White);
                                    header.Cell().Background("#0D3B6E").Padding(5).Text(BatchCaption).FontSize(9).Bold().FontColor(Colors.White);
                                    header.Cell().Background("#0D3B6E").Padding(5).Text("OK Count").FontSize(9).Bold().FontColor(Colors.White);
                                    header.Cell().Background("#0D3B6E").Padding(5).Text("Not OK Count").FontSize(9).Bold().FontColor(Colors.White);
                                    header.Cell().Background("#0D3B6E").Padding(5).Text("Total Count").FontSize(9).Bold().FontColor(Colors.White);
                                });

                                int rowIndex = 0;
                                foreach (var log in logs)
                                {
                                    rowIndex++;
                                    string bg = rowIndex % 2 == 0 ? "#FAFAFA" : "#FFFFFF";

                                    table.Cell().Background(bg).Border(1).BorderColor("#E8E8E8").Padding(5).Text(log.ProductionOrder ?? "-").FontSize(8);
                                    table.Cell().Background(bg).Border(1).BorderColor("#E8E8E8").Padding(5).Text(log.Date).FontSize(8);
                                    table.Cell().Background(bg).Border(1).BorderColor("#E8E8E8").Padding(5).Text(log.BatchName).FontSize(8);
                                    table.Cell().Background(bg).Border(1).BorderColor("#E8E8E8").Padding(5).Text(log.PassCount.ToString()).FontSize(8).FontColor("#2E7D32");
                                    table.Cell().Background(bg).Border(1).BorderColor("#E8E8E8").Padding(5).Text(log.FailCount.ToString()).FontSize(8).FontColor("#C62828");
                                    table.Cell().Background(bg).Border(1).BorderColor("#E8E8E8").Padding(5).Text(log.TotalCount.ToString()).FontSize(8).Bold();
                                }
                            });

                        // ----- FOOTER (page numbers) -----
                        page.Footer()
                            .AlignCenter()
                            .Text(x =>
                            {
                                x.Span("Page ");
                                x.CurrentPageNumber();
                                x.Span(" of ");
                                x.TotalPages();
                            });
                    });
                })
                .GeneratePdf(filePath);
            }
            catch
            {
                MessageBox.Show("Summary PDF generation failed.");
            }
        }

        private void btnDownloadPdf_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                LoadLogs();

                if (listOfLog == null || listOfLog.Count == 0)
                {
                    MessageBox.Show("No data found.");
                    return;
                }

                string from = Convert.ToDateTime(clStartDate.Text).ToString("dd-MM-yyyy");
                string to = Convert.ToDateTime(clToDate.Text).ToString("dd-MM-yyyy");

                Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
                dlg.FileName = SafeFileName($"DetailedReport_{from}_to_{to}");
                dlg.DefaultExt = ".pdf";
                dlg.Filter = "PDF Files (*.pdf)|*.pdf";

                if (dlg.ShowDialog() == true)
                {
                    GeneratePdf(dlg.FileName);
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Something went wrong. Please try again.");
            }
        }

        public void GeneratePdf(string filePath)
        {
            try
            {
                var logs = listOfLog;

                QuestPDF.Settings.License = LicenseType.Community;

                QuestPDF.Fluent.Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Margin(20);
                        page.Size(PageSizes.A4);

                        page.Header()
                            .ShowOnce()
                            .BorderBottom(2)
                            .BorderColor("#0D3B6E")
                            .PaddingBottom(8)
                            .Row(r =>
                            {
                                r.RelativeItem().Row(left =>
                                {
                                    var imagePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Magkraft.jpg");
                                    var imageBytes = File.ReadAllBytes(imagePath);

                                    left.AutoItem().Height(25).Width(25).AlignMiddle().Image(imageBytes, ImageScaling.FitHeight);
                                    left.ConstantItem(10);
                                    left.AutoItem().AlignMiddle().Text("SHORTER EDDY REPORT").FontSize(18).Bold().FontColor("#0D3B6E");
                                });

                                r.ConstantItem(180).AlignRight().AlignBottom()
                                    .Text($"Generated: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8).FontColor("#888888");
                            });

                        page.Content()
                            .PaddingTop(10)
                            .Column(col =>
                            {
                                col.Spacing(10);

                                foreach (var log in logs)
                                {
                                    col.Item()
                                        .Border(1)
                                        .BorderColor("#CCCCCC")
                                        .Column(batch =>
                                        {
                                            batch.Item()
                                                .Background("#0D3B6E")
                                                .Padding(7).PaddingLeft(12).PaddingRight(12)
                                                .Row(r =>
                                                {
                                                    r.RelativeItem().Text($"{BatchCaption} : {log.BatchName}").FontSize(8).Bold().FontColor("#FFFFFF");

                                                    r.ConstantItem(250).AlignRight().Row(stats =>
                                                    {
                                                        stats.AutoItem().Text($"OK : {log.PassCount}").FontSize(8).Bold().FontColor("#69F0AE");
                                                        stats.ConstantItem(10);
                                                        stats.AutoItem().Text($"NOT OK : {log.FailCount}").FontSize(8).Bold().FontColor("#FF5252");
                                                        stats.ConstantItem(10);
                                                        stats.AutoItem().Text($"TOTAL : {log.TotalCount}").FontSize(8).Bold().FontColor("#40C4FF");
                                                    });
                                                });

                                            var details = GetBatchDetails(log.BatchName, log.LogDateRaw, log.ProductionOrder);

                                            foreach (var group in details.GroupBy(d => BuildSettingsKey(d.FDData, d.PartData)))
                                            {
                                                List<GraphData> fdList = new();
                                                try { fdList = JsonConvert.DeserializeObject<List<GraphData>>(group.First().FDData); }
                                                catch { fdList = new(); }

                                                PartConfiguration part = null;
                                                try { part = JsonConvert.DeserializeObject<PartConfiguration>(group.First().PartData ?? "{}"); }
                                                catch { part = null; }

                                                batch.Item()
                                                    .BorderTop(1).BorderColor("#E8E8E8")
                                                    .Padding(7).PaddingLeft(12).PaddingRight(12)
                                                    .Column(grp =>
                                                    {
                                                        grp.Item()
                                                            .Border(1).BorderColor("#DDDDDD").Background("#FAFAFA").Padding(10)
                                                            .Column(section =>
                                                            {
                                                                section.Item().Text("CONFIGURATION SETTING").FontSize(8).Bold().FontColor("#0D3B6E");

                                                                section.Item().PaddingTop(5).Table(table =>
                                                                {
                                                                    table.ColumnsDefinition(columns =>
                                                                    {
                                                                        columns.RelativeColumn();
                                                                        columns.RelativeColumn();
                                                                        columns.RelativeColumn();
                                                                        columns.RelativeColumn();
                                                                        columns.RelativeColumn();
                                                                    });

                                                                    table.Header(header =>
                                                                    {
                                                                        header.Cell().Background("#0D3B6E").Padding(3).Text("Channel").FontColor(Colors.White);
                                                                        header.Cell().Background("#0D3B6E").Padding(3).Text("Frequency").FontColor(Colors.White);
                                                                        header.Cell().Background("#0D3B6E").Padding(3).Text("Gain").FontColor(Colors.White);
                                                                        header.Cell().Background("#0D3B6E").Padding(3).Text("Phase").FontColor(Colors.White);
                                                                        header.Cell().Background("#0D3B6E").Padding(3).Text("Status").FontColor(Colors.White);
                                                                    });

                                                                    foreach (var d in fdList)
                                                                    {
                                                                        table.Cell().Border(1).Padding(3).Text(d.Name);
                                                                        table.Cell().Border(1).Padding(3).Text(d.freq.ToString());
                                                                        table.Cell().Border(1).Padding(3).Text(d.gain.ToString());
                                                                        table.Cell().Border(1).Padding(3).Text(d.phase.ToString());
                                                                        table.Cell().Border(1).Padding(3).Text(d.isEnable ? "ON" : "OFF");
                                                                    }
                                                                });

                                                                section.Item().PaddingTop(10).Text("THRESHOLD SETTING").FontSize(8).Bold().FontColor("#0D3B6E");

                                                                section.Item().PaddingTop(5).Table(table =>
                                                                {
                                                                    table.ColumnsDefinition(columns =>
                                                                    {
                                                                        columns.RelativeColumn();
                                                                        columns.RelativeColumn();
                                                                        columns.RelativeColumn();
                                                                        columns.RelativeColumn();
                                                                        columns.RelativeColumn();
                                                                        columns.RelativeColumn();
                                                                    });

                                                                    table.Header(header =>
                                                                    {
                                                                        header.Cell().Background("#0D3B6E").Padding(3).Text("Channel").FontColor(Colors.White);
                                                                        header.Cell().Background("#0D3B6E").Padding(3).Text("Height").FontColor(Colors.White);
                                                                        header.Cell().Background("#0D3B6E").Padding(3).Text("Width").FontColor(Colors.White);
                                                                        header.Cell().Background("#0D3B6E").Padding(3).Text("Ex").FontColor(Colors.White);
                                                                        header.Cell().Background("#0D3B6E").Padding(3).Text("Ey").FontColor(Colors.White);
                                                                        header.Cell().Background("#0D3B6E").Padding(3).Text("Angle").FontColor(Colors.White);
                                                                    });

                                                                    foreach (var d in fdList)
                                                                    {
                                                                        table.Cell().Border(1).Padding(3).Text(d.Name);
                                                                        table.Cell().Border(1).Padding(3).Text(d.height.ToString("0.##"));
                                                                        table.Cell().Border(1).Padding(3).Text(d.width.ToString("0.##"));
                                                                        table.Cell().Border(1).Padding(3).Text(d.ex.ToString("0.##"));
                                                                        table.Cell().Border(1).Padding(3).Text(d.ey.ToString("0.##"));
                                                                        table.Cell().Border(1).Padding(3).Text(d.angel.ToString("0.##"));
                                                                    }
                                                                });

                                                                section.Item().PaddingTop(10).Text("PART CONFIGURATION").FontSize(8).Bold().FontColor("#0D3B6E");

                                                                section.Item().PaddingTop(5).Column(c =>
                                                                {
                                                                    if (IsReNewConfig)
                                                                    {
                                                                        c.Item().Row(r =>
                                                                        {
                                                                            r.RelativeItem(1).Element(x => LV(x, "Production Order", part?.ProductionOrder));
                                                                            r.ConstantItem(10);
                                                                            r.RelativeItem(1).Element(x => LV(x, "Machine Number", part?.MachineNumber));
                                                                            r.ConstantItem(10);
                                                                            r.RelativeItem(1).Element(x => LV(x, "Part Number", part?.PartNumber));
                                                                        });
                                                                        c.Item().PaddingTop(2).Row(r =>
                                                                        {
                                                                            r.RelativeItem(1).Element(x => LV(x, "Part Family", part?.PartFamily));
                                                                            r.ConstantItem(10);
                                                                            r.RelativeItem(1).Element(x => LV(x, "Shift", part?.BatchName));
                                                                            r.ConstantItem(10);
                                                                            r.RelativeItem(1).Element(x => LV(x, "Operator", part?.CheckedBy));
                                                                        });
                                                                    }
                                                                    else
                                                                    {
                                                                        c.Item().Row(r =>
                                                                        {
                                                                            r.RelativeItem(1).Element(x => LV(x, "Batch Name", part?.BatchName));
                                                                            r.ConstantItem(10);
                                                                            r.RelativeItem(1).Element(x => LV(x, "Part Name", part?.Name));
                                                                            r.ConstantItem(10);
                                                                            r.RelativeItem(1).Element(x => LV(x, "Grade", part?.Grade));
                                                                        });
                                                                        c.Item().PaddingTop(2).Row(r =>
                                                                        {
                                                                            r.RelativeItem(1).Element(x => LV(x, "Checked By", part?.CheckedBy));
                                                                            r.ConstantItem(10);
                                                                            r.RelativeItem(1).Element(x => LV(x, "Company Name", part?.CompanyName));
                                                                            r.ConstantItem(10);
                                                                            r.RelativeItem(1).Element(x => LV(x, "", ""));
                                                                        });
                                                                    }
                                                                });

                                                                // ---- RESULT RECORDS TABLE ----
                                                                section.Item().PaddingTop(10).Text("RESULTS").FontSize(8).Bold().FontColor("#0D3B6E");

                                                                section.Item().PaddingTop(5).Table(resultTable =>
                                                                {
                                                                    resultTable.ColumnsDefinition(cols =>
                                                                    {
                                                                        cols.ConstantColumn(35);   // Sr No
                                                                        cols.RelativeColumn();     // Timestamp
                                                                        cols.ConstantColumn(55);   // Result
                                                                    });

                                                                    resultTable.Header(h =>
                                                                    {
                                                                        h.Cell().Background("#0D3B6E").Padding(4).Text("Sr No").FontSize(8).Bold().FontColor(Colors.White);
                                                                        h.Cell().Background("#0D3B6E").Padding(4).Text("Timestamp").FontSize(8).Bold().FontColor(Colors.White);
                                                                        h.Cell().Background("#0D3B6E").Padding(4).Text("Result").FontSize(8).Bold().FontColor(Colors.White);
                                                                    });

                                                                    int recordIndex = 0;
                                                                    foreach (var item in group)
                                                                    {
                                                                        recordIndex++;
                                                                        string bg = recordIndex % 2 == 0 ? "#FAFAFA" : "#FFFFFF";

                                                                        resultTable.Cell().Background(bg).Border(1).BorderColor("#E8E8E8").Padding(4)
                                                                            .Text(recordIndex.ToString()).FontSize(8).FontColor("#333333");
                                                                        resultTable.Cell().Background(bg).Border(1).BorderColor("#E8E8E8").Padding(4)
                                                                            .Text($"{item.TimeStamp:dd/MM/yyyy HH:mm:ss}").FontSize(8).FontColor("#333333");
                                                                        resultTable.Cell().Background(item.Result ? "#E8F5E9" : "#FFEBEE").Border(1).BorderColor("#E8E8E8").Padding(4)
                                                                            .Text(item.Result ? "PASS" : "FAIL").FontSize(8).Bold()
                                                                            .FontColor(item.Result ? "#2E7D32" : "#C62828");
                                                                    }
                                                                });
                                                            });
                                                    });
                                            }
                                        });
                                }
                            });

                        page.Footer()
                            .AlignCenter()
                            .Text(x =>
                            {
                                x.Span("Page ");
                                x.CurrentPageNumber();
                                x.Span(" of ");
                                x.TotalPages();
                            });
                    });
                })
                .GeneratePdf(filePath);

                MessageBox.Show("PDF Generated Successfully");
            }
            catch
            {
                MessageBox.Show("Something went wrong. Please try again.");
            }
        }

        private List<BatchDetail> GetBatchDetails(string batchName, DateTime? logDate, string productionOrder)
        {
            try
            {
                List<BatchDetail> list = new List<BatchDetail>();

                using (var con = new NpgsqlConnection(System.Configuration.ConfigurationSettings.AppSettings["ConnectionString"]))
                {
                    con.Open();

                    string sql = @"SELECT ""TimeStamp"", ""BatchName"", ""Result"", ""FDData"", ""PartData""
                                   FROM public.""Logs""
                                   WHERE ""BatchName"" = @BatchName";

                    if (logDate.HasValue)
                        sql += @" AND ""TimeStamp""::date = @LogDate";

                    // match production order (handles NULL correctly via IS NOT DISTINCT FROM)
                    sql += @" AND ""PartData"" ->> 'ProductionOrder' IS NOT DISTINCT FROM @ProductionOrder";

                    sql += @" ORDER BY ""TimeStamp""";

                    NpgsqlCommand cmd = new NpgsqlCommand(sql, con);
                    cmd.Parameters.AddWithValue("@BatchName", batchName);

                    if (logDate.HasValue)
                        cmd.Parameters.AddWithValue("@LogDate", logDate.Value.Date);

                    cmd.Parameters.AddWithValue("@ProductionOrder",
                        (object)productionOrder ?? DBNull.Value);

                    DataTable dt = new DataTable();
                    dt.Load(cmd.ExecuteReader());

                    foreach (DataRow row in dt.Rows)
                    {
                        list.Add(new BatchDetail()
                        {
                            TimeStamp = Convert.ToDateTime(row["TimeStamp"]),
                            BatchName = row["BatchName"].ToString(),
                            Result = Convert.ToBoolean(row["Result"]),
                            FDData = row["FDData"].ToString(),
                            PartData = row["PartData"].ToString()
                        });
                    }
                }

                return list;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private void btnRowDownload_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Button btn = sender as Button;

                LogData log = btn.Tag as LogData;

                if (log == null)
                    return;

                string shiftPart = SafeFileName(BatchCaption.Replace(" ", "") + log.BatchName);
                string poPart = string.IsNullOrWhiteSpace(log.ProductionOrder)? "": "_" + SafeFileName(log.ProductionOrder);
                string datePart = "_" + log.LogDateRaw.ToString("dd-MM-yyyy");

                string fileName = $"DetailedReport_{shiftPart}{poPart}{datePart}";

                Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
                dlg.FileName = SafeFileName(fileName);
                dlg.DefaultExt = ".pdf";
                dlg.Filter = "PDF Files (*.pdf)|*.pdf";

                if (dlg.ShowDialog() == true)
                {
                    GeneratePdf(log, dlg.FileName);
                }
            }
            catch
            {
                MessageBox.Show("Something went wrong. Please try again.");
            }
        }

        public void GeneratePdf(LogData log, string filePath)
        {
            try
            {
                var details = GetBatchDetails(log.BatchName, log.LogDateRaw, log.ProductionOrder);

                QuestPDF.Settings.License = LicenseType.Community;

                QuestPDF.Fluent.Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Margin(20);
                        page.Size(PageSizes.A4);

                        page.Header()
                            .ShowOnce()
                            .BorderBottom(2)
                            .BorderColor("#0D3B6E")
                            .PaddingBottom(8)
                            .Row(r =>
                            {
                                r.RelativeItem().Row(left =>
                                {
                                    var imagePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Magkraft.jpg");
                                    var imageBytes = File.ReadAllBytes(imagePath);

                                    left.AutoItem().Height(25).Width(25).AlignMiddle().Image(imageBytes, ImageScaling.FitHeight);
                                    left.ConstantItem(10);
                                    left.AutoItem().AlignMiddle().Text("SHORTER EDDY REPORT").FontSize(18).Bold().FontColor("#0D3B6E");
                                });

                                r.ConstantItem(180).AlignRight().AlignBottom()
                                    .Text($"Generated: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8).FontColor("#888888");
                            });

                        page.Content()
                            .PaddingTop(10)
                            .Column(col =>
                            {
                                col.Item()
                                    .Border(1)
                                    .BorderColor("#CCCCCC")
                                    .Column(batch =>
                                    {
                                        batch.Item()
                                            .Background("#0D3B6E")
                                            .Padding(7).PaddingLeft(12).PaddingRight(12)
                                            .Row(r =>
                                            {
                                                r.RelativeItem().Text($"{BatchCaption} : {log.BatchName}").FontSize(8).Bold().FontColor("#FFFFFF");

                                                r.ConstantItem(250).AlignRight().Row(stats =>
                                                {
                                                    stats.AutoItem().Text($"OK : {log.PassCount}").FontSize(8).Bold().FontColor("#69F0AE");
                                                    stats.ConstantItem(10);
                                                    stats.AutoItem().Text($"NOT OK : {log.FailCount}").FontSize(8).Bold().FontColor("#FF5252");
                                                    stats.ConstantItem(10);
                                                    stats.AutoItem().Text($"TOTAL : {log.TotalCount}").FontSize(8).Bold().FontColor("#40C4FF");
                                                });
                                            });

                                        foreach (var group in details.GroupBy(d => BuildSettingsKey(d.FDData, d.PartData)))
                                        {
                                            List<GraphData> fdList = new();
                                            try { fdList = JsonConvert.DeserializeObject<List<GraphData>>(group.First().FDData); }
                                            catch { fdList = new(); }

                                            PartConfiguration part = null;
                                            try { part = JsonConvert.DeserializeObject<PartConfiguration>(group.First().PartData ?? "{}"); }
                                            catch { part = null; }

                                            batch.Item()
                                                .BorderTop(1).BorderColor("#E8E8E8")
                                                .Padding(7).PaddingLeft(12).PaddingRight(12)
                                                .Column(grp =>
                                                {
                                                    grp.Item()
                                                        .Border(1).BorderColor("#DDDDDD").Background("#FAFAFA").Padding(10)
                                                        .Column(section =>
                                                        {
                                                            section.Item().Text("CONFIGURATION SETTING").FontSize(8).Bold().FontColor("#0D3B6E");

                                                            section.Item().PaddingTop(5).Table(table =>
                                                            {
                                                                table.ColumnsDefinition(columns =>
                                                                {
                                                                    columns.RelativeColumn();
                                                                    columns.RelativeColumn();
                                                                    columns.RelativeColumn();
                                                                    columns.RelativeColumn();
                                                                    columns.RelativeColumn();
                                                                });

                                                                table.Header(header =>
                                                                {
                                                                    header.Cell().Background("#0D3B6E").Padding(3).Text("Channel").FontColor(Colors.White);
                                                                    header.Cell().Background("#0D3B6E").Padding(3).Text("Frequency").FontColor(Colors.White);
                                                                    header.Cell().Background("#0D3B6E").Padding(3).Text("Gain").FontColor(Colors.White);
                                                                    header.Cell().Background("#0D3B6E").Padding(3).Text("Phase").FontColor(Colors.White);
                                                                    header.Cell().Background("#0D3B6E").Padding(3).Text("Status").FontColor(Colors.White);
                                                                });

                                                                foreach (var d in fdList)
                                                                {
                                                                    table.Cell().Border(1).Padding(3).Text(d.Name);
                                                                    table.Cell().Border(1).Padding(3).Text(d.freq.ToString());
                                                                    table.Cell().Border(1).Padding(3).Text(d.gain.ToString());
                                                                    table.Cell().Border(1).Padding(3).Text(d.phase.ToString());
                                                                    table.Cell().Border(1).Padding(3).Text(d.isEnable ? "ON" : "OFF");
                                                                }
                                                            });

                                                            section.Item().PaddingTop(10).Text("THRESHOLD SETTING").FontSize(8).Bold().FontColor("#0D3B6E");

                                                            section.Item().PaddingTop(5).Table(table =>
                                                            {
                                                                table.ColumnsDefinition(columns =>
                                                                {
                                                                    columns.RelativeColumn();
                                                                    columns.RelativeColumn();
                                                                    columns.RelativeColumn();
                                                                    columns.RelativeColumn();
                                                                    columns.RelativeColumn();
                                                                    columns.RelativeColumn();
                                                                });

                                                                table.Header(header =>
                                                                {
                                                                    header.Cell().Background("#0D3B6E").Padding(3).Text("Channel").FontColor(Colors.White);
                                                                    header.Cell().Background("#0D3B6E").Padding(3).Text("Height").FontColor(Colors.White);
                                                                    header.Cell().Background("#0D3B6E").Padding(3).Text("Width").FontColor(Colors.White);
                                                                    header.Cell().Background("#0D3B6E").Padding(3).Text("Ex").FontColor(Colors.White);
                                                                    header.Cell().Background("#0D3B6E").Padding(3).Text("Ey").FontColor(Colors.White);
                                                                    header.Cell().Background("#0D3B6E").Padding(3).Text("Angle").FontColor(Colors.White);
                                                                });

                                                                foreach (var d in fdList)
                                                                {
                                                                    table.Cell().Border(1).Padding(3).Text(d.Name);
                                                                    table.Cell().Border(1).Padding(3).Text(d.height.ToString("0.##"));
                                                                    table.Cell().Border(1).Padding(3).Text(d.width.ToString("0.##"));
                                                                    table.Cell().Border(1).Padding(3).Text(d.ex.ToString("0.##"));
                                                                    table.Cell().Border(1).Padding(3).Text(d.ey.ToString("0.##"));
                                                                    table.Cell().Border(1).Padding(3).Text(d.angel.ToString("0.##"));
                                                                }
                                                            });

                                                            section.Item().PaddingTop(10).Text("PART CONFIGURATION").FontSize(8).Bold().FontColor("#0D3B6E");

                                                            section.Item().PaddingTop(5).Column(c =>
                                                            {
                                                                if (IsReNewConfig)
                                                                {
                                                                    c.Item().Row(r =>
                                                                    {
                                                                        r.RelativeItem(1).Element(x => LV(x, "Production Order", part?.ProductionOrder));
                                                                        r.ConstantItem(10);
                                                                        r.RelativeItem(1).Element(x => LV(x, "Machine Number", part?.MachineNumber));
                                                                        r.ConstantItem(10);
                                                                        r.RelativeItem(1).Element(x => LV(x, "Part Number", part?.PartNumber));
                                                                    });
                                                                    c.Item().PaddingTop(2).Row(r =>
                                                                    {
                                                                        r.RelativeItem(1).Element(x => LV(x, "Part Family", part?.PartFamily));
                                                                        r.ConstantItem(10);
                                                                        r.RelativeItem(1).Element(x => LV(x, "Shift", part?.BatchName));
                                                                        r.ConstantItem(10);
                                                                        r.RelativeItem(1).Element(x => LV(x, "Operator", part?.CheckedBy));
                                                                    });
                                                                }
                                                                else
                                                                {
                                                                    c.Item().Row(r =>
                                                                    {
                                                                        r.RelativeItem(1).Element(x => LV(x, "Batch Name", part?.BatchName));
                                                                        r.ConstantItem(10);
                                                                        r.RelativeItem(1).Element(x => LV(x, "Part Name", part?.Name));
                                                                        r.ConstantItem(10);
                                                                        r.RelativeItem(1).Element(x => LV(x, "Grade", part?.Grade));
                                                                    });
                                                                    c.Item().PaddingTop(2).Row(r =>
                                                                    {
                                                                        r.RelativeItem(1).Element(x => LV(x, "Checked By", part?.CheckedBy));
                                                                        r.ConstantItem(10);
                                                                        r.RelativeItem(1).Element(x => LV(x, "Company Name", part?.CompanyName));
                                                                        r.ConstantItem(10);
                                                                        r.RelativeItem(1).Element(x => LV(x, "", ""));
                                                                    });
                                                                }
                                                            });

                                                            // ---- RESULT RECORDS TABLE ----
                                                            section.Item().PaddingTop(10).Text("RESULTS").FontSize(8).Bold().FontColor("#0D3B6E");

                                                            section.Item().PaddingTop(5).Table(resultTable =>
                                                            {
                                                                resultTable.ColumnsDefinition(cols =>
                                                                {
                                                                    cols.ConstantColumn(35);   // Sr No
                                                                    cols.RelativeColumn();     // Timestamp
                                                                    cols.ConstantColumn(55);   // Result
                                                                });

                                                                resultTable.Header(h =>
                                                                {
                                                                    h.Cell().Background("#0D3B6E").Padding(4).Text("Sr No").FontSize(8).Bold().FontColor(Colors.White);
                                                                    h.Cell().Background("#0D3B6E").Padding(4).Text("Timestamp").FontSize(8).Bold().FontColor(Colors.White);
                                                                    h.Cell().Background("#0D3B6E").Padding(4).Text("Result").FontSize(8).Bold().FontColor(Colors.White);
                                                                });

                                                                int recordIndex = 0;
                                                                foreach (var item in group)
                                                                {
                                                                    recordIndex++;
                                                                    string bg = recordIndex % 2 == 0 ? "#FAFAFA" : "#FFFFFF";

                                                                    resultTable.Cell().Background(bg).Border(1).BorderColor("#E8E8E8").Padding(4)
                                                                        .Text(recordIndex.ToString()).FontSize(8).FontColor("#333333");
                                                                    resultTable.Cell().Background(bg).Border(1).BorderColor("#E8E8E8").Padding(4)
                                                                        .Text($"{item.TimeStamp:dd/MM/yyyy HH:mm:ss}").FontSize(8).FontColor("#333333");
                                                                    resultTable.Cell().Background(item.Result ? "#E8F5E9" : "#FFEBEE").Border(1).BorderColor("#E8E8E8").Padding(4)
                                                                        .Text(item.Result ? "PASS" : "FAIL").FontSize(8).Bold()
                                                                        .FontColor(item.Result ? "#2E7D32" : "#C62828");
                                                                }
                                                            });
                                                        });
                                                });
                                        }
                                    });
                            });

                        page.Footer()
                            .AlignCenter()
                            .Text(x =>
                            {
                                x.Span("Page ");
                                x.CurrentPageNumber();
                                x.Span(" of ");
                                x.TotalPages();
                            });
                    });
                })
                .GeneratePdf(filePath);

                MessageBox.Show("PDF Generated Successfully");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Something went wrong. Please try again.");
            }
        }

        void LV(IContainer c, string label, string value)
        {
            c.Text(t =>
            {
                t.Span($"{label}: ").FontSize(7).FontColor("#888888");
                t.Span(value ?? "-").FontSize(7).Bold().FontColor("#111111");
            });
        }

        private string BuildSettingsKey(string fdData, string partData)
        {
            try
            {
                var list = JsonConvert.DeserializeObject<List<GraphData>>(fdData) ?? new List<GraphData>();
                var sb = new StringBuilder();
                foreach (var d in list.OrderBy(x => x.Name))
                {
                    sb.Append(d.Name).Append('|').Append(d.freq).Append('|').Append(d.gain).Append('|')
                      .Append(d.phase).Append('|').Append(d.isEnable).Append('|').Append(d.height).Append('|')
                      .Append(d.width).Append('|').Append(d.ex).Append('|').Append(d.ey).Append('|')
                      .Append(d.angel).Append(';');
                }
                sb.Append("##PART##").Append(partData ?? "");
                return sb.ToString();
            }
            catch { return "INVALID_" + (fdData ?? "") + (partData ?? ""); }
        }

        private string SafeFileName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "";

            foreach (char c in System.IO.Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');

            return name.Trim();
        }
    }
}

