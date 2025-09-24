
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

                txtFMS.Text = DeviceCOM.Configuration.Marker.FmS.ToString();
                txtRMS.Text = DeviceCOM.Configuration.Marker.RmS.ToString();
                txtPMS.Text = DeviceCOM.Configuration.Marker.P1mS.ToString();

                txtC1C2.Text = DeviceCOM.Configuration.Marker.C1C2.ToString();
                txtC2E.Text = DeviceCOM.Configuration.Marker.C2E.ToString();
                txtCC2.Text = DeviceCOM.Configuration.Marker.CC2.ToString();
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

                    DeviceCOM.Configuration.Marker.FmS = Convert.ToInt32(txtFMS.Text);
                    DeviceCOM.Configuration.Marker.RmS = Convert.ToInt32(txtRMS.Text);
                    DeviceCOM.Configuration.Marker.P1mS = Convert.ToInt32(txtPMS.Text);

                    DeviceCOM.Configuration.Marker.C1C2 = Convert.ToInt32(txtC1C2.Text);
                    DeviceCOM.Configuration.Marker.C2E = Convert.ToInt32(txtC2E.Text);
                    DeviceCOM.Configuration.Marker.CC2 = Convert.ToInt32(txtCC2.Text);

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

                    System.IO.File.WriteAllText("Config.txt", JsonConvert.SerializeObject(DeviceCOM.Configuration));
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
