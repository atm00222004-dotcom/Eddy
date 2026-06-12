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
    public partial class Attenuation : Window
    {
        public bool IsSaved = false;
        private DispatcherTimer clearLabelTimer;
        public DeviceCOM deviceCOM;
        bool IsEddyAdvance = false; 
        public Attenuation()
        {
            InitializeComponent();
            IsEddyAdvance = Convert.ToBoolean(System.Configuration.ConfigurationSettings.AppSettings["IsEddyAdvance"]);
            


            if (DeviceCOM.Configuration.Frequency.FD.Count > 0)
            {
                foreach (var item in DeviceCOM.Configuration.Frequency.FD)
                {
                    if (DeviceCOM.IsAttRequired == true && item.AT== -1)
                    {
                        item.AT = 0;
                    }

                    if (item.FN == 1)
                    {
                        drpAttenuation.Text = (item.AT).ToString();                        
                    }                
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
                if (DeviceCOM.IsTubeSatart || DeviceCOM.IsCalibarationStart)
                {
                    lblMsg.Content = "The tube/calibration is in progress, no changes are allowed!";
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
                                    item.AT = Convert.ToInt32(drpAttenuation.Text);                                   
                                }                               
                                else if (item.FN == 3)
                                {
                                    item.AT = Convert.ToInt32(drpAttenuation.Text);

                                }
                            }

                        }

                        //DeviceCOM.Configuration.SaveGraphImage = chkSaveGraph.IsChecked == true;

                        var rat1 = false;
                       
                        var rat = false;

                        if (IsEddyAdvance)
                        {
                            rat1 = true;
                            ConfigurationToWrite configurationToWrite = new ConfigurationToWrite();
                            configurationToWrite.FQ = DeviceCOM.Configuration.Frequency.FD;
                            configurationToWrite.FT = DeviceCOM.Configuration.Filter.FD;
                            //configurationToWrite.SaveGraphImage = chkSaveGraph.IsChecked == true; 
                            var data = JsonConvert.SerializeObject(configurationToWrite); 
                            rat = deviceCOM.WriteData(data);
                        }
                        else
                        {

                            rat = deviceCOM.WriteData(JsonConvert.SerializeObject(DeviceCOM.Configuration.Frequency));
                            Filter1 filter1 = new Filter1();
                            filter1.FD = new List<FilterFD1>();

                            foreach (var item in DeviceCOM.Configuration.Filter.FD)
                            {
                                filter1.FD.Add(new FilterFD1 { FN = item.FN, H = item.H, L = item.L });
                            }

                            rat1 = deviceCOM.WriteData(JsonConvert.SerializeObject(filter1));
                        }


                        if (rat && rat1)
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
            
            return validationMsg;
        }

        private void PreviewTextInput_NumericOnly(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }


    }
}
