using ConfigurationKeyGenerator.Models;
using ConfigurationKeyGenerator.Services;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;

namespace ConfigurationKeyGenerator.Views
{
    public partial class AddLicense : Window
    {
        private string selectedFile = string.Empty;

        private readonly bool isEditMode;

        private readonly ConfigurationKeyLog? existingLog;
        public AddLicense(ConfigurationKeyLog? log = null)
        {
            InitializeComponent();

            if (log != null)
            {
                isEditMode = true;
                existingLog = log;

                cmbProduct.Text = log.ProductName;
                txtCustomerName.Text = log.CustomerName;
                txtMachineId.Text = log.MachineId;
                txtConfigFile.Text = log.ConfigurationFileName;
            }
        }
        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Filter = "Text File (*.txt)|*.txt"
            };


            if (dialog.ShowDialog() == true)
            {
                selectedFile = dialog.FileName;

                txtConfigFile.Text =
                    Path.GetFileName(selectedFile);
            }
        }
        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidateForm())
                    return;


                // Check duplicate Machine ID
                string machineIdInput = txtMachineId.Text.Trim();


                ConfigurationKeyLogService logService =
                    new ConfigurationKeyLogService();

                if (!isEditMode &&
                    logService.ExistsByMachineId(machineIdInput))
                {
                    MessageBox.Show(
                        "Configuration already exists for this Machine ID.\n\nPlease edit the existing configuration.",
                        "Duplicate Machine ID",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }



                // Read uploaded configuration file

                TextFileService textService =
                    new TextFileService();


                Dictionary<string, string> keys =
                    textService.ReadConfiguration(selectedFile);




                ConfigurationGeneration config =
                    new ConfigurationGeneration
                    {
                        Product = GetSelectedProduct(),

                        CustomerName =
                            txtCustomerName.Text.Trim(),

                        MachineId =
                            machineIdInput,

                        ConfigurationKeys = keys,

                        GeneratedOn = DateTime.Now
                    };





                // Encrypt configuration

                ConfigurationFileService encryptionService =
                    new ConfigurationFileService();


                byte[] encryptedData =
                    encryptionService.GenerateEncryptedBytes(config);





                string machineId =
                    config.MachineId.Length > 10
                    ? config.MachineId.Substring(0, 10)
                    : config.MachineId;



                string customerName =
                    string.IsNullOrWhiteSpace(config.CustomerName)
                    ? "Customer"
                    : config.CustomerName.Replace(" ", "_");





                string generatedFileName =
                    $"{customerName}_{config.Product}_{machineId}.key";






                // Save record into PostgreSQL

                ConfigurationKeyLog log =
                    new ConfigurationKeyLog
                    {
                        ProductName =
                            config.Product.ToString(),


                        CustomerName =
                            config.CustomerName,


                        MachineId =
                            config.MachineId,


                        ConfigurationFileName =
                            Path.GetFileName(selectedFile),


                        GeneratedFileName =
                            generatedFileName,


                        GeneratedFile =
                            encryptedData,


                        GeneratedDate =
                            DateTime.Now
                    };





                if (isEditMode)
                {
                    log.Id = existingLog!.Id;

                    logService.Update(log);
                }
                else
                {
                    logService.Save(log);
                }



                MessageBox.Show(isEditMode? "Configuration updated successfully.": "Configuration generated successfully.","Success",MessageBoxButton.OK,MessageBoxImage.Information);

                DialogResult = true;
                Close();

            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        private ProductType GetSelectedProduct()
        {

            if (cmbProduct.SelectedItem
                is System.Windows.Controls.ComboBoxItem item)
            {

                switch (item.Content.ToString())
                {

                    case "EddyTube":
                        return ProductType.EddyTube;


                    case "EddyFaster":
                        return ProductType.EddyFaster;


                    default:
                        return ProductType.EddyShorter;
                }

            }

            return ProductType.EddyShorter;
        }
        private bool ValidateForm()
        {

            if (string.IsNullOrWhiteSpace(txtCustomerName.Text))
            {
                MessageBox.Show(
                    "Customer name is required.");

                return false;
            }





            if (string.IsNullOrWhiteSpace(txtMachineId.Text))
            {
                MessageBox.Show(
                    "Machine ID is required.");

                return false;
            }






            if (string.IsNullOrWhiteSpace(selectedFile))
            {
                MessageBox.Show(
                    "Please select configuration file.");

                return false;
            }





            return true;

        }
        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;

            Close();
        }

    }
}