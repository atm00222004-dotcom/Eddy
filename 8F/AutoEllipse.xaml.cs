using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using _8F.Models;
using _8F.Services;

namespace _8F
{
    /// <summary>
    /// Interaction logic for AutoEllipse.xaml
    /// </summary>
    public partial class AutoEllipse : Window
    {
        public bool IsSaved { get; set; } = false;
        public DeviceCOM? portCOM { get; set; }

        private readonly Dictionary<int, DataTable> _channelTables = new();
        private readonly Dictionary<int, List<AutoEllipseTest>> _channelRawRecords = new();
        private readonly DispatcherTimer _acquisitionTimer;
        private readonly InspectionLogRepository _repository;

        private bool _isTestActive = false;
        private DateTime _testStartTime;
        private int _lastProcessedResponseIndex = 0;
        private DataTable? _activeTable;

        public AutoEllipse()
        {
            InitializeComponent();

            _repository = new InspectionLogRepository();

            _acquisitionTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            _acquisitionTimer.Tick += AcquisitionTimer_Tick;

            Loaded += AutoEllipse_Loaded;
        }

        private void AutoEllipse_Loaded(object sender, RoutedEventArgs e)
        {
            DeviceCOM.IsAutoEllipseActive = false;
            Unloaded += AutoEllipse_Unloaded;
            Closed += AutoEllipse_Closed;
            PopulateChannels();
        }

        private void Window_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
            {
                try { DragMove(); } catch { }
            }
        }

        private void AutoEllipse_Unloaded(object sender, RoutedEventArgs e)
        {
            DeviceCOM.IsAutoEllipseActive = false;
        }

        private void AutoEllipse_Closed(object? sender, EventArgs e)
        {
            DeviceCOM.IsAutoEllipseActive = false;
        }

        private void PopulateChannels()
        {
            cboChannel.Items.Clear();

            if (DeviceCOM.channelDatas == null || DeviceCOM.channelDatas.Count == 0)
            {
                cboChannel.Items.Add("Channel-1");
            }
            else
            {
                foreach (var ch in DeviceCOM.channelDatas)
                {
                    if (ch.Id <= DeviceCOM.ChannelNo)
                    {
                        cboChannel.Items.Add($"Channel-{ch.Id}");
                    }
                }
            }

            if (cboChannel.Items.Count > 0)
            {
                cboChannel.SelectedIndex = 0;
            }
        }

        private async void cboChannel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboChannel.SelectedIndex >= 0)
            {
                int chId = cboChannel.SelectedIndex + 1;
                await SetupChannelTableAndColumnsAsync(chId);
            }
        }

        private async Task SetupChannelTableAndColumnsAsync(int channelId)
        {
            dgTestResults.Columns.Clear();

            bool isNewTable = !_channelTables.ContainsKey(channelId);
            if (isNewTable)
            {
                DataTable dt = new DataTable();
                dt.Columns.Add("IsSelected", typeof(bool));
                dt.Columns.Add("DbId", typeof(long));
                dt.Columns.Add("TestName", typeof(string));
                dt.Columns.Add("Timestamp", typeof(string));

                var chData = DeviceCOM.channelDatas?.FirstOrDefault(c => c.Id == channelId);
                if (chData != null && chData.graphDatas != null)
                {
                    foreach (var freq in chData.graphDatas)
                    {
                        dt.Columns.Add($"F{freq.Id}", typeof(string));
                        dt.Columns.Add($"F{freq.Id}_X", typeof(double));
                        dt.Columns.Add($"F{freq.Id}_Y", typeof(double));

                        if (!dt.Columns.Contains(freq.Name)) dt.Columns.Add(freq.Name, typeof(string));
                        if (!dt.Columns.Contains($"{freq.Name}_X")) dt.Columns.Add($"{freq.Name}_X", typeof(double));
                        if (!dt.Columns.Contains($"{freq.Name}_Y")) dt.Columns.Add($"{freq.Name}_Y", typeof(double));
                    }
                }
                else
                {
                    dt.Columns.Add("F1", typeof(string));
                    dt.Columns.Add("F1_X", typeof(double));
                    dt.Columns.Add("F1_Y", typeof(double));
                }

                _channelTables[channelId] = dt;
                _channelRawRecords[channelId] = new List<AutoEllipseTest>();
            }

            _activeTable = _channelTables[channelId];
            dgTestResults.ItemsSource = _activeTable.DefaultView;

            dgTestResults.Columns.Add(new DataGridCheckBoxColumn
            {
                Header = "Include",
                Binding = new Binding("IsSelected"),
                Width = 50
            });

            dgTestResults.Columns.Add(new DataGridTextColumn
            {
                Header = "Test #",
                Binding = new Binding("TestName"),
                IsReadOnly = true,
                Width = 60
            });

            dgTestResults.Columns.Add(new DataGridTextColumn
            {
                Header = "Timestamp",
                Binding = new Binding("Timestamp"),
                IsReadOnly = true,
                Width = 90
            });

            var activeCh = DeviceCOM.channelDatas?.FirstOrDefault(c => c.Id == channelId);
            if (activeCh != null && activeCh.graphDatas != null)
            {
                foreach (var freq in activeCh.graphDatas)
                {
                    dgTestResults.Columns.Add(new DataGridTextColumn
                    {
                        Header = $"{freq.Name} (X,Y)",
                        Binding = new Binding($"F{freq.Id}"),
                        IsReadOnly = true,
                        Width = new DataGridLength(1, DataGridLengthUnitType.Star)
                    });
                }
            }

            FrameworkElementFactory btnFactory = new FrameworkElementFactory(typeof(Button));
            btnFactory.SetValue(Button.ContentProperty, "Delete");
            btnFactory.SetValue(Button.BackgroundProperty, new SolidColorBrush(Color.FromRgb(220, 38, 38)));
            btnFactory.SetValue(Button.ForegroundProperty, Brushes.White);
            btnFactory.SetValue(Button.FontWeightProperty, FontWeights.Bold);
            btnFactory.SetValue(Button.FontSizeProperty, 11.0);
            btnFactory.SetValue(Button.WidthProperty, 55.0);
            btnFactory.SetValue(Button.HeightProperty, 20.0);
            btnFactory.SetValue(Button.CursorProperty, System.Windows.Input.Cursors.Hand);
            btnFactory.AddHandler(Button.ClickEvent, new RoutedEventHandler(btnDeleteRow_Click));

            DataTemplate cellTemplate = new DataTemplate();
            cellTemplate.VisualTree = btnFactory;

            dgTestResults.Columns.Add(new DataGridTemplateColumn
            {
                Header = "Action",
                CellTemplate = cellTemplate,
                Width = 65
            });

            if (isNewTable)
            {
                lblStatus.Text = $"Loading Channel-{channelId} calibration data from PostgreSQL...";
                var dbRecords = await _repository.GetAutoEllipseTestsByChannelAsync(channelId);

                _channelRawRecords[channelId] = dbRecords;
                foreach (var record in dbRecords)
                {
                    DataRow row = _activeTable.NewRow();
                    row["IsSelected"] = false;
                    row["DbId"] = record.Id;
                    row["TestName"] = $"Test {record.TestNumber}";
                    row["Timestamp"] = record.TimeStamp.ToLocalTime().ToString("HH:mm:ss.fff");

                    if (!string.IsNullOrWhiteSpace(record.FrequencyValuesJson))
                    {
                        try
                        {
                            var freqDict = JsonConvert.DeserializeObject<Dictionary<string, Newtonsoft.Json.Linq.JObject>>(record.FrequencyValuesJson);
                            if (freqDict != null)
                            {
                                foreach (var kvp in freqDict)
                                {
                                    string rawKey = kvp.Key; // e.g. "F1", "D1", "1"
                                    string keyNum = new string(rawKey.Where(char.IsDigit).ToArray());

                                    double x = kvp.Value["x"]?.ToObject<double>() ?? 0.0;
                                    double y = kvp.Value["y"]?.ToObject<double>() ?? 0.0;

                                    string[] targetDisplayCols = new[] { rawKey, $"F{keyNum}", $"D{keyNum}" };
                                    string[] targetXCols = new[] { $"{rawKey}_X", $"F{keyNum}_X", $"D{keyNum}_X" };
                                    string[] targetYCols = new[] { $"{rawKey}_Y", $"F{keyNum}_Y", $"D{keyNum}_Y" };

                                    foreach (var cName in targetDisplayCols)
                                    {
                                        if (!string.IsNullOrEmpty(cName) && _activeTable.Columns.Contains(cName))
                                        {
                                            row[cName] = $"{x:F2}, {y:F2}";
                                        }
                                    }
                                    foreach (var cName in targetXCols)
                                    {
                                        if (!string.IsNullOrEmpty(cName) && _activeTable.Columns.Contains(cName))
                                        {
                                            row[cName] = x;
                                        }
                                    }
                                    foreach (var cName in targetYCols)
                                    {
                                        if (!string.IsNullOrEmpty(cName) && _activeTable.Columns.Contains(cName))
                                        {
                                            row[cName] = y;
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error parsing JSON for test {record.TestNumber}: {ex.Message}");
                        }
                    }

                    _activeTable.Rows.Add(row);
                }
            }

            int rowCount = _activeTable.Rows.Count;
            lblStatus.Text = $"Channel-{channelId} loaded. Captured test runs: {rowCount}";
            btnMakeEllipse.IsEnabled = rowCount > 0;
        }

        private void btnSelectAll_Click(object sender, RoutedEventArgs e)
        {
            if (_activeTable == null) return;
            foreach (DataRow row in _activeTable.Rows)
            {
                row["IsSelected"] = true;
            }
        }

        private void btnDeselectAll_Click(object sender, RoutedEventArgs e)
        {
            if (_activeTable == null) return;
            foreach (DataRow row in _activeTable.Rows)
            {
                row["IsSelected"] = false;
            }
        }

        private void btnDeleteRow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is DataRowView rowView)
            {
                DataRow row = rowView.Row;
                string testName = row.Field<string>("TestName") ?? "Test";
                long dbId = row.Field<long?>("DbId") ?? 0;

                var confirmResult = MessageBox.Show(
                    $"Are you sure you want to delete {testName}?",
                    "Confirm Delete Run",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirmResult == MessageBoxResult.Yes)
                {
                    int chId = cboChannel.SelectedIndex + 1;

                    if (dbId > 0)
                    {
                        Task.Run(async () =>
                        {
                            await _repository.DeleteAutoEllipseTestAsync(dbId);
                        });
                    }

                    if (_channelRawRecords.TryGetValue(chId, out var rawList))
                    {
                        var rec = rawList.FirstOrDefault(r => r.Id == dbId || $"Test {r.TestNumber}" == testName);
                        if (rec != null) rawList.Remove(rec);
                    }

                    _activeTable?.Rows.Remove(row);

                    int remaining = _activeTable?.Rows.Count ?? 0;
                    btnMakeEllipse.IsEnabled = remaining > 0;
                    lblStatus.Text = $"{testName} removed. Remaining test runs: {remaining}";
                }
            }
        }

        private void btnRunTest_Click(object sender, RoutedEventArgs e)
        {
            if (DeviceCOM.IsLogEnable)
            {
                MessageBox.Show("Cannot run Auto Ellipse while production logging is active. Please stop log first.", "Command Conflict", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (DeviceCOM.IsSystemBusy)
            {
                MessageBox.Show("System is currently busy. Please wait for previous operation to complete.", "System Busy", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int chId = cboChannel.SelectedIndex + 1;

            bool isChannelBalanced = !DeviceCOM.IsBalanceRequired &&
                                     (DeviceCOM.responses != null && DeviceCOM.responses.Any(r => r.CN == chId && r.IsBalacenced));

            if (DeviceCOM.IsBalanceRequired || !isChannelBalanced)
            {
                lblStatus.Text = "Please click Balance first.";
                MessageBox.Show("Please click Balance first.", "Balance Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!_channelTables.ContainsKey(chId))
            {
                _ = SetupChannelTableAndColumnsAsync(chId);
            }

            DeviceCOM.IsAutoEllipseActive = true;
            bool success = SendTestCommand();
            if (!success)
            {
                DeviceCOM.IsAutoEllipseActive = false;
                lblStatus.Text = "Failed to communicate with ECT hardware.";
                MessageBox.Show("Unable to start test acquisition due to communication error.", "Communication Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _isTestActive = true;
            _testStartTime = DateTime.Now;
            _lastProcessedResponseIndex = DeviceCOM.responses?.Count ?? 0;

            btnRunTest.IsEnabled = false;
            cboChannel.IsEnabled = false;

            lblStatus.Text = "Acquiring multi-frequency test run from ECT instrument...";
            _acquisitionTimer.Start();
        }

        private bool SendTestCommand()
        {
            try
            {
                if (portCOM == null) return false;

                BalanceTest testCmd = new BalanceTest { FC = 17, CN = 0 };
                bool isJson = Convert.ToBoolean(System.Configuration.ConfigurationManager.AppSettings["IsJSON"]);

                if (isJson)
                {
                    return portCOM.WriteData(JsonConvert.SerializeObject(testCmd));
                }
                else
                {
                    byte[] data = new byte[6];
                    data[0] = Convert.ToByte(2);  // STX
                    data[1] = Convert.ToByte(17); // FC 17 (Test Command)
                    data[2] = Convert.ToByte(1);  // Length
                    data[3] = Convert.ToByte(0);  // CN 0

                    return portCOM.WriteDataInBytes(data);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SendTestCommand exception: {ex.Message}");
                return false;
            }
        }

        private void AcquisitionTimer_Tick(object? sender, EventArgs e)
        {
            if (!_isTestActive || _activeTable == null) return;

            int chId = cboChannel.SelectedIndex + 1;

            if (DeviceCOM.responses != null && DeviceCOM.responses.Count > _lastProcessedResponseIndex)
            {
                for (int i = _lastProcessedResponseIndex; i < DeviceCOM.responses.Count; i++)
                {
                    var resp = DeviceCOM.responses[i];
                    if (resp == null || resp.CN != chId || resp.FD == null) continue;

                    int maxTestNum = 0;
                    if (_activeTable.Rows.Count > 0)
                    {
                        foreach (DataRow r in _activeTable.Rows)
                        {
                            string name = r.Field<string>("TestName") ?? "";
                            if (name.StartsWith("Test ") && int.TryParse(name.Substring(5), out int num))
                            {
                                if (num > maxTestNum) maxTestNum = num;
                            }
                        }
                    }
                    int testNum = maxTestNum + 1;
                    DataRow newRow = _activeTable.NewRow();
                    newRow["IsSelected"] = true; 
                    newRow["TestName"] = $"Test {testNum}";
                    newRow["Timestamp"] = DateTime.Now.ToString("HH:mm:ss.fff");

                    Dictionary<string, object> freqValuesJson = new();

                    foreach (var fd in resp.FD)
                    {
                        string colDisplay = $"F{fd.FN}";
                        string colX = $"F{fd.FN}_X";
                        string colY = $"F{fd.FN}_Y";

                        if (_activeTable.Columns.Contains(colDisplay)) newRow[colDisplay] = $"{fd.X:F2}, {fd.Y:F2}";
                        if (_activeTable.Columns.Contains(colX)) newRow[colX] = (double)fd.X;
                        if (_activeTable.Columns.Contains(colY)) newRow[colY] = (double)fd.Y;

                        freqValuesJson[$"F{fd.FN}"] = new { x = fd.X, y = fd.Y };
                    }

                    AutoEllipseTest testRecord = new AutoEllipseTest
                    {
                        ChannelId = chId,
                        TestNumber = testNum,
                        TimeStamp = DateTime.UtcNow,
                        OperatorName = DeviceCOM.part?.Name ?? "Operator",
                        FrequencyValuesJson = JsonConvert.SerializeObject(freqValuesJson),
                        IsDeleted = false
                    };

                    Task.Run(async () =>
                    {
                        await _repository.InsertAutoEllipseTestAsync(testRecord);
                    });

                    newRow["DbId"] = testRecord.Id;
                    _activeTable.Rows.Add(newRow);

                    if (_channelRawRecords.TryGetValue(chId, out var rawList))
                    {
                        rawList.Add(testRecord);
                    }

                    if (_activeTable.Rows.Count > 0)
                    {
                        dgTestResults.ScrollIntoView(_activeTable.DefaultView[_activeTable.Rows.Count - 1]);
                    }

                    StopAcquisition($"Test {testNum} captured successfully across all frequencies.");
                    break;
                }

                _lastProcessedResponseIndex = DeviceCOM.responses.Count;
            }

            if (_isTestActive && (DateTime.Now - _testStartTime).TotalSeconds >= 10)
            {
                StopAcquisition("Acquisition timed out waiting for hardware response.");
            }
        }

        private void StopAcquisition(string statusMsg)
        {
            DeviceCOM.IsAutoEllipseActive = false;
            _isTestActive = false;
            _acquisitionTimer.Stop();

            btnRunTest.IsEnabled = true;
            cboChannel.IsEnabled = true;

            int rowCount = _activeTable?.Rows.Count ?? 0;
            btnMakeEllipse.IsEnabled = rowCount > 0;
            lblStatus.Text = statusMsg;
        }

        private void btnMakeEllipse_Click(object sender, RoutedEventArgs e)
        {
            if (_activeTable == null || _activeTable.Rows.Count == 0)
            {
                MessageBox.Show("No test runs captured in table for this channel.", "No Data", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int chId = cboChannel.SelectedIndex + 1;

            var selectedRows = _activeTable.AsEnumerable()
                .Where(r => r.Field<bool>("IsSelected"))
                .ToList();

            if (selectedRows.Count == 0)
            {
                MessageBox.Show("Please check at least one test run in the table to compute threshold ellipses.", "No Runs Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (selectedRows.Count < 3)
            {
                MessageBox.Show("Please select a minimum of 3 test runs to compute threshold ellipses.", "Insufficient Test Runs", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var chData = DeviceCOM.channelDatas?.FirstOrDefault(c => c.Id == chId);
                if (chData == null || chData.graphDatas == null)
                {
                    MessageBox.Show("Channel configuration is invalid.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                List<long> selectedDbIds = new();
                _channelRawRecords.TryGetValue(chId, out var rawRecords);

                foreach (var sRow in selectedRows)
                {
                    string tName = sRow.Field<string>("TestName") ?? "";
                    long dbId = sRow.Field<long?>("DbId") ?? 0;
                    if (dbId > 0)
                    {
                        selectedDbIds.Add(dbId);
                    }
                    else if (rawRecords != null)
                    {
                        var match = rawRecords.FirstOrDefault(r => $"Test {r.TestNumber}" == tName);
                        if (match != null && match.Id > 0)
                        {
                            selectedDbIds.Add(match.Id);
                        }
                    }
                }
                string selectedTestIdsJson = JsonConvert.SerializeObject(selectedDbIds);

                double aStretch = 1.0;
                double bStretch = 1.0;

                if (chkAutoStretch.IsChecked != true)
                {
                    if (!double.TryParse(txtStretchA.Text, out aStretch) || aStretch <= 0)
                    {
                        aStretch = 1.0;
                    }
                    if (!double.TryParse(txtStretchB.Text, out bStretch) || bStretch <= 0)
                    {
                        bStretch = 1.0;
                    }
                }

                foreach (var graph in chData.graphDatas)
                {
                    string[] possibleColX = new[] { $"F{graph.Id}_X", $"D{graph.Id}_X", $"{graph.Name}_X", $"{graph.Id}_X" };
                    string[] possibleColY = new[] { $"F{graph.Id}_Y", $"D{graph.Id}_Y", $"{graph.Name}_Y", $"{graph.Id}_Y" };

                    string? colX = possibleColX.FirstOrDefault(c => _activeTable.Columns.Contains(c));
                    string? colY = possibleColY.FirstOrDefault(c => _activeTable.Columns.Contains(c));

                    List<(double X, double Y)> points = new();

                    if (colX != null && colY != null)
                    {   
                        foreach (DataRow row in selectedRows)
                        {
                            if (row[colX] != DBNull.Value && row[colY] != DBNull.Value)
                            {
                                double x = Convert.ToDouble(row[colX]);
                                double y = Convert.ToDouble(row[colY]);
                                points.Add((x, y));
                            }
                        }
                    }

                    var result = EllipseFitter.FitEllipse(graph.Name, graph.Id, points, aStretch, bStretch);

                    if (result.IsValid)
                    {
                        if (graph.ellipses == null) graph.ellipses = new List<Ellips>();
                        if (graph.ellipses.Count == 0) graph.ellipses.Add(new Ellips { Id = 1 });

                        var targetEll = graph.ellipses[0];
                        targetEll.ex = result.CenterX;
                        targetEll.ey = result.CenterY;
                        targetEll.width = result.Width;
                        targetEll.height = result.Height;
                        targetEll.angel = result.RotationAngle;

                        AutoEllipseResultRecord auditRecord = new AutoEllipseResultRecord
                        {
                            ChannelId = chId,
                            Frequency = graph.Name,
                            TimeStamp = DateTime.UtcNow,
                            SelectedTestIdsJson = selectedTestIdsJson,
                            ComputedCenterX = result.CenterX,
                            ComputedCenterY = result.CenterY,
                            ComputedWidth = result.Width,
                            ComputedHeight = result.Height,
                            ComputedRotationAngle = result.RotationAngle,
                            SampleCount = result.SampleCount
                        };

                        Task.Run(async () =>
                        {
                            await _repository.InsertAutoEllipseResultAsync(auditRecord);
                        });
                    }
                }

                if (Owner is MainWindow mw)
                {
                    mw.ImplementChanges(0);
                }

                IsSaved = true;
                string statusText = $"Auto Ellipse threshold applied to Channel-{chId} using {selectedRows.Count} of {_activeTable.Rows.Count} selected test runs.";
                lblStatus.Text = statusText;

                MessageBox.Show(statusText, "Threshold Applied Successfully", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Computation error: {ex.Message}";
                MessageBox.Show($"Error computing Auto Ellipse: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void chkAutoStretch_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (pnlCustomStretch != null)
            {
                pnlCustomStretch.Visibility = (chkAutoStretch.IsChecked == true) ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            if (_isTestActive)
            {
                StopAcquisition("Window closing.");
            }
            Close();
        }
    }
}
