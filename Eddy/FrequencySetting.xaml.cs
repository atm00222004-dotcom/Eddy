using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Security.Cryptography;
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
    public partial class FrequencySetting : Window
    {
        public bool IsSaved = false;
        private DispatcherTimer clearLabelTimer;
        public DeviceCOM deviceCOM;
        public FrequencySetting()
        {
            InitializeComponent();

            if (DeviceCOM.Configuration.Frequency.FD.Count > 0)
            {
                foreach (var item in DeviceCOM.Configuration.Frequency.FD)
                {
                    if (item.FN == 1)
                    {
                        txtPhase1.Text = (item.F / 1000).ToString();
                        txtGain1.Text = item.G.ToString();
                        txtUTH1.Text = item.UTH.ToString();
                        txtLTH1.Text = item.LTH.ToString();
                        txtPP1.Text = item.PP.ToString();
                        txtTH1.Text = item.TH.ToString();
                    }
                    //else if (item.FN == 2)
                    //{
                    //    chkD2.IsChecked = Convert.ToBoolean(item.E);
                    //    txtPhase2.Text = item.F.ToString();
                    //    txtGain2.Text = item.G.ToString();
                    //    txtUTH2.Text = item.UTH.ToString();
                    //    txtLTH2.Text = item.LTH.ToString();
                    //    txtPP2.Text = item.PP.ToString();
                    //    txtPM2.Text = item.PM.ToString();
                    //}
                    //else if (item.FN == 3)
                    //{
                    //    chkD3.IsChecked = Convert.ToBoolean(item.E);
                    //    txtPhase3.Text = item.F.ToString();
                    //    txtGain3.Text = item.G.ToString();
                    //    txtUTH3.Text = item.UTH.ToString();
                    //    txtLTH3.Text = item.LTH.ToString();
                    //    txtPP3.Text = item.PP.ToString();
                    //    txtPM3.Text = item.PM.ToString();
                    //}

                }

                foreach (var item in DeviceCOM.Configuration.Filter.FD)
                {
                    if (item.FN == 1)
                    {
                        txtH1.Text = item.H.ToString();
                        txtL1.Text = item.L.ToString();
                    }
                    //else if (item.FN == 2)
                    //{
                    //    txtH2.Text = item.H.ToString();
                    //    txtL2.Text = item.L.ToString();
                    //}
                    //else if (item.FN == 3)
                    //{
                    //    txtH3.Text = item.H.ToString();
                    //    txtL3.Text = item.L.ToString();
                    //}
                }
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
                if (DeviceCOM.IsTubeSatart)
                {
                    lblMsg.Content = "The tube is in progress, no changes are allowed!";
                }
                else
                {
                    lblMsg.Content = "";
                    var msg = Validaton();

                    if (msg.Count == 0)
                    {
                        //DeviceCOM.Configuration.Marker.M3 = Convert.ToInt32(txtM3.Text);
                        if (DeviceCOM.Configuration.Frequency.FD.Count > 0)
                        {
                            foreach (var item in DeviceCOM.Configuration.Frequency.FD)
                            {
                                if (item.FN == 1)
                                {
                                    item.E = 1;
                                    item.F = Convert.ToInt32(txtPhase1.Text) * 1000;
                                    item.G = Convert.ToInt32(txtGain1.Text);
                                    item.UTH = Convert.ToInt32(txtUTH1.Text);
                                    item.LTH = Convert.ToInt32(txtLTH1.Text);
                                    item.PP = Convert.ToInt32(txtPP1.Text);
                                    item.TH = Convert.ToInt32(txtTH1.Text);
                                }
                                //else if (item.FN == 2)
                                //{
                                //    item.E = (Convert.ToBoolean(chkD2.IsChecked) ? 1 : 0);
                                //    item.F = Convert.ToInt32(txtPhase2.Text);
                                //    item.G = Convert.ToInt32(txtGain2.Text);
                                //    item.UTH = Convert.ToInt32(txtUTH2.Text);
                                //    item.LTH = Convert.ToInt32(txtLTH2.Text);
                                //    item.PP = Convert.ToInt32(txtPP2.Text);
                                //    item.PM = Convert.ToInt32(txtPM2.Text);
                                //}
                                else if (item.FN == 3)
                                {
                                    item.E = 0;
                                    item.F = Convert.ToInt32(txtPhase1.Text) * 1000;
                                    item.G = Convert.ToInt32(txtGain1.Text);
                                    item.UTH = Convert.ToInt32(txtUTH1.Text);
                                    item.LTH = Convert.ToInt32(txtLTH1.Text);
                                    item.PP = Convert.ToInt32(txtPP1.Text);
                                    item.TH = Convert.ToInt32(txtTH1.Text);
                                }
                            }

                            foreach (var item in DeviceCOM.Configuration.Filter.FD)
                            {
                                if (item.FN == 1)
                                {
                                    item.H = Convert.ToInt32(txtH1.Text);
                                    item.L = Convert.ToInt32(txtL1.Text);
                                }
                                //else if (item.FN == 2)
                                //{
                                //    item.H = Convert.ToInt32(txtH2.Text);
                                //    item.L = Convert.ToInt32(txtL2.Text);
                                //}
                                else if (item.FN == 3)
                                {
                                    item.H = Convert.ToInt32(txtH1.Text);
                                    item.L = Convert.ToInt32(txtL1.Text);
                                }
                            }
                        }
                        var rat = deviceCOM.WriteData(JsonConvert.SerializeObject(DeviceCOM.Configuration.Frequency));
                        var rat1 = deviceCOM.WriteData(JsonConvert.SerializeObject(DeviceCOM.Configuration.Filter));

                        if (rat && rat1)
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
            if (string.IsNullOrEmpty(txtPhase1.Text))
            {
                validationMsg.Add("Frequeny(KHz) is required and the range is 1 to 50.");
            }
            else
            {
                if (Convert.ToInt32(txtPhase1.Text) < 1 || Convert.ToInt32(txtPhase1.Text) > 50)
                {
                    validationMsg.Add("Frequeny(KHz) is required and the range is 1 to 50.");
                }
            }

            if (string.IsNullOrEmpty(txtGain1.Text))
            {
                validationMsg.Add("Gain(dB) is required and the range is 1 to 60.");
            }
            else
            {
                if (Convert.ToInt32(txtGain1.Text) < 1 || Convert.ToInt32(txtGain1.Text) > 60)
                {
                    validationMsg.Add("Gain(dB) is required and the range is 1 to 60.");
                }
            }

            if (string.IsNullOrEmpty(txtPP1.Text))
            {
                validationMsg.Add("Phase is required and the range is 0 to 360.");
            }
            else
            {
                if (Convert.ToInt32(txtPP1.Text) < 0 || Convert.ToInt32(txtPP1.Text) > 360)
                {
                    validationMsg.Add("Phase is required and the range is 0 to 360.");
                }
            }

            if (string.IsNullOrEmpty(txtUTH1.Text))
            {
                validationMsg.Add("High Thresold is required and the range is 0 to 100.");
            }
            else
            {
                if (Convert.ToInt32(txtUTH1.Text) < 0 || Convert.ToInt32(txtUTH1.Text) > 100)
                {
                    validationMsg.Add("High Thresold is required and the range is 0 to 100.");
                }
            }

            if (string.IsNullOrEmpty(txtLTH1.Text))
            {
                validationMsg.Add("Low Thresold is required and the range is 0 to 100.");
            }
            else
            {
                if (Convert.ToInt32(txtLTH1.Text) < 0 || Convert.ToInt32(txtLTH1.Text) > 100)
                {
                    validationMsg.Add("Low Thresold is required and the range is 0 to 100.");
                }
            }

            if (string.IsNullOrEmpty(txtTH1.Text))
            {
                validationMsg.Add("Thresold is required and the range is 0 to 100.");
            }
            else
            {
                if (Convert.ToInt32(txtTH1.Text) < 0 || Convert.ToInt32(txtTH1.Text) > 100)
                {
                    validationMsg.Add("Thresold is required and the range is 0 to 100.");
                }
            }

            if (string.IsNullOrEmpty(txtTH1.Text))
            {
                validationMsg.Add("Thresold is required and the range is 0 to 100.");
            }
            else
            {
                if (Convert.ToInt32(txtTH1.Text) < 0 || Convert.ToInt32(txtTH1.Text) > 100)
                {
                    validationMsg.Add("Thresold is required and the range is 0 to 100.");
                }
            }

            if (string.IsNullOrEmpty(txtH1.Text))
            {
                validationMsg.Add("High Pass Filter is required and the range is 1 to 100.");
            }
            else
            {
                if (Convert.ToInt32(txtH1.Text) < 1 || Convert.ToInt32(txtH1.Text) > 100)
                {
                    validationMsg.Add("High Pass Filter is required and the range is 1 to 100.");
                }
            }

            if (string.IsNullOrEmpty(txtL1.Text))
            {
                validationMsg.Add("Low Pass Filter is required and the range is 1 to 100.");
            }
            else
            {
                if (Convert.ToInt32(txtL1.Text) < 1 || Convert.ToInt32(txtL1.Text) > 100)
                {
                    validationMsg.Add("Low Pass Filter is required and the range is 1 to 100.");
                }
            }

            return validationMsg;
        }

        private void PreviewTextInput_NumericOnly(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }


    }
}
