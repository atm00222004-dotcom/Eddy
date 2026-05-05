using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IO;
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
    public partial class PartConfigReNew : Window
    {
        public bool IsSaved = false;
        List<PartTypeData> parts;
        public PartConfigReNew()
        {
            InitializeComponent();
            if (DeviceCOM.part == null )
                DeviceCOM.part = new Part();

            if(File.Exists("PartTypeData.json"))
            {
                string json = File.ReadAllText("PartTypeData.json");

                parts = JsonConvert.DeserializeObject<List<PartTypeData>>(json);
            }
            else
            {
                parts = new List<PartTypeData>();
            }

            ddlPartType.ItemsSource = parts;
            ddlPartType.DisplayMemberPath = "Name";
            ddlPartType.SelectedValuePath = "Name";

            ddlPartType.Text = DeviceCOM.part.BatchName;
            ddlPart.Text = DeviceCOM.part.Name;           
            txtCheckedBy.Text = DeviceCOM.part.CheckedBy;
            txtCompanyName.Text = DeviceCOM.part.CompanyName;           
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
                    DeviceCOM.part.BatchName = ddlPartType.Text;
                    DeviceCOM.part.Name = ddlPart.Text;                   
                    DeviceCOM.part.CheckedBy = txtCheckedBy.Text;
                    DeviceCOM.part.CompanyName = txtCompanyName.Text;                
                    
                    DeviceCOM.IsLogEnable = true;
                    IsSaved = true;
                    lblMsg.Content = "Log has been started!!!";
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
            if (string.IsNullOrEmpty(ddlPartType.Text))
            {
                validationMsg.Add("Part type is required.");
            }
            if (string.IsNullOrEmpty(ddlPart.Text))
            {
                validationMsg.Add("Part is required.");
            }
            

            if (string.IsNullOrEmpty(txtCompanyName.Text))
            {
                validationMsg.Add("Company Name is required.");
            }

            if (string.IsNullOrEmpty(txtCheckedBy.Text))
            {
                validationMsg.Add("Checked By is required.");
            }

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

        private void ddlPartType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var text = (PartTypeData)e.AddedItems[0];
            ddlPart.ItemsSource = text.Values;
        }
    }
}
