
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

namespace Eddy
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class MarkerSetting : Window
    {
        public bool IsSaved = false; 
        private DispatcherTimer clearLabelTimer;
        public DeviceCOM deviceCOM;
        public MarkerSetting()
        {
            InitializeComponent();
            IsSaved = false;
            if (DeviceCOM.Configuration.Marker != null)
            {
                txtM1.Text = DeviceCOM.Configuration.Marker.M1.ToString();
                txtM2.Text = DeviceCOM.Configuration.Marker.M2.ToString();

                txtSPT.Text = DeviceCOM.Configuration.Marker.SPT.ToString();
                txtSPT1.Text = DeviceCOM.Configuration.Marker.SPT1.ToString();

                txtSPID.Text = DeviceCOM.Configuration.Marker.SPID.ToString();
                txtSPI.Text = DeviceCOM.Configuration.Marker.SPI.ToString();

                txtSPID1.Text = DeviceCOM.Configuration.Marker.SPID1.ToString();
                txtSPI1.Text = DeviceCOM.Configuration.Marker.SPI1.ToString();

                txtFSFC.Text = DeviceCOM.Configuration.Marker.FSFC.ToString();
                txtFCC.Text = DeviceCOM.Configuration.Marker.FCC.ToString();

                txtCSCC.Text = DeviceCOM.Configuration.Marker.CSC.ToString();

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
                var msg = Validaton();

                if (msg.Count == 0)
                {
                    DeviceCOM.Configuration.Marker.M1 = Convert.ToInt32(txtM1.Text);
                    DeviceCOM.Configuration.Marker.M2 = Convert.ToInt32(txtM2.Text);

                    DeviceCOM.Configuration.Marker.SPT = Convert.ToInt32(txtSPT.Text);
                    DeviceCOM.Configuration.Marker.SPT1 = Convert.ToInt32(txtSPT1.Text);

                    DeviceCOM.Configuration.Marker.SPID = Convert.ToInt32(txtSPID.Text);
                    DeviceCOM.Configuration.Marker.SPI = Convert.ToInt32(txtSPI.Text);

                    DeviceCOM.Configuration.Marker.SPID1 = Convert.ToInt32(txtSPID1.Text);
                    DeviceCOM.Configuration.Marker.SPI1 = Convert.ToInt32(txtSPI1.Text);

                    DeviceCOM.Configuration.Marker.FSFC = Convert.ToInt32(txtFSFC.Text);
                    DeviceCOM.Configuration.Marker.FCC = Convert.ToInt32(txtFCC.Text);

                    DeviceCOM.Configuration.Marker.CSC = Convert.ToInt32(txtCSCC.Text);


                    var rat = deviceCOM.WriteData(JsonConvert.SerializeObject(DeviceCOM.Configuration.Marker));
                    if (rat)
                    {
                        lblMsg.Content = "Configuration Saved!!!";
                    }
                    else
                    {
                        lblMsg.Content = "Configuration Saved but no response from the board, please reboot it and write the configuration again!!!";
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

        public List<String> Validaton()
        {
            List<String> validationMsg = new List<string>();
            //if (string.IsNullOrEmpty(txtFreq.Text))
            //{
            //    validationMsg.Add("Frequency is required and the range is 100 to 50000.");
            //}
            //else
            //{
            //    if (Convert.ToInt32(txtFreq.Text) < 100 || Convert.ToInt32(txtFreq.Text) > 50000)
            //    {
            //        validationMsg.Add("Frequency is required and the range is 100 to 50000.");
            //    }
            //}
            //if (string.IsNullOrEmpty(txtGain.Text))
            //{
            //    validationMsg.Add("Gain is required and the range is 10 to 56.");
            //}
            //else
            //{
            //    if (Convert.ToInt32(txtGain.Text) < 10 || Convert.ToInt32(txtGain.Text) > 56)
            //    {
            //        validationMsg.Add("Gain is required and the range is 10 to 56.");
            //    }
            //}
            //if (string.IsNullOrEmpty(txtPhase.Text))
            //{
            //    validationMsg.Add("Phase is required and the range is 0 to 359.");
            //}
            //else
            //{
            //    if (Convert.ToInt32(txtPhase.Text) < 0 || Convert.ToInt32(txtPhase.Text) > 359)
            //    {
            //        validationMsg.Add("Phase is required and the range is 0 to 359.");
            //    }
            //}

            return validationMsg;
        }

        private void PreviewTextInput_NumericOnly(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

       
    }
}
