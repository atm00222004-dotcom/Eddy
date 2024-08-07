using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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

namespace _8F
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Freq : Window
    {
        public bool IsSaved = false; 
        public PortCOM portCOM;
        public Freq()
        {
            InitializeComponent();

            ddlFrChennel.ItemsSource = PortCOM.channelDatas.FirstOrDefault(c => c.IsSeleted == true).graphDatas.Select(x=> x.Name).ToList();
            ddlFrChennel.SelectedIndex = 0;
            var Gdata = PortCOM.channelDatas.FirstOrDefault(c => c.IsSeleted == true).graphDatas.FirstOrDefault(d => d.Name == "D1");
            if (Gdata != null)
            {
                txtFreq.Text = Gdata.freq;
                txtGain.Text = Gdata.gain;
                txtPhase.Text = Gdata.phase;

                //txtHeight.Text = Gdata.height.ToString();
                //txtWidth.Text = Gdata.width.ToString();
                //txtX_Shift.Text = Gdata.ex.ToString();
                //txtY_Shift.Text = Gdata.ey.ToString();
                //txtAngel.Text = Gdata.angel.ToString();
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
                var ch = PortCOM.channelDatas.FirstOrDefault(c => c.IsSeleted == true);
                var Gdata = ch.graphDatas.FirstOrDefault(d => d.Name == ddlFrChennel.Text);
                if (Gdata != null)
                {
                    Gdata.freq = txtFreq.Text;
                    Gdata.gain = txtGain.Text;
                    Gdata.phase = txtPhase.Text;

                    //Gdata.height = Convert.ToDouble(txtHeight.Text);
                    //Gdata.width = Convert.ToDouble(txtWidth.Text);
                    //Gdata.ex = Convert.ToDouble(txtX_Shift.Text);
                    //Gdata.ey = Convert.ToDouble(txtY_Shift.Text);
                    //Gdata.angel = Convert.ToDouble(txtAngel.Text);
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
            catch (Exception ex)
            {
                lblMsg.Content = "Error while saving the Configuration!!!";
            }
        }

        private void ddlFrChennel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var Gdata = PortCOM.channelDatas.FirstOrDefault(c => c.IsSeleted == true).graphDatas.FirstOrDefault(d => d.Name == ddlFrChennel.Text);
            if (Gdata != null)
            {
                txtFreq.Text = Gdata.freq;
                txtGain.Text = Gdata.gain;
                txtPhase.Text = Gdata.phase;

                //txtHeight.Text = Gdata.height.ToString();
                //txtWidth.Text = Gdata.width.ToString();
                //txtX_Shift.Text = Gdata.ex.ToString();
                //txtY_Shift.Text = Gdata.ey.ToString();
                //txtAngel.Text = Gdata.angel.ToString();
            }

        }
    }
}
