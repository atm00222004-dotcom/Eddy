using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Threading;

namespace _8F
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class CircleSetting : Window
    {
        public bool IsSaved = false;
        public DeviceCOM portCOM;
        string _selectChannel;
        public ObservableCollection<EllipsDTO> ellipses;
        private DispatcherTimer clearLabelTimer;
        public CircleSetting(string selectChannel)
        {
            InitializeComponent();
            ddlFrChennel.ItemsSource = DeviceCOM.channelDatas.FirstOrDefault(c => c.IsSeleted == true).graphDatas.Select(x => x.Name).ToList();
            ddlFrChennel.SelectedItem = selectChannel;
        }

        private void ChangeFreq (string selectChannel)
        {
            lblHeader.Content = "System Configuration (" + selectChannel + ")";
            _selectChannel = selectChannel;
            var Gdata = DeviceCOM.channelDatas.FirstOrDefault(c => c.IsSeleted == true).graphDatas.FirstOrDefault(d => d.Name == selectChannel);
            if (Gdata != null)
            {
                //Gdata.ellipses
                ellipses = new ObservableCollection<EllipsDTO>();

                Gdata.ellipses.ForEach(ell =>
                {
                    var index = Gdata.ellipses.IndexOf(ell);
                    EllipsDTO ellips = new EllipsDTO();
                    ellips.Id = ell.Id;
                    ellips.height = ell.height;
                    ellips.width = ell.width;
                    ellips.ex = ell.ex;
                    ellips.ey = ell.ey;
                    ellips.angel = ell.angel;
                    ellips.ColorName = MyColor.GetColorName(index).ToString();
                    ellipses.Add(ellips);
                });
                gdFreq.ItemsSource = null;
                gdFreq.ItemsSource = ellipses;

                txtWidth.Text = Gdata.width_O.ToString();
                txtHeight.Text = Gdata.height_O.ToString();
                txtAngel.Text = Gdata.angel_O.ToString();
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (IsSaved)
            {
                btnConfigSave_Click(btnConfigSave, null);
            }
            this.Close();
        }

        private void btnConfigSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                gdFreq.CommitEdit();
                //CollectionViewSource.GetDefaultView(gdFreq.ItemsSource)?.Refresh();
                SaveData();
                //if (!IsSaved)
                //    btnConfigSave_Click(sender, e);

                IsSaved = true;
                ((MainWindow)this.Owner).ImplementChanges(2);

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

        private void SaveData()
        {
            lblMsg.Content = "";
            var ch = DeviceCOM.channelDatas.FirstOrDefault(c => c.IsSeleted == true);
            ElliplseWrite ellipseWrite = new ElliplseWrite();
            ellipseWrite.FC = 5;
            ellipseWrite.CN = ch.Id;
            ellipseWrite.FD = new List<Frequ>();
            var Gdata = ch.graphDatas.FirstOrDefault(d => d.Name == _selectChannel);

            Frequ frequ = new Frequ();
            frequ.FN = Gdata.Id;
            frequ.ED = new List<Elliplse>();

            if (Gdata != null)
            {
                Gdata.ellipses.Clear();
                foreach (var item in ellipses)
                {
                    Ellips el = new Ellips();
                    el.Id = Gdata.ellipses.Count + 1;
                    el.height = item.height;
                    el.width = item.width;
                    el.ex = item.ex;
                    el.ey = item.ey;
                    el.angel = item.angel;

                    Gdata.ellipses.Add(el);

                    Elliplse elliplse = new Elliplse() { FN = Gdata.Id, EId = el.Id, a = el.height, b = el.width, t = el.angel, x = (int)Math.Round(el.ex, MidpointRounding.AwayFromZero), y = (int)Math.Round(el.ey, MidpointRounding.AwayFromZero) };
                    frequ.ED.Add(elliplse);
                }
            }

             Gdata.width_O =Convert.ToInt64(txtWidth.Text);
             Gdata.height_O = Convert.ToInt64(txtHeight.Text);
            Gdata.angel_O = Convert.ToInt64(txtAngel.Text);


            ellipseWrite.FD.Add(frequ);

            Mode _mode = new Mode() { FC = 2, M = DeviceCOM.Mode };
            if (_mode.M == 1)
            {
                _mode.OE = new OuterElliplse { a = Gdata.height_O, b = Gdata.width_O, t = Gdata.angel_O };
            }
            portCOM.WriteData(JsonConvert.SerializeObject(_mode));

            var rat = portCOM.WriteData(JsonConvert.SerializeObject(ellipseWrite));
            if (rat)
            {
                lblMsg.Content = "Configuration Saved!!!";
            }
            else
            {
                lblMsg.Content = "Configuration Saved but no response from the board, please reboot it and write the configuration again!!!";
            }

        }

        public List<String> Validaton()
        {
            List<String> validationMsg = new List<string>();
            //if (string.IsNullOrEmpty(txtAngel.Text))
            //{
            //    validationMsg.Add("Angle is required and the range is 0 to 359.");
            //}
            //else
            //{
            //    if (Convert.ToInt32(txtAngel.Text) < 0 || Convert.ToInt32(txtAngel.Text) > 359)
            //    {
            //        validationMsg.Add("Angle is required and the range is 0 to 359.");
            //    }
            //}
            //if (string.IsNullOrEmpty(txtHeight.Text))
            //{
            //    validationMsg.Add("Height is required and the range is 100 to 5000.");
            //}
            //else
            //{
            //    if (Convert.ToInt32(txtHeight.Text) < 100 || Convert.ToInt32(txtHeight.Text) > 5000)
            //    {
            //        validationMsg.Add("Height is required and the range is 100 to 5000.");
            //    }
            //}

            //if (string.IsNullOrEmpty(txtWidth.Text))
            //{
            //    validationMsg.Add("Width is required and the range is 100 to 5000.");
            //}
            //else
            //{
            //    if (Convert.ToInt32(txtWidth.Text) < 100 || Convert.ToInt32(txtWidth.Text) > 5000)
            //    {
            //        validationMsg.Add("Width is required and the range is 100 to 5000.");
            //    }
            //}

            //if (string.IsNullOrEmpty(txtX_Shift.Text))
            //{
            //    validationMsg.Add("X Offset is required and the range is -2000 to 2000.");
            //}
            //else
            //{
            //    if (Convert.ToInt32(txtX_Shift.Text) < -2000 || Convert.ToInt32(txtX_Shift.Text) > 2000)
            //    {
            //        validationMsg.Add("X Offset is required and the range is -2000 to 2000.");
            //    }
            //}

            //if (string.IsNullOrEmpty(txtY_Shift.Text))
            //{
            //    validationMsg.Add("Y Offset is required and the range is -2000 to 2000.");
            //}
            //else
            //{
            //    if (Convert.ToInt32(txtY_Shift.Text) < -2000 || Convert.ToInt32(txtY_Shift.Text) > 2000)
            //    {
            //        validationMsg.Add("Y Offset is required and the range is -2000 to 2000.");
            //    }
            //}

            return validationMsg;
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

        private void btnNew_MouseDown(object sender, MouseButtonEventArgs e)
        {
            EllipsDTO ellips = new EllipsDTO();
            ellips.Id = ellipses.Count+1;
            ellips.height= DeviceCOM.DefaultHeight;
            ellips.width = DeviceCOM.DefaultWidth;
            //ellips.ColorName = MyColor.GetColor(ellipses.Count).ToString();
            ellipses.Add(ellips);

            gdFreq.ItemsSource = null;
            gdFreq.ItemsSource = ellipses;

        }

        private void btn_installSnippet_Click(object sender, RoutedEventArgs e)
        {
            if (gdFreq.SelectedItem != null)
            {
                ellipses.Remove((EllipsDTO)gdFreq.SelectedItem);
                gdFreq.ItemsSource = null;
                gdFreq.ItemsSource = ellipses;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (IsSaved)
            {
                btnConfigSave_Click(btnConfigSave, null);
            }
            //this.Close();
        }

        private void ddlFrChennel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var text = e.AddedItems[0].ToString();
            ChangeFreq(text);
        }
    }
}
