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

                        var rat1 = true;

                        byte[] data1 = new byte[50];
                        data1[0] = Convert.ToByte(2);
                        data1[1] = Convert.ToByte(57);
                        data1[2] = Convert.ToByte(45);
                        data1[3] = Convert.ToByte(1);
                        data1[4] = Convert.ToByte(1);
                        data1[5] = Convert.ToByte(2);

                        int startBytes = 6;
                        foreach (var fd in DeviceCOM.Configuration.Frequency.FD)
                        {
                            data1[startBytes] = Convert.ToByte(fd.FN);

                            data1[startBytes + 1] = (byte)(fd.F & 0xFF);         // Lowest byte
                            data1[startBytes + 2] = (byte)((fd.F >> 8) & 0xFF);  // Byte 2
                            data1[startBytes + 3] = (byte)((fd.F >> 16) & 0xFF); // Byte 3
                            data1[startBytes + 4] = (byte)((fd.F >> 24) & 0xFF); // Highest byte

                            var gaint = Convert.ToInt16(fd.G * 10);
                            data1[startBytes + 5] = (byte)(gaint & 0xFF);
                            data1[startBytes + 6] = (byte)((gaint >> 8) & 0xFF);

                            data1[startBytes + 7] = (byte)(fd.LTH & 0xFF);
                            data1[startBytes + 8] = (byte)((fd.LTH >> 8) & 0xFF);

                            data1[startBytes + 9] = (byte)(fd.UTH & 0xFF);
                            data1[startBytes + 10] = (byte)((fd.UTH >> 8) & 0xFF);

                            startBytes = startBytes + 11;
                        }

                        foreach (var fd in DeviceCOM.Configuration.Filter.FD)
                        {
                            data1[startBytes] = Convert.ToByte(fd.FN);

                            ushort h = Convert.ToUInt16(fd.H);
                            ushort l = Convert.ToUInt16(fd.L);
                            ushort x = Convert.ToUInt16(fd.X);
                            ushort y = Convert.ToUInt16(fd.Y);

                            // H
                            data1[startBytes + 1] = (byte)(h & 0xFF);         // Low byte
                            data1[startBytes + 2] = (byte)((h >> 8) & 0xFF);  // High byte

                            // L
                            data1[startBytes + 3] = (byte)(l & 0xFF);
                            data1[startBytes + 4] = (byte)((l >> 8) & 0xFF);

                            // X
                            data1[startBytes + 5] = (byte)(x & 0xFF);
                            data1[startBytes + 6] = (byte)((x >> 8) & 0xFF);

                            // Y
                            data1[startBytes + 7] = (byte)(y & 0xFF);
                            data1[startBytes + 8] = (byte)((y >> 8) & 0xFF);

                            startBytes = startBytes + 9;
                        }

                        data1[startBytes] = Convert.ToByte(DeviceCOM.Configuration.Frequency.FD[0].AT);

                        rat1 = deviceCOM.WriteDataInByte(data1);



                        if (rat1)
                        {
                            lblMsg.Content = "Configuration Saved!!!";
                        }
                        else
                        {
                            lblMsg.Content = "Configuration Saved but no response from the ECT Instrument, please reboot it and write the configuration again!!!";
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
