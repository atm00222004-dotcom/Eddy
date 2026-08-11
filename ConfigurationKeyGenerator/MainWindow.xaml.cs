using System.Windows;
using ConfigurationKeyGenerator.Views;


namespace ConfigurationKeyGenerator
{
    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            InitializeComponent();

            MainFrame.Navigate(new LicenseList());
        }

    }
}