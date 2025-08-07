using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.IO;
using System.IO.Ports;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Newtonsoft.Json;
using OpenTK.Windowing.Common.Input;
using ScottPlot;
using ScottPlot.Colormaps;
using ScottPlot.Plottables;

namespace Eddy
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ObservableCollection<MenuItemViewModel> MenuItems { get; set; }
        SerialPort portR;
        ScottPlot.Plot myPlot1;
        ScottPlot.Plot myPlot2;
        ScottPlot.Plot myPlot3;
        ScottPlot.Plot myPlot4;
        // setup a logger that will grow as data is added
        DataLogger logger1;
        DataLogger logger2;
        DataLogger logger3;
        DataLogger logger4;
        public DeviceCOM deviceCOM;

        DispatcherTimer dispatcherTimer;
        TcpClient client;
        NetworkStream stream;
        int CommunicationType = 0;

        string IpAddress;
        int Port;
        public MainWindow()
        {
            InitializeComponent();

            MenuItems = new ObservableCollection<MenuItemViewModel>
            {
                new MenuItemViewModel { Header = "File",
                    MenuItems = new ObservableCollection<MenuItemViewModel>
                        {
                            new MenuItemViewModel { Header = "New", mainWindow =this },
                            new MenuItemViewModel { Header = "Open" ,mainWindow =this },
                            new MenuItemViewModel { Header = "Save", mainWindow =this },
                            new MenuItemViewModel { Header = "Save As", mainWindow =this },
                            new MenuItemViewModel { Header = "Exit" ,mainWindow =this }
                        }
                },
                new MenuItemViewModel { Header = "Configuration",
                    MenuItems = new ObservableCollection<MenuItemViewModel>
                        {
                            new MenuItemViewModel { Header = "Marker Setting", mainWindow = this },
                            new MenuItemViewModel { Header = "Frequency Setting", mainWindow = this },
                            new MenuItemViewModel { Header = "Write Configuration", mainWindow = this },
                        }
                },
                new MenuItemViewModel { Header = "View Log" },
            };

            this.DataContext = this;
            myPlot1 = WpfPlot1.Plot;
            myPlot1.Title("D1 Response");
            //myPlot1.Axes.SetLimits(0, 1000, 0, 2000);
            //logger1 = myPlot1.Add.DataLogger();
            WpfPlot1.Refresh();

            myPlot2 = WpfPlot2.Plot;
            myPlot2.Title("D2 Response");
            //myPlot2.Axes.SetLimits(0, 1000, 0, 2000);
            //logger2 = myPlot2.Add.DataLogger();
            WpfPlot2.Refresh();

            myPlot3 = WpfPlot3.Plot;
            myPlot3.Title("Absolute Response");
            //myPlot3.Axes.SetLimits(0, 1000, 0, 2000);
            //logger3 = myPlot3.Add.DataLogger();
            WpfPlot3.Refresh();

            //myPlot4 = WpfPlot4.Plot;
            //myPlot4.Title("Last D1  Response");
            //myPlot4.Axes.SetLimits(0, 1000, 0, 2000);
            //logger4 = myPlot4.Add.DataLogger();
            //WpfPlot4.Refresh();

            DeviceCOM.graphData = new GraphData();

            // Prepare Configurtion data
            DeviceCOM.Configuration = new Configuration();
            DeviceCOM.Configuration.Marker = new Marker();
            DeviceCOM.Configuration.Frequency = new Frequency();
            DeviceCOM.Configuration.Frequency.FD = new List<FD>();
            DeviceCOM.Configuration.Frequency.FD.Add(new FD() { FN = 1 });
            DeviceCOM.Configuration.Frequency.FD.Add(new FD() { FN = 2 });
            DeviceCOM.Configuration.Frequency.FD.Add(new FD() { FN = 3 });
            DeviceCOM.Configuration.Filter = new Filter();
            DeviceCOM.Configuration.Filter.FD = new List<FilterFD>();
            DeviceCOM.Configuration.Filter.FD.Add(new FilterFD() { FN = 1 });
            DeviceCOM.Configuration.Filter.FD.Add(new FilterFD() { FN = 2 });
            DeviceCOM.Configuration.Filter.FD.Add(new FilterFD() { FN = 3 });

            DeviceCOM.BaudRate = Convert.ToInt32(System.Configuration.ConfigurationSettings.AppSettings["BaudRate"]);
            DeviceCOM.PortName = Convert.ToString(System.Configuration.ConfigurationSettings.AppSettings["PortName"]);

            deviceCOM = new DeviceCOM();
            deviceCOM.InitialPort();

            deviceCOM.WriteData(JsonConvert.SerializeObject(DeviceCOM.Configuration.Marker));
            deviceCOM.WriteData(JsonConvert.SerializeObject(DeviceCOM.Configuration.Frequency));
            deviceCOM.WriteData(JsonConvert.SerializeObject(DeviceCOM.Configuration.Filter));

            IpAddress = Convert.ToString(System.Configuration.ConfigurationSettings.AppSettings["IP"]);
            Port = Convert.ToInt32(System.Configuration.ConfigurationSettings.AppSettings["Port"]);

            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(5000000);
            //dispatcherTimer.Start();
            client = new TcpClient();

            Status status = new Status() { FC = 23 };
            var rat = deviceCOM.GetSystemStatus(JsonConvert.SerializeObject(status));
            // Marked Busy Flag 
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            
            client.NoDelay = false;
            if (!client.Connected)
            {
                client = new TcpClient();
                IPAddress iPAddress = IPAddress.Parse(IpAddress);
                var ipEndPoint = new IPEndPoint(iPAddress, Port);
                client.Connect(ipEndPoint);
            }
            if (client.Connected)
            {
                stream = client.GetStream();
                if (stream.DataAvailable)
                {
                    try
                    {
                        var buffer = new byte[client.Available];
                        int received = stream.Read(buffer);
                        var message = Encoding.UTF8.GetString(buffer, 0, received);
                        new Thread(() =>
                        {
                            //ProcessPortData(buffer);
                        }).Start();
                    }
                    catch (Exception ex)
                    {

                    }
                }
            }
        }

        private void ProcessPortData(byte[] indata)
        {
            try
            {
                if (indata.Length == 1)
                {
                    if (indata[0] == 53 || indata[0] == 54)
                    {

                    }
                }
                else if (indata[0] == 55)
                {
                    int NoOfSamples = indata[1];
                    int startIndex = indata[2] + (indata[3] * 256) + (indata[4] * 256) + (indata[5] * 256);
                    int FN1 = indata[6];
                    int markerIndex = indata[7] + (indata[8] * 256) + (indata[9] * 256) + (indata[10] * 256);

                    int fStartIndex = 11;

                    if (startIndex == 1)
                    {
                        myPlot4.Clear();
                        logger4 = myPlot4.Add.DataLogger();
                        logger4.Clear();
                        var AMPDat = DeviceCOM.graphData.AmpD1.ToList();

                        if (AMPDat.Count > 0)
                        {
                            bool result = true;
                            for (int i = 0; i < AMPDat.Count; i++)
                            {
                                var item = AMPDat[i];
                                logger4.Add(item);
                                var lst = DeviceCOM.graphData.D1MarkerIndexs.ToList();
                                var obj = lst.FirstOrDefault(j => j == i);
                                if (obj > 0)
                                {
                                    myPlot4.Add.Scatter(i, item);
                                    result = false;
                                }
                                myPlot4.Axes.SetLimits(0, DeviceCOM.graphData.AmpD1.Count, 0, 2000);
                                WpfPlot4.Refresh();
                            }
                            // Counter and Log Create Id, Result, Timestamp, graphData(JSON), Configuration(JSON) 
                            // Database call 
                        }
                        DeviceCOM.graphData.AmpD1 = new List<int>();
                        DeviceCOM.graphData.AmpD2 = new List<int>();
                        DeviceCOM.graphData.AmpD3 = new List<int>();
                        DeviceCOM.graphData.D1MarkerIndexs = new List<int>();
                        DeviceCOM.graphData.D2MarkerIndexs = new List<int>();
                        DeviceCOM.graphData.D3MarkerIndexs = new List<int>();

                        myPlot1.Clear();
                        logger1 = myPlot1.Add.DataLogger();
                        myPlot2.Clear();
                        logger2 = myPlot2.Add.DataLogger();
                        myPlot3.Clear();
                        logger3 = myPlot3.Add.DataLogger();
                        logger1.Clear();
                        logger2.Clear();
                        logger3.Clear();
                    }

                    // FN -- First 
                    for (int i = 0; i < NoOfSamples; i++)
                    {
                        int amp = indata[fStartIndex] + (indata[fStartIndex + 1] * 256);
                        DeviceCOM.graphData.AmpD1.Add(amp);
                        int phase = indata[fStartIndex + 2] + (indata[fStartIndex + 3] * 256);
                        int x = indata[fStartIndex + 4] + (indata[fStartIndex + 6] * 256);
                        int y = indata[fStartIndex + 6] + (indata[fStartIndex + 7] * 256);
                        fStartIndex = fStartIndex + 8;

                        logger1.Add(amp);

                        if (markerIndex > 0)
                        {
                            if (markerIndex == (startIndex + i + 1))
                            {
                                DeviceCOM.graphData.D1MarkerIndexs.Add(DeviceCOM.graphData.AmpD1.Count);
                                myPlot1.Add.Scatter(DeviceCOM.graphData.AmpD1.Count, amp);
                            }
                        }
                        myPlot1.Axes.SetLimits(0, 500, 0, 2000);
                        WpfPlot1.Refresh();
                    }

                    int FN2 = indata[fStartIndex];
                    int markerIndex2 = indata[fStartIndex + 1] + (indata[fStartIndex + 2] * 256) + (indata[fStartIndex + 3] * 256) + (indata[fStartIndex + 4] * 256);

                    fStartIndex = fStartIndex + 5;

                    for (int i = 0; i < NoOfSamples; i++)
                    {
                        int amp = indata[fStartIndex] + (indata[fStartIndex + 1] * 256);
                        DeviceCOM.graphData.AmpD2.Add(amp);
                        int phase = indata[fStartIndex + 2] + (indata[fStartIndex + 3] * 256);
                        int x = indata[fStartIndex + 4] + (indata[fStartIndex + 6] * 256);
                        int y = indata[fStartIndex + 6] + (indata[fStartIndex + 7] * 256);
                        fStartIndex = fStartIndex + 8;

                        logger2.Add(amp);
                        if (markerIndex2 > 0)
                        {
                            if (markerIndex2 == (startIndex + i + 1))
                            {
                                DeviceCOM.graphData.D2MarkerIndexs.Add(DeviceCOM.graphData.AmpD2.Count);
                                myPlot2.Add.Scatter(DeviceCOM.graphData.AmpD2.Count, amp);
                            }
                        }
                        myPlot2.Axes.SetLimits(0, 500, 0, 2000);
                        WpfPlot2.Refresh();
                    }

                    int FN3 = indata[fStartIndex];
                    int markerIndex3 = indata[fStartIndex + 1] + (indata[fStartIndex + 2] * 256) + (indata[fStartIndex + 3] * 256) + (indata[fStartIndex + 4] * 256);

                    fStartIndex = fStartIndex + 5;

                    for (int i = 0; i < NoOfSamples; i++)
                    {
                        int amp = indata[fStartIndex] + (indata[fStartIndex + 1] * 256);
                        DeviceCOM.graphData.AmpD3.Add(amp);
                        int phase = indata[fStartIndex + 2] + (indata[fStartIndex + 3] * 256);
                        int x = indata[fStartIndex + 4] + (indata[fStartIndex + 6] * 256);
                        int y = indata[fStartIndex + 6] + (indata[fStartIndex + 7] * 256);
                        fStartIndex = fStartIndex + 8;

                        logger3.Add(amp);
                        if (markerIndex3 > 0)
                        {
                            if (markerIndex3 == (startIndex + i + 1))
                            {
                                DeviceCOM.graphData.D3MarkerIndexs.Add(DeviceCOM.graphData.AmpD3.Count);
                                myPlot3.Add.Scatter(DeviceCOM.graphData.AmpD3.Count, amp);
                            }
                        }
                        myPlot3.Axes.SetLimits(0, 500, 0, 2000);
                        WpfPlot3.Refresh();
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }

        private void ProcessPortData(string indata)
        {
            try
            {
                if (!string.IsNullOrEmpty(indata))
                {
                    //var res = JsonConvert.DeserializeObject<Response>(indata);

                    //if (res.FC == 21)
                    //{
                    //    //IsSystemBusy = true;
                    //    //busyStamp = System.DateTime.Now;
                    //}
                    //else if (res.FC == 22)
                    //{
                    //    //IsSystemBusy = false;                        
                    //}
                }
            }
            catch (Exception ex)
            {

            }
        }
    }


    public class MenuItemViewModel
    {
        private readonly ICommand _command;

        public MenuItemViewModel()
        {
            _command = new CommandViewModel(Execute);
        }
        public string Header { get; set; }
        string filename { get; set; }
        public MainWindow mainWindow { get; set; }
        public ObservableCollection<MenuItemViewModel> MenuItems { get; set; }

        public FrequencySetting freqPop { get; set; }
        public MarkerSetting markerPop { get; set; }


        public ICommand Command
        {
            get
            {
                return _command;
            }
        }

        private void Execute()
        {
            if (Header == "Save")
            {
                try
                {
                    if (String.IsNullOrEmpty(filename))
                    {
                        Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
                        dlg.FileName = "Document"; // Default file name
                        dlg.DefaultExt = ".text"; // Default file extension
                        dlg.Filter = "Text documents (.txt)|*.txt"; // Filter files by extension

                        // Show save file dialog box
                        Nullable<bool> result = dlg.ShowDialog();

                        // Process save file dialog box results
                        if (result == true)
                        {
                            // Save document
                            filename = dlg.FileName;

                            string conecnt = JsonConvert.SerializeObject(DeviceCOM.Configuration);
                            File.WriteAllText(filename, conecnt);
                            //MessageBox.Show("Configuation changes saved at '" + filename + "'!!!!");
                            this.mainWindow.lblConfigFileName.Content = filename;
                        }

                    }
                    else
                    {
                        string conecnt = JsonConvert.SerializeObject(DeviceCOM.Configuration);
                        File.WriteAllText(filename, conecnt);
                    }

                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error while saving the configation file!!!!", "Error Information");
                }

            }
            else if (Header == "Save As")
            {
                try
                {
                    Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
                    dlg.FileName = "Document"; // Default file name
                    dlg.DefaultExt = ".text"; // Default file extension
                    dlg.Filter = "Text documents (.txt)|*.txt"; // Filter files by extension

                    // Show save file dialog box
                    Nullable<bool> result = dlg.ShowDialog();

                    // Process save file dialog box results
                    if (result == true)
                    {
                        // Save document
                        filename = dlg.FileName;

                        string conecnt = JsonConvert.SerializeObject(DeviceCOM.Configuration);
                        File.WriteAllText(filename, conecnt);
                        this.mainWindow.lblConfigFileName.Content = filename;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error while saving the configuration file!!!!", "Error Information");
                }
            }
            else if (Header == "Open")
            {
                try
                {
                    var dialog = new Microsoft.Win32.OpenFileDialog();
                    dialog.FileName = "Document"; // Default file name
                    dialog.DefaultExt = ".txt"; // Default file extension
                    dialog.Filter = "Text documents (.txt)|*.txt"; // Filter files by extension

                    // Show open file dialog box
                    bool? result = dialog.ShowDialog();

                    // Process open file dialog box results
                    if (result == true)
                    {
                        string data = File.ReadAllText(dialog.FileName);
                        DeviceCOM.Configuration = JsonConvert.DeserializeObject<Configuration>(data);
                        // Open document
                        filename = dialog.FileName;
                        this.mainWindow.lblConfigFileName.Content = filename;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error while loading the configuration file!!!!", "Error Information");
                }
            }
            else if (Header == "New")
            {
                filename = null;

            }
            else if (Header == "Exit")
            {
                //this.mainWindow.btnLog.Visibility = Visibility.Hidden;
                mainWindow.Close();
            }
            else if (Header == "Frequency Setting")
            {
                freqPop = new FrequencySetting();
                freqPop.Closing += freqPop_Closing;
                freqPop.deviceCOM = mainWindow.deviceCOM;
                freqPop.Owner = mainWindow;
                freqPop.ShowDialog();
            }
            else if (Header == "Marker Setting")
            {
                markerPop = new MarkerSetting();
                markerPop.Closing += markerPop_Closing;
                markerPop.deviceCOM = mainWindow.deviceCOM;
                markerPop.Owner = mainWindow;
                markerPop.ShowDialog();
            }
            else if (Header == "Write Configuration")
            {
                var msg = "Configuation Write successfully!!";
                var rat = mainWindow.deviceCOM.WriteData(JsonConvert.SerializeObject(DeviceCOM.Configuration.Marker));
                var rat1 = mainWindow.deviceCOM.WriteData(JsonConvert.SerializeObject(DeviceCOM.Configuration.Frequency));
                var rat2 = mainWindow.deviceCOM.WriteData(JsonConvert.SerializeObject(DeviceCOM.Configuration.Filter));

                if (!rat && rat1 && rat2)
                {
                    msg = "No response from the system, please reboot the board";
                }

                MessageBox.Show(msg, "Information");
            }
        }

        private void freqPop_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //if (freqPop.IsSaved)
            //{
            //    deviceCOM.WriteData(JsonConvert.SerializeObject(DeviceCOM.Configuration.Frequency));
            //    mainWindow.deviceCOM.WriteData(JsonConvert.SerializeObject(DeviceCOM.Configuration.Filter));
            //}
        }
        private void markerPop_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //if (markerPop.IsSaved)
            //{
            //    mainWindow.deviceCOM.WriteData(JsonConvert.SerializeObject(DeviceCOM.Configuration.Marker));   
            //}
        }

    }
    public class CommandViewModel : ICommand
    {
        private readonly Action _action;

        public CommandViewModel(Action action)
        {
            _action = action;
        }

        public void Execute(object o)
        {
            _action();
        }

        public bool CanExecute(object o)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged
        {
            add { }
            remove { }
        }
    }
}