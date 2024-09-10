using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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
    public partial class Freq : Window
    {
        public bool IsSaved = false; 
        public DeviceCOM portCOM;
        public Freq()
        {
            InitializeComponent();

            ddlFrChennel.ItemsSource = DeviceCOM.channelDatas.FirstOrDefault(c => c.IsSeleted == true).graphDatas.Select(x=> x.Name).ToList();
            ddlFrChennel.SelectedIndex = 0;
            var Gdata = DeviceCOM.channelDatas.FirstOrDefault(c => c.IsSeleted == true).graphDatas.FirstOrDefault(d => d.Name == "D1");
            if (Gdata != null)
            {
                txtFreq.Text = Gdata.freq.ToString();
                txtGain.Text = Gdata.gain.ToString();
                txtPhase.Text = Gdata.phase.ToString();
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
                var msg = Validaton();

                if (msg.Count == 0 && DeviceCOM.IsSystemBusy)
                {
                    msg.Add("System is busy so you can not perform this command, please wait...");
                }

                if (msg.Count == 0)
                {
                    var ch = DeviceCOM.channelDatas.FirstOrDefault(c => c.IsSeleted == true);
                    var Gdata = ch.graphDatas.FirstOrDefault(d => d.Name == ddlFrChennel.Text);
                    if (Gdata != null)
                    {
                        Gdata.freq = Convert.ToInt32(txtFreq.Text);
                        Gdata.gain = Convert.ToInt32(txtGain.Text);
                        Gdata.phase = Convert.ToInt32(txtPhase.Text);

                        FrequencyWrite frequencyWrite = new FrequencyWrite();
                        frequencyWrite.FC = 4;
                        frequencyWrite.CN = ch.Id;
                        frequencyWrite.FD = new List<Frequency>();

                        Frequency frequency = new Frequency() { FN = Gdata.Id, F = Gdata.freq, G = Gdata.gain, P = Gdata.phase };
                        frequencyWrite.FD.Add(frequency);
                        portCOM.WriteData(JsonConvert.SerializeObject(frequencyWrite));

                    }
                    IsSaved = true;
                    lblMsg.Content = "Configuration Saved!!!";
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
            catch (Exception ex)
            {
                lblMsg.Content = "Error while saving the Configuration!!!";
            }
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

            return validationMsg;
        }

        private void ddlFrChennel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {             
            var text = e.AddedItems[0].ToString();
            var Gdata = DeviceCOM.channelDatas.FirstOrDefault(c => c.IsSeleted == true).graphDatas.FirstOrDefault(d => d.Name == text);
            if (Gdata != null)
            {
                txtFreq.Text = Gdata.freq.ToString();
                txtGain.Text = Gdata.gain.ToString();
                txtPhase.Text = Gdata.phase.ToString();
            }

        }

        private void PreviewTextInput_NumericOnly(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

       
    }
}
