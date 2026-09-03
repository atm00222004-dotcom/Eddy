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
using System.Windows.Threading;

namespace _8F.Views
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Freq : Window
    {
        public bool IsSaved = false; 
        public DeviceCOM portCOM = default!;
        private DispatcherTimer clearLabelTimer = default!;
        public Freq()
        {
            InitializeComponent();



            ddlFrChennel.ItemsSource = DeviceCOM.channelDatas.FirstOrDefault(c => c.IsSeleted == true)?.graphDatas.Select(x=> x.Name).ToList();
            List<String> statuses = new List<string>();
            statuses.Add("True");
            statuses.Add("False");
            ddlStatus.ItemsSource = statuses;
            ddlFrChennel.SelectedIndex = 0;
            var Gdata = DeviceCOM.channelDatas.FirstOrDefault(c => c.IsSeleted == true)?.graphDatas.FirstOrDefault(d => d.Name == "D1");
            if (Gdata != null)
            {
                txtSOL.Text = Gdata.sol.ToString();
                txtFreq.Text = Gdata.freq.ToString();
                txtGain.Text = Gdata.gain.ToString();
                txtPhase.Text = Gdata.phase.ToString();
                txtTxStrength.Text = Gdata.txStrength.ToString();
                txtPostGain.Text = Gdata.postGain.ToString();
                if (Gdata.isEnable)
                {
                    ddlStatus.SelectedIndex = 0;
                }
                else
                {
                    ddlStatus.SelectedIndex = 1;
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private async void btnConfigSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                lblMsg.Content = "";
                var msg = Validaton();

                if (msg.Count == 0 && DeviceCOM.IsSystemBusy)
                {
                    msg.Add("System is busy so you can not perform this command, please wait...");
                }

                if (msg.Count == 0)
                {
                    var ch = DeviceCOM.channelDatas.FirstOrDefault(c => c.IsSeleted == true);
                    var Gdata = ch?.graphDatas.FirstOrDefault(d => d.Name == ddlFrChennel.Text);
                    if (ch != null && Gdata != null)
                    {
                        Gdata.sol = Convert.ToInt32(txtSOL.Text);
                        Gdata.freq = Convert.ToInt32(txtFreq.Text);
                        Gdata.gain = Convert.ToInt32(txtGain.Text);
                        Gdata.phase = Convert.ToInt32(txtPhase.Text);
                        Gdata.txStrength = Convert.ToInt32(txtTxStrength.Text);
                        Gdata.postGain = Convert.ToInt32(txtPostGain.Text);
                        
                        if (ddlStatus.SelectedIndex == 0)
                        {
                            Gdata.isEnable = true; 
                        }
                        else
                        {
                            Gdata.isEnable = false;
                        }

                        FrequencyWrite frequencyWrite = new FrequencyWrite();
                        frequencyWrite.FC = 4;
                        frequencyWrite.CN = ch.Id;
                        frequencyWrite.S = Gdata.sol;
                        frequencyWrite.FD = new List<Frequency>();

                        Frequency frequency = new Frequency() { FN = Gdata.Id, F = Gdata.freq, G = Gdata.gain, P = Gdata.phase, ST = Gdata.txStrength, PG = Gdata.postGain, E = Gdata.isEnable ? 1 : 0 };

                        frequencyWrite.FD.Add(frequency);

                        var rat = false;
                        var IsJSON = Convert.ToBoolean(System.Configuration.ConfigurationManager.AppSettings["IsJSON"]);
                        if (IsJSON)
                        {
                            rat = await portCOM.WriteDataAsync(JsonConvert.SerializeObject(frequencyWrite));
                        }
                        else
                        {
                            int length = (frequencyWrite.FD.Count * 10) + 8;
                            byte[] data = new byte[length];

                            data[0] = Convert.ToByte(2); // STX
                            data[1] = Convert.ToByte(4); // Function Code
                            data[2] = Convert.ToByte((frequencyWrite.FD.Count * 10) + 3); // Payload length (N*10 + 1 CN + 1 ST + 1 PG)
                            data[3] = Convert.ToByte(ch.Id); // Channel ID

                            int startB = 4;
                            foreach (var kvp in frequencyWrite.FD)
                            {
                                data[startB]     = Convert.ToByte(kvp.FN);                  // Byte 0: FN (1B)

                                data[startB + 1] = (byte)(kvp.F & 0xFF);                    // Byte 1: Freq Low
                                data[startB + 2] = (byte)((kvp.F >> 8) & 0xFF);             // Byte 2: Freq Byte 2
                                data[startB + 3] = (byte)((kvp.F >> 16) & 0xFF);            // Byte 3: Freq Byte 3
                                data[startB + 4] = (byte)((kvp.F >> 24) & 0xFF);            // Byte 4: Freq Byte 4

                                data[startB + 5] = (byte)(kvp.G & 0xFF);                    // Byte 5: Gain Low
                                data[startB + 6] = (byte)((kvp.G >> 8) & 0xFF);             // Byte 6: Gain High

                                data[startB + 7] = (byte)(kvp.P & 0xFF);                    // Byte 7: Phase Low
                                data[startB + 8] = (byte)((kvp.P >> 8) & 0xFF);             // Byte 8: Phase High

                                data[startB + 9] = (byte)(kvp.E);                           // Byte 9: Enable (1B)

                                startB = startB + 10;
                            }
                            var firstFreq = frequencyWrite.FD.FirstOrDefault();
                            if (firstFreq != null)
                            {
                                data[startB]     = (byte)(firstFreq.ST & 0xFF);              // Byte 10: TxStrength (1B)
                                data[startB + 1] = (byte)(firstFreq.PG & 0xFF);              // Byte 11: PostGain (1B)
                            }

                            rat = await portCOM.WriteDataInBytesAsync(data);
                        }

                        if (rat)
                        {
                            lblMsg.Content = "Configuration Saved!!!";
                        }
                        else
                        {
                            lblMsg.Content = "Configuration Saved but no response from the board, please reboot it and write the configuration again!!!";
                        }
                    }
                    IsSaved = true;
                }
                else
                {
                    lblMsg.Content = "Validatoin Error:-";
                    foreach (var m in msg)
                    {
                        lblMsg.Content = lblMsg.Content + "\r\n" + (msg.IndexOf(m) + 1).ToString() + ". " + m ;
                    }
                    //lblMsg.Content = "Error while saving the Configuration!!!";
                }
            }
            catch (Exception)
            {
                lblMsg.Content = "Error while saving the Configuration!!!";
            }


            clearLabelTimer = new DispatcherTimer();
            clearLabelTimer.Interval = TimeSpan.FromSeconds(20);
            clearLabelTimer.Tick += ClearLabelTimer_Tick;
            clearLabelTimer.Start();


        }

        private void ClearLabelTimer_Tick(object? sender, EventArgs e)
        {
            lblMsg.Content = string.Empty;
            clearLabelTimer.Stop(); // Stop the timer after clearing
        }

        public List<String> Validaton()
        {
            List<String> validationMsg = new List<string>();
            if (string.IsNullOrEmpty(txtFreq.Text))
            {
                validationMsg.Add("Frequency is required and the range is 100 to 50000.");
            }
            else
            {
                if (Convert.ToInt32(txtFreq.Text) < 100 || Convert.ToInt32(txtFreq.Text) > 50000)
                {
                    validationMsg.Add("Frequency is required and the range is 100 to 50000.");
                }
            }
            if (string.IsNullOrEmpty(txtGain.Text))
            {
                validationMsg.Add("Gain is required and the range is 10 to 56.");
            }
            else
            {
                if (Convert.ToInt32(txtGain.Text) < 10 || Convert.ToInt32(txtGain.Text) > 56)
                {
                    validationMsg.Add("Gain is required and the range is 10 to 56.");
                }
            }
            if (string.IsNullOrEmpty(txtPhase.Text))
            {
                validationMsg.Add("Phase is required and the range is 0 to 359.");
            }
            else
            {
                if (Convert.ToInt32(txtPhase.Text) < 0 || Convert.ToInt32(txtPhase.Text) > 359)
                {
                    validationMsg.Add("Phase is required and the range is 0 to 359.");
                }
            }
            if (string.IsNullOrEmpty(txtTxStrength.Text))
            {
                validationMsg.Add("Tx Strength is required and the range is 1 to 100.");
            }
            else
            {
                if (Convert.ToInt32(txtTxStrength.Text) < 1 || Convert.ToInt32(txtTxStrength.Text) > 100)
                {
                    validationMsg.Add("Tx Strength is required and the range is 1 to 100.");
                }
            }
            if (string.IsNullOrEmpty(txtPostGain.Text))
            {
                validationMsg.Add("Post Gain is required and the range is 1 to 60.");
            }
            else
            {
                if (Convert.ToInt32(txtPostGain.Text) < 1 || Convert.ToInt32(txtPostGain.Text) > 100)
                {
                    validationMsg.Add("Post Gain is required and the range is 1 to 60.");
                }
            }

            return validationMsg;
        }

        private void ddlFrChennel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            lblMsg.Content = string.Empty;
            if (e.AddedItems.Count > 0 && e.AddedItems[0] != null)
            {
                var text = e.AddedItems[0]!.ToString();
                var Gdata = DeviceCOM.channelDatas.FirstOrDefault(c => c.IsSeleted == true)?.graphDatas.FirstOrDefault(d => d.Name == text);
                if (Gdata != null)
                {
                    txtFreq.Text = Gdata.freq.ToString();
                    txtGain.Text = Gdata.gain.ToString();
                    txtPhase.Text = Gdata.phase.ToString();
                    txtTxStrength.Text = Gdata.txStrength.ToString();
                    txtPostGain.Text = Gdata.postGain.ToString();
                    if (Gdata.isEnable)
                    {
                        ddlStatus.SelectedIndex = 0;
                    }
                    else
                    {
                        ddlStatus.SelectedIndex = 1;
                    }
                }
            }
        }

        private void PreviewTextInput_NumericOnly(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

       
    }
}
