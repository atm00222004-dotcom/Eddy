using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using Newtonsoft.Json;
using _8F.Services;

namespace _8F
{
    /// <summary>
    /// Interaction logic for ExportProfilePickerWindow.xaml
    /// </summary>
    public partial class ExportProfilePickerWindow : Window
    {
        private readonly IConfigProfileRepository _repository = new InspectionLogRepository();

        public bool IsSelectionMode { get; set; } = false;
        public int SelectedProfileId { get; private set; } = 0;
        public string SelectedProfileName { get; private set; } = string.Empty;

        public ExportProfilePickerWindow()
        {
            InitializeComponent();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (IsSelectionMode)
            {
                btnExport.Content = "Select Profile";
            }

            try
            {
                lblStatus.Text = "Loading saved configuration profiles from database...";
                var profiles = await _repository.ListConfigProfilesAsync();

                if (profiles == null || profiles.Count == 0)
                {
                    lblStatus.Text = "No saved configuration profiles found in the database. Please save a configuration first.";
                    btnExport.IsEnabled = false;
                    return;
                }

                dgProfiles.ItemsSource = profiles;
                dgProfiles.SelectedIndex = 0;
                lblStatus.Text = $"Found {profiles.Count} saved profile(s). Select a profile and click {(IsSelectionMode ? "Select Profile" : "Export")}.";
                btnExport.IsEnabled = true;
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Error connecting to database: {ex.Message}";
                btnExport.IsEnabled = false;
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                try
                {
                    DragMove();
                }
                catch (InvalidOperationException)
                {
                    // Ignore DragMove exceptions if mouse state changes mid-click
                }
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            SelectedProfileId = 0;
            Close();
        }

        private void dgProfiles_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgProfiles.SelectedItem is ConfigProfileSummary)
            {
                PerformExport();
            }
        }

        private void btnExport_Click(object sender, RoutedEventArgs e)
        {
            PerformExport();
        }

        private async void PerformExport()
        {
            if (dgProfiles.SelectedItem is not ConfigProfileSummary selectedProfile)
            {
                MessageBox.Show("Please select a profile from the list.", "Selection Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (IsSelectionMode)
            {
                SelectedProfileId = selectedProfile.Id;
                SelectedProfileName = selectedProfile.Name;
                Close();
                return;
            }

            try
            {
                string safeProfileName = string.Concat(selectedProfile.Name.Split(Path.GetInvalidFileNameChars())).Replace(" ", "_");
                string timestampStr = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string defaultFileName = $"{safeProfileName}_{timestampStr}.csv";

                Microsoft.Win32.SaveFileDialog saveDlg = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "Export Configuration File",
                    FileName = defaultFileName,
                    DefaultExt = ".csv",
                    Filter = "CSV Documents (*.csv)|*.csv|JSON Documents (*.json)|*.json|All Files (*.*)|*.*"
                };

                bool? dialogResult = saveDlg.ShowDialog();
                if (dialogResult != true)
                {
                    return;
                }

                string targetFilePath = saveDlg.FileName;
                lblStatus.Text = $"Fetching profile '{selectedProfile.Name}' from database...";

                var channels = await _repository.GetConfigProfileAsync(selectedProfile.Id);
                if (channels == null || channels.Count == 0)
                {
                    MessageBox.Show("The selected configuration profile contains no channel data.", "Empty Profile", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string fileContent;
                string fileExt = Path.GetExtension(targetFilePath).ToLowerInvariant();

                if (fileExt == ".json")
                {
                    fileContent = ExportToJson(channels);
                }
                else
                {
                    fileContent = ExportToCsv(channels);
                }

                File.WriteAllText(targetFilePath, fileContent, Encoding.UTF8);

                string successMsg = $"Configuration profile '{selectedProfile.Name}' successfully exported to:\n{targetFilePath}";
                lblStatus.Text = "Export completed successfully.";
                MessageBox.Show(successMsg, "Export Successful", MessageBoxButton.OK, MessageBoxImage.Information);

                Close();
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Export error: {ex.Message}";
                MessageBox.Show($"Failed to export configuration profile: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static string ExportToCsv(List<ChannelData> channels)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Channel,Tx Strength,Frequency Number,Frequency Name,Frequency (Hz),Gain (dB),Phase (deg),Enabled,Strength,Post Gain,Center X (ex),Center Y (ey),Width,Height,Rotation Angle (deg),Overlay Center X (ex_O),Overlay Center Y (ey_O),Overlay Width,Overlay Height,Overlay Rotation Angle");

            foreach (var ch in channels)
            {
                if (ch.graphDatas == null) continue;
                foreach (var graph in ch.graphDatas)
                {
                    if (graph.ellipses != null && graph.ellipses.Count > 0)
                    {
                        foreach (var ell in graph.ellipses)
                        {
                            sb.AppendLine($"{ch.Id},{ch.TxStrength},{graph.Id},\"{graph.Name}\",{graph.freq},{graph.gain},{graph.phase},{graph.isEnable},{graph.strength},{graph.postGain},{ell.ex},{ell.ey},{ell.width},{ell.height},{ell.angel},{graph.ex_O},{graph.ey_O},{graph.width_O},{graph.height_O},{graph.angel_O}");
                        }
                    }
                    else
                    {
                        sb.AppendLine($"{ch.Id},{ch.TxStrength},{graph.Id},\"{graph.Name}\",{graph.freq},{graph.gain},{graph.phase},{graph.isEnable},{graph.strength},{graph.postGain},{graph.ex},{graph.ey},{graph.width},{graph.height},{graph.angel},{graph.ex_O},{graph.ey_O},{graph.width_O},{graph.height_O},{graph.angel_O}");
                    }
                }
            }

            return sb.ToString();
        }

        private static string ExportToJson(List<ChannelData> channels)
        {
            var formattedList = channels.Select(ch => new
            {
                ChannelNumber = ch.Id,
                IsSelected = ch.IsSeleted,
                TxStrength = ch.TxStrength,
                Frequencies = ch.graphDatas?.Select(g => new
                {
                    FrequencyNumber = g.Id,
                    FrequencyName = g.Name,
                    FrequencyHz = g.freq,
                    Gain = g.gain,
                    Phase = g.phase,
                    IsEnabled = g.isEnable,
                    Strength = g.strength,
                    PostGain = g.postGain,
                    DefaultCenterX = g.ex,
                    DefaultCenterY = g.ey,
                    DefaultWidth = g.width,
                    DefaultHeight = g.height,
                    DefaultRotationAngle = g.angel,
                    OverlayCenterX = g.ex_O,
                    OverlayCenterY = g.ey_O,
                    OverlayWidth = g.width_O,
                    OverlayHeight = g.height_O,
                    OverlayRotationAngle = g.angel_O,
                    Ellipses = g.ellipses?.Select(e => new
                    {
                        EllipseIndex = e.Id,
                        CenterX = e.ex,
                        CenterY = e.ey,
                        Width = e.width,
                        Height = e.height,
                        RotationAngle = e.angel
                    })
                })
            });

            return JsonConvert.SerializeObject(formattedList, Newtonsoft.Json.Formatting.Indented);
        }
    }
}
