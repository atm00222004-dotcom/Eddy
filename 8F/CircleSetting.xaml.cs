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
    public partial class CircleSetting : Window
    {
        public bool IsSaved = false;
        public PortCOM portCOM;
        public CircleSetting(string selectChannel)
        {
            InitializeComponent();

            ddlFrChennel.ItemsSource = PortCOM.channelDatas.FirstOrDefault(c=> c.IsSeleted == true).graphDatas.Select(x=> x.Name).ToList();
            ddlFrChennel.SelectedIndex = 0;
            var Gdata = PortCOM.channelDatas.FirstOrDefault(c => c.IsSeleted == true).graphDatas.FirstOrDefault(d => d.Name == selectChannel);
            if (Gdata != null)
            {
                ddlFrChennel.SelectedItem = selectChannel;
                txtHeight.Text = Gdata.height.ToString();
                txtWidth.Text = Gdata.width.ToString();
                txtX_Shift.Text = Gdata.ex.ToString();
                txtY_Shift.Text = Gdata.ey.ToString();
                txtAngel.Text = Gdata.angel.ToString();
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
                if (msg.Count == 0)
                {
                    var ch = PortCOM.channelDatas.FirstOrDefault(c => c.IsSeleted == true);
                    var Gdata = ch.graphDatas.FirstOrDefault(d => d.Name == ddlFrChennel.Text);
                    if (Gdata != null)
                    {
                        Gdata.height = Convert.ToDouble(txtHeight.Text);
                        Gdata.width = Convert.ToDouble(txtWidth.Text);
                        Gdata.ex = Convert.ToDouble(txtX_Shift.Text);
                        Gdata.ey = Convert.ToDouble(txtY_Shift.Text);
                        Gdata.angel = Convert.ToDouble(txtAngel.Text);

                        ElliplseWrite ellipseWrite = new ElliplseWrite();
                        ellipseWrite.FC = 5;
                        ellipseWrite.CN = ch.Id;
                        ellipseWrite.ED = new List<Elliplse>();

                        Elliplse elliplse = new Elliplse() { FN = Gdata.Id, EId = Gdata.Id, a = Gdata.height, b = Gdata.width, t = Gdata.angel, x = Gdata.ex, y = Gdata.ey };
                        ellipseWrite.ED.Add(elliplse);
                        portCOM.WriteData(JsonConvert.SerializeObject(ellipseWrite));
                    }
                    IsSaved = true;
                    lblMsg.Content = "Configuration Saved!!!";
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
        }

        public List<String> Validaton()
        {
            List<String> validationMsg = new List<string>();
            if (string.IsNullOrEmpty(txtAngel.Text))
            {
                validationMsg.Add("Angle is required and the range is 0 to 359.");
            }
            else
            {
                if (Convert.ToInt32(txtAngel.Text) < 0 || Convert.ToInt32(txtAngel.Text) > 359)
                {
                    validationMsg.Add("Angle is required and the range is 0 to 359.");
                }
            }
            if (string.IsNullOrEmpty(txtHeight.Text))
            {
                validationMsg.Add("Height is required and the range is 100 to 5000.");
            }
            else
            {
                if (Convert.ToInt32(txtHeight.Text) < 100 || Convert.ToInt32(txtHeight.Text) > 5000)
                {
                    validationMsg.Add("Height is required and the range is 100 to 5000.");
                }
            }

            if (string.IsNullOrEmpty(txtWidth.Text))
            {
                validationMsg.Add("Width is required and the range is 100 to 5000.");
            }
            else
            {
                if (Convert.ToInt32(txtWidth.Text) < 100 || Convert.ToInt32(txtWidth.Text) > 5000)
                {
                    validationMsg.Add("Width is required and the range is 100 to 5000.");
                }
            }

            if (string.IsNullOrEmpty(txtX_Shift.Text))
            {
                validationMsg.Add("X Offset is required and the range is -2000 to 2000.");
            }
            else
            {
                if (Convert.ToInt32(txtX_Shift.Text) < -2000 || Convert.ToInt32(txtX_Shift.Text) > 2000)
                {
                    validationMsg.Add("X Offset is required and the range is -2000 to 2000.");
                }
            }

            if (string.IsNullOrEmpty(txtY_Shift.Text))
            {
                validationMsg.Add("Y Offset is required and the range is -2000 to 2000.");
            }
            else
            {
                if (Convert.ToInt32(txtY_Shift.Text) < -2000 || Convert.ToInt32(txtY_Shift.Text) > 2000)
                {
                    validationMsg.Add("Y Offset is required and the range is -2000 to 2000.");
                }
            }

            return validationMsg;
        }

        private void ddlFrChennel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var Gdata = PortCOM.channelDatas.FirstOrDefault(c => c.IsSeleted == true).graphDatas.FirstOrDefault(d => d.Name == ddlFrChennel.Text);
            if (Gdata != null)
            {
                txtHeight.Text = Gdata.height.ToString();
                txtWidth.Text = Gdata.width.ToString();
                txtX_Shift.Text = Gdata.ex.ToString();
                txtY_Shift.Text = Gdata.ey.ToString();
                txtAngel.Text = Gdata.angel.ToString();
            }
            
        }

        private void PreviewTextInput_NumericOnly(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void PreviewTextInput_NumericWithNegativeOnly(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("^[0-9]+([0-9]-)+$");
            e.Handled = regex.IsMatch(e.Text);
        }
    }
}
