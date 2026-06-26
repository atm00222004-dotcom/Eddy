using Newtonsoft.Json;
using Npgsql;
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
        List<Operator> operators;
        List<PartFamily> partFamilies;
        List<PartMaster> parts;
        public DeviceCOM portCOM;

        public PartConfigReNew()
        {
            InitializeComponent();

            if (DeviceCOM.part == null)
            {
                DeviceCOM.part = new Part();
            }
            LoadOperators();
            LoadPartFamilies();
            LoadParts();

            BindUI();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnConfigSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DeviceCOM.part == null)
                {
                    DeviceCOM.part = new Part();
                }

                var msg = Validaton();
                if (msg.Count == 0)
                {
                    DeviceCOM.part.ProductionOrder = txtProductionOrder.Text;
                    DeviceCOM.part.MachineNumber = txtMachineNumber.Text;
                    DeviceCOM.part.PartFamily = ddlPartFamily.Text;
                    DeviceCOM.part.PartNumber = ddlPartNumber.Text;


                    //Common fields
                    DeviceCOM.part.BatchName = ddlShift.Text;
                    DeviceCOM.part.Name = ddlPartNumber.Text;
                    DeviceCOM.part.CheckedBy = ddlOperator.Text;

                    if (DeviceCOM.IsLogRequiredOnBalance)
                    {

                        if (DeviceCOM.IsSystemBusy)
                        {
                            msg.Add("System is busy so you can not perform this command, please wait...");
                            return;
                        }

                        byte[] data = new byte[6];
                        data[0] = Convert.ToByte(2);
                        data[1] = Convert.ToByte(19);
                        data[2] = Convert.ToByte(1);
                        data[3] = DeviceCOM.IsLogEnable ? Convert.ToByte(2) : Convert.ToByte(1);

                        var rat = portCOM.WriteDataInBytes(data);

                        if (rat)
                        {
                            DeviceCOM.IsLogEnable = true;
                            IsSaved = true;
                            lblMsg.Content = "Log has been started!!!";
                        }
                        else
                        {
                            lblMsg.Content = "Unable to start log becuase no response from the ECT Instrument, please reboot it and start log again!!!";
                        }
                    }
                    else
                    {
                        DeviceCOM.IsLogEnable = true;
                        IsSaved = true;
                        lblMsg.Content = "Log has been started!!!";
                    }

                   
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

        public List<string> Validaton()
        {
            List<string> validationMsg = new List<string>();

            if (string.IsNullOrEmpty(txtProductionOrder.Text))
            {
                validationMsg.Add("Production Order is required.");
            }

            if (string.IsNullOrEmpty(ddlShift.Text))
            {
                validationMsg.Add("Shift is required.");
            }

            if (string.IsNullOrEmpty(ddlOperator.Text))
            {
                validationMsg.Add("Operator Name is required.");
            }

            if (string.IsNullOrEmpty(txtMachineNumber.Text))
            {
                validationMsg.Add("Machine Number is required.");
            }

            if (string.IsNullOrEmpty(ddlPartFamily.Text))
            {
                validationMsg.Add("Part Family is required.");
            }

            if (string.IsNullOrEmpty(ddlPartNumber.Text))
            {
                validationMsg.Add("Part Number is required.");
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

        private void ddlPartFamily_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ddlPartFamily.SelectedItem == null)
                return;

            string selectedFamily = ddlPartFamily.SelectedItem.ToString();

            int familyId = partFamilies
                .First(x => x.FamilyName == selectedFamily)
                .Id;

            ddlPartNumber.ItemsSource = parts
                .Where(x => x.PartFamilyId == familyId)
                .Select(x => x.PartNumber)
                .ToList();
        }

        private void BindUI()
        {
            ddlPartFamily.ItemsSource = partFamilies
                .Select(x => x.FamilyName)
                .ToList();

            ddlShift.ItemsSource = new List<string> { "I", "II", "III" };

            ddlOperator.ItemsSource = operators.Select(x => x.OperatorName).ToList();

            txtProductionOrder.Text = DeviceCOM.part?.ProductionOrder;
            txtMachineNumber.Text = DeviceCOM.part?.MachineNumber;

            ddlShift.Text = DeviceCOM.part?.BatchName;
            ddlPartFamily.Text = DeviceCOM.part?.PartFamily;
            ddlPartNumber.Text = DeviceCOM.part?.PartNumber;

            ddlOperator.Text = DeviceCOM.part?.CheckedBy;
        }

        private void LoadOperators()
        {
            operators = new List<Operator>();

            using (var con = new NpgsqlConnection(
                System.Configuration.ConfigurationSettings.AppSettings["ConnectionString"]))
            {
                con.Open();

                string query = "SELECT \"OperatorName\" FROM \"Operators\" WHERE \"IsActive\" = true";

                using (var cmd = new NpgsqlCommand(query, con))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        operators.Add(new Operator
                        {
                            OperatorName = reader.GetString(0)
                        });
                    }
                }
            }
        }

        private void LoadParts()
        {
            parts = new List<PartMaster>();

            using (var con = new NpgsqlConnection(
                System.Configuration.ConfigurationSettings.AppSettings["ConnectionString"]))
            {
                con.Open();

                string query =
                    "SELECT \"Id\", \"PartFamilyId\", \"PartNumber\" " +
                    "FROM \"Parts\" " +
                    "WHERE \"IsActive\" = true";

                using (var cmd = new NpgsqlCommand(query, con))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        parts.Add(new PartMaster
                        {
                            Id = reader.GetInt32(0),
                            PartFamilyId = reader.GetInt32(1),
                            PartNumber = reader.GetString(2),
                            IsActive = true
                        });
                    }
                }
            }
        }

        private void LoadPartFamilies()
        {
            partFamilies = new List<PartFamily>();

            using (var con = new NpgsqlConnection(
                System.Configuration.ConfigurationSettings.AppSettings["ConnectionString"]))
            {
                con.Open();

                string query =
                    "SELECT \"Id\", \"FamilyName\" " +
                    "FROM \"PartFamilies\" " +
                    "WHERE \"IsActive\" = true";

                using (var cmd = new NpgsqlCommand(query, con))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        partFamilies.Add(new PartFamily
                        {
                            Id = reader.GetInt32(0),
                            FamilyName = reader.GetString(1),
                            IsActive = true
                        });
                    }
                }
            }
        }
    }
}
