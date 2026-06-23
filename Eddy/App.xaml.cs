using System;
using System.Configuration;
using System.Windows;

namespace Eddy
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            bool isAbsolute = Convert.ToBoolean(ConfigurationManager.AppSettings["isAbsolute"]);

            Window window;

            if (isAbsolute)
            {
                window = new MainWindow_APS();
            }
            else
            {
                window = new MainWindow();
            }

            window.Show();
        }
    }
}