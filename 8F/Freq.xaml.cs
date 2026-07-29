using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
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
using System.Windows.Shell;
using System.Windows.Threading;

namespace _8F
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Freq : Window
    {
        public bool IsSaved = false; 
        public DeviceCOM portCOM;
        private DispatcherTimer clearLabelTimer;
        private List<GraphData> tempList;
        private bool isTxStrengthEnabled;

        public Freq()
        {
            InitializeComponent();

            isTxStrengthEnabled = Convert.ToBoolean(System.Configuration.ConfigurationSettings.AppSettings["IsTxStrengthEnable"]);
            colTxStrength.Visibility = isTxStrengthEnabled ? Visibility.Visible : Visibility.Collapsed;

            var selectedChannel = DeviceCOM.channelDatas.FirstOrDefault(c => c.IsSeleted);
            if (selectedChannel != null && selectedChannel.graphDatas != null)
            {
                tempList = selectedChannel.graphDatas
                .Select(x => new GraphData
                {
                    Id = x.Id,
                    Name = x.Name,
                    freq = x.freq,
                    gain = x.gain,
                    phase = x.phase,
                    isEnable = x.isEnable,
                    txStrength = x.txStrength == 0 ? 100 : x.txStrength // default 100
                }).ToList();

                gdFreq.ItemsSource = tempList;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnConfigSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                lblMsg.Content = "";

                var ch = DeviceCOM.channelDatas.FirstOrDefault(c => c.IsSeleted == true);

                // commit latest edits
                gdFreq.CommitEdit(DataGridEditingUnit.Cell, true);
                gdFreq.CommitEdit(DataGridEditingUnit.Row, true);

                var list = tempList;

                var msg = Validaton(list);

                if (DeviceCOM.IsSystemBusy)
                {
                    msg.Add("System is busy so you can not perform this command, please wait...");
                    return;
                }

                if (msg.Count == 0)
                {
                    FrequencyWrite frequencyWrite = new FrequencyWrite();
                    frequencyWrite.FC = 4;
                    frequencyWrite.CN = ch.Id;
                    frequencyWrite.FD = new List<Frequency>();

                    foreach (var Gdata in list)
                    {
                        Frequency frequency = new Frequency()
                        {
                            FN = Gdata.Id,
                            F = Gdata.freq,
                            G = Gdata.gain,
                            P = Gdata.phase,
                            E = Gdata.isEnable ? 1 : 0,
                            T = Gdata.txStrength,
                        };

                        frequencyWrite.FD.Add(frequency);
                    }

                    var rat = false;

                    var IsJSON = Convert.ToBoolean(System.Configuration.ConfigurationSettings.AppSettings["IsJSON"]);
                    if (IsJSON)
                    {
                        rat = portCOM.WriteData(JsonConvert.SerializeObject(frequencyWrite));
                    }
                    else
                    {
                        //int length = (frequencyWrite.FD.Count * 10) + 6;
                        int length = (frequencyWrite.FD.Count * (isTxStrengthEnabled ? 11 : 10)) + 6;
                        byte[] data = new byte[length];
                        data[0] = Convert.ToByte(2);
                        data[1] = Convert.ToByte(4);
                        //data[2] = Convert.ToByte((frequencyWrite.FD.Count * 10) + 1);
                        data[2] = Convert.ToByte((frequencyWrite.FD.Count * (isTxStrengthEnabled ? 11 : 10)) + 1);
                        data[3] = Convert.ToByte(ch.Id);
                        int startB = 4;
                        foreach (var kvp in frequencyWrite.FD)
                        {
                            data[startB] = Convert.ToByte(kvp.FN);

                            data[startB + 1] = (byte)(kvp.F & 0xFF);         // Lowest byte
                            data[startB + 2] = (byte)((kvp.F >> 8) & 0xFF);  // Byte 2
                            data[startB + 3] = (byte)((kvp.F >> 16) & 0xFF); // Byte 3
                            data[startB + 4] = (byte)((kvp.F >> 24) & 0xFF); // Highest byte

                            data[startB + 5] = (byte)(kvp.G & 0xFF);         // Lowest byte
                            data[startB + 6] = (byte)((kvp.G >> 8) & 0xFF);  // Byte 2

                            data[startB + 7] = (byte)(kvp.P & 0xFF);         // Lowest byte
                            data[startB + 8] = (byte)((kvp.P >> 8) & 0xFF);  // Byte 2

                            data[startB + 9] = (byte)(kvp.E);

                            if(isTxStrengthEnabled)
                            {
                                data[startB + 10] = (byte)(kvp.T);

                            }

                            //startB = startB + 10;
                            startB = startB + (isTxStrengthEnabled ? 11 : 10);
                        }

                        rat = portCOM.WriteDataInBytes(data);
                    }

                    // Copy temp data → original data
                    foreach (var original in ch.graphDatas)
                    {
                        var updated = tempList.FirstOrDefault(x => x.Id == original.Id);
                        if (updated != null)
                        {
                            original.freq = updated.freq;
                            original.gain = updated.gain;
                            original.phase = updated.phase;
                            original.isEnable = updated.isEnable;
                            if (isTxStrengthEnabled)
                            {
                                original.txStrength = updated.txStrength;
                            }
                        }
                    }

                    if (rat)
                    {
                        lblMsg.Content = "Configuration Saved!!!";
                    }
                    else
                    {
                        lblMsg.Content = "Configuration Saved but no response from the ECT Instrument, please reboot it and write the configuration again!!!";
                    }

                    IsSaved = true;
                }
                else
                {
                    lblMsg.Content = "Validatoin Error:-";
                    foreach (var m in msg)
                    {
                        lblMsg.Content = lblMsg.Content + "\r\n" + (msg.IndexOf(m) + 1).ToString() + ". " + m;
                    }
                }
            }
            catch (Exception ex)
            {
                lblMsg.Content = "Error while saving the Configuration!!!";
            }


            clearLabelTimer = new DispatcherTimer();
            clearLabelTimer.Interval = TimeSpan.FromSeconds(20);
            clearLabelTimer.Tick += ClearLabelTimer_Tick;
            clearLabelTimer.Start();


        }

        private void ClearLabelTimer_Tick(object sender, EventArgs e)
        {
            lblMsg.Content = string.Empty;
            clearLabelTimer.Stop(); // Stop the timer after clearing
        }

        public List<string> Validaton(List<GraphData> list)
        {
            List<string> validationMsg = new List<string>();
            var MinFreq = Convert.ToInt32(System.Configuration.ConfigurationSettings.AppSettings["MinFreq"]);
            foreach (var item in list)
            {
                if (item.freq < MinFreq || item.freq > 50000)
                    validationMsg.Add($"{item.Name}: Frequency must be "+ MinFreq.ToString() + "–50000");

                if (item.gain < 1 || item.gain > 56)
                    validationMsg.Add($"{item.Name}: Gain must be 10–56");

                if (item.phase < 0 || item.phase > 359)
                    validationMsg.Add($"{item.Name}: Phase must be 0–359");

                if (isTxStrengthEnabled && (item.txStrength < 1 || item.txStrength > 100))
                    validationMsg.Add($"{item.Name}: Tx Strength must be 1–100");
            }

            return validationMsg;
        }
       
    }
}
