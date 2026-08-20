using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using _8F.Models;
using _8F.Services;

namespace _8F
{
    /// <summary>
    /// Interaction logic for MakeEllipseSelection.xaml
    /// </summary>
    public partial class MakeEllipseSelection : Window
    {
        public bool IsApplied { get; private set; } = false;
        public string SummaryStatus { get; private set; } = string.Empty;

        private readonly int _channelId;
        private readonly DataTable _selectionTable;
        private readonly List<AutoEllipseTest> _rawTestRecords;
        private readonly InspectionLogRepository _repository;

        public MakeEllipseSelection(int channelId, DataTable sourceTable, List<AutoEllipseTest> rawTestRecords)
        {
            InitializeComponent();

            _channelId = channelId;
            _repository = new InspectionLogRepository();
            _rawTestRecords = rawTestRecords ?? new List<AutoEllipseTest>();

            // Clone structure & copy data with IsSelected column
            _selectionTable = sourceTable.Clone();
            if (!_selectionTable.Columns.Contains("IsSelected"))
            {
                _selectionTable.Columns.Add("IsSelected", typeof(bool));
                _selectionTable.Columns["IsSelected"]!.SetOrdinal(0);
            }

            foreach (DataRow row in sourceTable.Rows)
            {
                DataRow newRow = _selectionTable.NewRow();
                foreach (DataColumn col in sourceTable.Columns)
                {
                    if (_selectionTable.Columns.Contains(col.ColumnName))
                    {
                        newRow[col.ColumnName] = row[col.ColumnName];
                    }
                }
                newRow["IsSelected"] = true; // Default selected
                _selectionTable.Rows.Add(newRow);
            }

            Loaded += MakeEllipseSelection_Loaded;
        }

        private void MakeEllipseSelection_Loaded(object sender, RoutedEventArgs e)
        {
            SetupDataGridColumns();
        }

        private void SetupDataGridColumns()
        {
            dgSelection.Columns.Clear();
            dgSelection.ItemsSource = _selectionTable.DefaultView;

            // Include CheckBox Column
            dgSelection.Columns.Add(new DataGridCheckBoxColumn
            {
                Header = "Include",
                Binding = new Binding("IsSelected"),
                Width = 70
            });

            // Test Name Column
            dgSelection.Columns.Add(new DataGridTextColumn
            {
                Header = "Test #",
                Binding = new Binding("TestName"),
                IsReadOnly = true,
                Width = 75
            });

            // Timestamp Column
            dgSelection.Columns.Add(new DataGridTextColumn
            {
                Header = "Timestamp",
                Binding = new Binding("Timestamp"),
                IsReadOnly = true,
                Width = 110
            });

            // Combined Frequency Columns: Dn (X,Y)
            var activeCh = DeviceCOM.channelDatas?.FirstOrDefault(c => c.Id == _channelId);
            if (activeCh != null && activeCh.graphDatas != null)
            {
                foreach (var freq in activeCh.graphDatas)
                {
                    dgSelection.Columns.Add(new DataGridTextColumn
                    {
                        Header = $"{freq.Name} (X,Y)",
                        Binding = new Binding($"F{freq.Id}"),
                        IsReadOnly = true,
                        Width = 150
                    });
                }
            }
        }

        private void btnSelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (DataRow row in _selectionTable.Rows)
            {
                row["IsSelected"] = true;
            }
        }

        private void btnDeselectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (DataRow row in _selectionTable.Rows)
            {
                row["IsSelected"] = false;
            }
        }

        private void btnComputeApply_Click(object sender, RoutedEventArgs e)
        {
            var selectedRows = _selectionTable.AsEnumerable()
                .Where(r => r.Field<bool>("IsSelected"))
                .ToList();

            if (selectedRows.Count < 3)
            {
                MessageBox.Show("Please select a minimum of 3 test runs to compute threshold ellipses.", "Insufficient Test Runs", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var chData = DeviceCOM.channelDatas?.FirstOrDefault(c => c.Id == _channelId);
                if (chData == null || chData.graphDatas == null)
                {
                    MessageBox.Show("Channel configuration is invalid.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                List<AutoEllipseResult> computedResults = new();

                List<long> selectedDbIds = new();
                foreach (var sRow in selectedRows)
                {
                    string tName = sRow.Field<string>("TestName") ?? "";
                    long dbId = sRow.Field<long?>("DbId") ?? 0;
                    if (dbId > 0)
                    {
                        selectedDbIds.Add(dbId);
                    }
                    else
                    {
                        var match = _rawTestRecords.FirstOrDefault(r => $"Test {r.TestNumber}" == tName);
                        if (match != null && match.Id > 0)
                        {
                            selectedDbIds.Add(match.Id);
                        }
                    }
                }
                string selectedTestIdsJson = JsonConvert.SerializeObject(selectedDbIds);

                foreach (var graph in chData.graphDatas)
                {
                    string[] possibleColX = new[] { $"F{graph.Id}_X", $"D{graph.Id}_X", $"{graph.Name}_X", $"{graph.Id}_X" };
                    string[] possibleColY = new[] { $"F{graph.Id}_Y", $"D{graph.Id}_Y", $"{graph.Name}_Y", $"{graph.Id}_Y" };

                    string? colX = possibleColX.FirstOrDefault(c => _selectionTable.Columns.Contains(c));
                    string? colY = possibleColY.FirstOrDefault(c => _selectionTable.Columns.Contains(c));

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

                    var result = EllipseFitter.FitEllipse(graph.Name, graph.Id, points);
                    computedResults.Add(result);

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

                        // Save Computed Audit Record to DB with SelectedTestIds
                        AutoEllipseResultRecord auditRecord = new AutoEllipseResultRecord
                        {
                            ChannelId = _channelId,
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

                // Display Computed Parameters Table
                dgComputedParameters.ItemsSource = computedResults;

                // Apply changes to ECT Hardware & Main UI
                if (Owner is AutoEllipse parentWin && parentWin.Owner is MainWindow mw)
                {
                    mw.ImplementChanges(2);
                }
                else if (Owner is MainWindow mwDirect)
                {
                    mwDirect.ImplementChanges(2);
                }

                IsApplied = true;
                SummaryStatus = $"Auto Ellipse applied to Channel-{_channelId} using {selectedRows.Count} of {_selectionTable.Rows.Count} test runs.";
                lblStatus.Text = SummaryStatus;

                MessageBox.Show($"Auto Ellipse threshold configuration applied successfully to Channel-{_channelId} using {selectedRows.Count} selected run(s)!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Computation error: {ex.Message}";
                MessageBox.Show($"Error computing Auto Ellipse: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
