using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Npgsql;
using OpenTK.Compute.OpenCL;
using OpenTK.Windowing.Common.Input;
using ScottPlot;
using ScottPlot.Colormaps;
using ScottPlot.Interactivity;
using ScottPlot.Plottables;
using ScottPlot.TickGenerators;
using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.IO;
using System.IO.Ports;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Security.Policy;
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
using static SkiaSharp.HarfBuzz.SKShaper;
using Colors = System.Windows.Media.Colors;
using Ellipse = System.Windows.Shapes.Ellipse;

namespace Eddy
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow_APS : Window
    {
        public ObservableCollection<MenuItemViewModel_APS> MenuItems { get; set; }
        SerialPort portR;
        ScottPlot.Plot myPlot1;
        //ScottPlot.Plot myPlot2;
        //ScottPlot.Plot myPlot3;
        ScottPlot.Plot myPlot4;
        // setup a logger that will grow as data is added
        DataStreamer logger1;
        //DataLogger logger2;
        //DataLogger logger3;
        DataLogger logger4;
        public DeviceCOM deviceCOM;
        public string filename { get; set; }

        DispatcherTimer dispatcherTimer;
        DispatcherTimer dispatcherTimerui;
        int CommunicationType = 0;
        public PartConfig partConfig { get; set; }

        UdpReceiver receiver;
        string IpAddress;
        int Port;
        string resultStatus = String.Empty;



        public MainWindow_APS()
        {
            InitializeComponent();
            //resultStatus = "Invalid Result!!";
            //DeviceCOM.Ok = 100;
            //DeviceCOM.NoOk = 200;
            DeviceCOM.IsAttRequired = Convert.ToBoolean(System.Configuration.ConfigurationSettings.AppSettings["IsAttRequired"]);

            MenuItems = new ObservableCollection<MenuItemViewModel_APS>
            {
                new MenuItemViewModel_APS { Header = "File",
                    MenuItems = new ObservableCollection<MenuItemViewModel_APS>
                        {
                            new MenuItemViewModel_APS { Header = "New", mainWindow =this },
                            new MenuItemViewModel_APS { Header = "Open" ,mainWindow =this },
                            new MenuItemViewModel_APS { Header = "Save", mainWindow =this },
                            new MenuItemViewModel_APS { Header = "Save As", mainWindow =this },
                            new MenuItemViewModel_APS { Header = "Exit" ,mainWindow =this }
                        }
                },
                new MenuItemViewModel_APS { Header = "Configuration",
                    MenuItems =
                    (DeviceCOM.IsAttRequired==true ?new ObservableCollection<MenuItemViewModel_APS>
                        {
                            new MenuItemViewModel_APS { Header = "Marker Setting", mainWindow = this },
                            new MenuItemViewModel_APS { Header = "Frequency Setting", mainWindow = this },
                            new MenuItemViewModel_APS { Header = "Attenuation", mainWindow = this },
                            new MenuItemViewModel_APS { Header = "Write Configuration", mainWindow = this },
                        } : new ObservableCollection<MenuItemViewModel_APS>
                        {
                            new MenuItemViewModel_APS { Header = "Marker Setting", mainWindow = this },
                            new MenuItemViewModel_APS { Header = "Frequency Setting", mainWindow = this },
                            new MenuItemViewModel_APS { Header = "Write Configuration", mainWindow = this },
                        })

                },
                new MenuItemViewModel_APS { Header = "View Log",
                    MenuItems = new ObservableCollection<MenuItemViewModel_APS>
                    {
                        new MenuItemViewModel_APS { Header = "Batch Wise Log", mainWindow =this }
                    }
                },
            };
            //DeviceCOM.dataBuffer = new double[8000];
            this.DataContext = this;


            int tt = Convert.ToInt16(System.Configuration.ConfigurationSettings.AppSettings["TestTime"]);
            int ss = Convert.ToInt16(System.Configuration.ConfigurationSettings.AppSettings["SamplePerSecond"]);


            //logger1 = myPlot1.Add.DataLogger();            

            DeviceCOM.graphData = new GraphData();

            // Prepare Configurtion data
            DeviceCOM.Configuration = new Configuration() { TestTime = tt, SamplePerSecond = ss };
            DeviceCOM.Configuration.Marker = new Marker();
            DeviceCOM.Configuration.Frequency = new Frequency();
            DeviceCOM.Configuration.Frequency.FD = new List<FD>();
            DeviceCOM.Configuration.Frequency.FD.Add(new FD() { FN = 1 });
            //DeviceCOM.Configuration.Frequency.FD.Add(new FD() { FN = 2 });
            //DeviceCOM.Configuration.Frequency.FD.Add(new FD() { FN = 2, E = 0 });
            DeviceCOM.Configuration.Filter = new Filter();
            DeviceCOM.Configuration.Filter.FD = new List<FilterFD>();
            DeviceCOM.Configuration.Filter.FD.Add(new FilterFD() { FN = 1 });
            //DeviceCOM.Configuration.Filter.FD.Add(new FilterFD() { FN = 2 });

            DeviceCOM.BaudRate = Convert.ToInt32(System.Configuration.ConfigurationSettings.AppSettings["BaudRate"]);
            DeviceCOM.PortName = Convert.ToString(System.Configuration.ConfigurationSettings.AppSettings["PortName"]);

            DeviceCOM.MaxValue = Convert.ToInt32(System.Configuration.ConfigurationSettings.AppSettings["MaxValue"]);
            DeviceCOM.Factor = Convert.ToInt32(System.Configuration.ConfigurationSettings.AppSettings["Factor"]);
            DeviceCOM.DBConnection = Convert.ToString(System.Configuration.ConfigurationSettings.AppSettings["DBConnection"]);

            List<int> statuses = new List<int>();
            statuses.Add(1);
            statuses.Add(2);
            statuses.Add(3);
            statuses.Add(4);
            statuses.Add(5);
            statuses.Add(6);
            statuses.Add(7);
            statuses.Add(8);
            statuses.Add(9);
            statuses.Add(10);
            statuses.Add(11);
            statuses.Add(12);
            statuses.Add(13);
            statuses.Add(14);
            statuses.Add(15);
            ddlTT.ItemsSource = statuses;


            InitialGraphSetting();
            logger1 = myPlot1.Add.DataStreamer((tt * ss));
            logger1.LineColor = ScottPlot.Colors.LightBlue; // Change line color here

            //logger1.ViewScrollRight();
            logger1.ViewScrollLeft();

            deviceCOM = new DeviceCOM();
            deviceCOM.InitialPort();

            if (System.IO.File.Exists("Config.txt"))
            {
                DeviceCOM.Configuration = JsonConvert.DeserializeObject<Configuration>(System.IO.File.ReadAllText("Config.txt"));
                ddlTT.SelectedIndex = DeviceCOM.Configuration.TestTime - 1;
            }
            else
            {
                ddlTT.SelectedIndex = tt - 1;
            }

            deviceCOM.WriteData(JsonConvert.SerializeObject(DeviceCOM.Configuration.Marker));
            var IsEddyAdvance = Convert.ToBoolean(System.Configuration.ConfigurationSettings.AppSettings["IsEddyAdvance"]);
            if (IsEddyAdvance)
            {
                ConfigurationToWrite configurationToWrite = new ConfigurationToWrite();
                configurationToWrite.FQ = DeviceCOM.Configuration.Frequency.FD;
                configurationToWrite.FT = DeviceCOM.Configuration.Filter.FD;
                deviceCOM.WriteData(JsonConvert.SerializeObject(configurationToWrite));
            }
            else
            {
                deviceCOM.WriteData(JsonConvert.SerializeObject(DeviceCOM.Configuration.Frequency));
                Filter1 filter1 = new Filter1();
                filter1.FD = new List<FilterFD1>();

                foreach (var item in DeviceCOM.Configuration.Filter.FD)
                {
                    filter1.FD.Add(new FilterFD1 { FN = item.FN, H = item.H, L = item.L });
                }

                deviceCOM.WriteData(JsonConvert.SerializeObject(filter1));


            }


            //ConfigurationToWrite configurationWrite = new ConfigurationToWrite();
            //configurationWrite.Frequency = DeviceCOM.Configuration.Frequency;
            //configurationWrite.Filter = DeviceCOM.Configuration.Filter;
            //deviceCOM.WriteData(JsonConvert.SerializeObject(configurationWrite));

            IpAddress = Convert.ToString(System.Configuration.ConfigurationSettings.AppSettings["IP"]);
            Port = Convert.ToInt32(System.Configuration.ConfigurationSettings.AppSettings["Port"]);

            receiver = new UdpReceiver(Port);
            receiver.StartReceiving();

            //dispatcherTimer = new DispatcherTimer();
            //dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            //dispatcherTimer.Interval = new TimeSpan(0,0,0,0,10);
            //dispatcherTimer.Start();

            dispatcherTimerui = new DispatcherTimer();
            dispatcherTimerui.Tick += new EventHandler(dispatcherTimerui_Tick);
            dispatcherTimerui.Interval = new TimeSpan(0, 0, 0, 0, 10);
            dispatcherTimerui.Start();

            Task.Run(() => PollLoop());

            //Task.Run(() => UIUpdateLoop());

            //Status status = new Status() { FC = 23 };
            //var rat = deviceCOM.GetSystemStatus(JsonConvert.SerializeObject(status));
            // Marked Busy Flag 

            List<PieSlice> pieSlices = new List<PieSlice>();

            PieSlice pieSliceG = new PieSlice(0, ScottPlot.Colors.Green, "Ok");
            pieSlices.Add(pieSliceG);

            PieSlice pieSliceN = new PieSlice(0, ScottPlot.Colors.Red, "Not Ok");
            pieSlices.Add(pieSliceN);

            //PieSlice pieSliceT = new PieSlice(20, ScottPlot.Colors.Orange, "Total");
            //pieSlices.Add(pieSliceT);

            var pie = wpCounter.Plot.Add.Pie(pieSlices);

            wpCounter.Plot.Axes.Bottom.IsVisible = false;
            wpCounter.Plot.Axes.Left.IsVisible = false;

            wpCounter.Plot.Title("Counter Distribution");
            wpCounter.Refresh();

            //Get counter file and Reset wpCounter


        }

        HorizontalLine thresholdLine4;
        HorizontalLine thresholdLine5;
        HorizontalLine thresholdLine6;
        public void InitialGraphSetting()
        {
            var limits = new ScottPlot.AxisLimits(0, (DeviceCOM.Configuration.TestTime * DeviceCOM.Configuration.SamplePerSecond), 0, DeviceCOM.Factor);
            var rule = new ScottPlot.AxisRules.MinimumBoundary(
                xAxis: WpfPlot1.Plot.Axes.Bottom,
                yAxis: WpfPlot1.Plot.Axes.Left,
                limits: limits
            );

            WpfPlot1.Plot.Axes.Rules.Clear();
            WpfPlot1.Plot.Axes.Rules.Add(rule);

            var d1 = DeviceCOM.Configuration.Frequency.FD.FirstOrDefault(f => f.FN == 1);
            myPlot1 = WpfPlot1.Plot;

            myPlot1.Title("D1 Response(" + d1.F.ToString() + "," + d1.G.ToString() + "," + d1.PP.ToString() + ")");

            //myPlot1.Grid.XAxis.IsVisible = false;
            //myPlot1.Grid.XAxis.IsVisible = false;

            myPlot1.Axes.Bottom.IsVisible = false;
            //myPlot1.Axes.Left.IsVisible = false;

            WpfPlot1.Plot.FigureBackground.Color = ScottPlot.Colors.DarkGray;  // entire canvas background
            WpfPlot1.Plot.DataBackground.Color = ScottPlot.Colors.Black;

            // Set grid line colors
            WpfPlot1.Plot.Grid.LineColor = ScottPlot.Colors.Gray;
            //WpfPlot1.Plot.Grid.IsVisible = false;

            WpfPlot1.Plot.Axes.Bottom.TickGenerator = new NumericFixedInterval(2000000000); // 10 units
            WpfPlot1.Plot.Axes.Top.TickGenerator = new NumericFixedInterval(2000000000); // 10 units
            WpfPlot1.Plot.Axes.Left.TickGenerator = new NumericFixedInterval(20);   // 20 units
            WpfPlot1.Plot.Grid.LineWidth = 1;

            WpfPlot1.Refresh();


            var limits1 = new ScottPlot.AxisLimits(0, 20, 0, DeviceCOM.Factor);
            var rule1 = new ScottPlot.AxisRules.MinimumBoundary(
                xAxis: WpfPlot4.Plot.Axes.Bottom,
                yAxis: WpfPlot4.Plot.Axes.Left,
                limits: limits1
            );

            WpfPlot4.Plot.Axes.Rules.Clear();
            WpfPlot4.Plot.Axes.Rules.Add(rule1);

            myPlot4 = WpfPlot4.Plot;
            myPlot4.Title("Last D1  Response(" + d1.F.ToString() + "," + d1.G.ToString() + "," + d1.PP.ToString() + ")"); ;

            //myPlot4.Grid.XAxis.IsVisible = false;
            //myPlot4.Grid.XAxis.IsVisible = false;

            logger4 = myPlot4.Add.DataLogger();
            logger4.LineColor = ScottPlot.Colors.Blue; // Change line color here


            myPlot4.Axes.Bottom.IsVisible = false;
            //myPlot4.Axes.Left.IsVisible = false;

            WpfPlot4.Plot.FigureBackground.Color = ScottPlot.Colors.DarkGray;  // entire canvas background
            WpfPlot4.Plot.DataBackground.Color = ScottPlot.Colors.Black;

            // Set grid line colors
            WpfPlot4.Plot.Grid.LineColor = ScottPlot.Colors.Gray;

            WpfPlot4.Plot.Axes.Bottom.TickGenerator = new NumericFixedInterval(2000000000); // 10 units
            WpfPlot4.Plot.Axes.Top.TickGenerator = new NumericFixedInterval(2000000000); // 10 units
            WpfPlot4.Plot.Axes.Left.TickGenerator = new NumericFixedInterval(20);   // 20 units
            //WpfPlot4.Plot.Axes.Bottom.

            if (thresholdLine4 != null)
            {
                WpfPlot4.Plot.Remove(thresholdLine4);
            }
            thresholdLine4 = WpfPlot4.Plot.Add.HorizontalLine(y: d1.LTH);
            thresholdLine4.LineWidth = 0.5f;
            thresholdLine4.Color = ScottPlot.Colors.Orange;

            if (thresholdLine5 != null)
            {
                WpfPlot4.Plot.Remove(thresholdLine5);
            }
            thresholdLine5 = WpfPlot4.Plot.Add.HorizontalLine(y: d1.UTH);
            thresholdLine5.LineWidth = 0.5f;
            thresholdLine5.Color = ScottPlot.Colors.Red;

            if (thresholdLine6 != null)
            {
                WpfPlot4.Plot.Remove(thresholdLine6);
            }
            thresholdLine6 = WpfPlot4.Plot.Add.HorizontalLine(y: d1.TH);
            thresholdLine6.LineWidth = 0.5f;
            thresholdLine6.Color = ScottPlot.Colors.White;

            WpfPlot4.Plot.Grid.LineWidth = 1;
            WpfPlot4.Refresh();

            InitialGraphSettingAPS();
        }
        public void InitialGraphSettingAPS()
        {
            var limits = new ScottPlot.AxisLimits(0, (DeviceCOM.Configuration.TestTime * DeviceCOM.Configuration.SamplePerSecond), 0, DeviceCOM.Factor);
            var rule = new ScottPlot.AxisRules.MinimumBoundary(
                xAxis: WpfPlotA1.Plot.Axes.Bottom,
                yAxis: WpfPlotA1.Plot.Axes.Left,
                limits: limits
            );

            WpfPlotA1.Plot.Axes.Rules.Clear();
            WpfPlotA1.Plot.Axes.Rules.Add(rule);

            var d1 = DeviceCOM.Configuration.Frequency.FD.FirstOrDefault(f => f.FN == 1);
            myPlot1 = WpfPlotA1.Plot;

            myPlot1.Title("A1 Response(" + d1.F.ToString() + "," + d1.G.ToString() + "," + d1.PP.ToString() + ")");

            //myPlot1.Grid.XAxis.IsVisible = false;
            //myPlot1.Grid.XAxis.IsVisible = false;

            myPlot1.Axes.Bottom.IsVisible = false;
            //myPlot1.Axes.Left.IsVisible = false;

            WpfPlotA1.Plot.FigureBackground.Color = ScottPlot.Colors.DarkGray;  // entire canvas background
            WpfPlotA1.Plot.DataBackground.Color = ScottPlot.Colors.Black;

            // Set grid line colors
            WpfPlotA1.Plot.Grid.LineColor = ScottPlot.Colors.Gray;
            //WpfPlot1.Plot.Grid.IsVisible = false;

            WpfPlotA1.Plot.Axes.Bottom.TickGenerator = new NumericFixedInterval(2000000000); // 10 units
            WpfPlotA1.Plot.Axes.Top.TickGenerator = new NumericFixedInterval(2000000000); // 10 units
            WpfPlotA1.Plot.Axes.Left.TickGenerator = new NumericFixedInterval(20);   // 20 units
            WpfPlotA1.Plot.Grid.LineWidth = 1;

            WpfPlotA1.Refresh();


            var limits1 = new ScottPlot.AxisLimits(0, 20, 0, DeviceCOM.Factor);
            var rule1 = new ScottPlot.AxisRules.MinimumBoundary(
                xAxis: WpfPlotA1Last.Plot.Axes.Bottom,
                yAxis: WpfPlotA1Last.Plot.Axes.Left,
                limits: limits1
            );

            WpfPlotA1Last.Plot.Axes.Rules.Clear();
            WpfPlotA1Last.Plot.Axes.Rules.Add(rule1);

            myPlot4 = WpfPlotA1Last.Plot;
            myPlot4.Title("Last A1  Response(" + d1.F.ToString() + "," + d1.G.ToString() + "," + d1.PP.ToString() + ")"); ;

            //myPlot4.Grid.XAxis.IsVisible = false;
            //myPlot4.Grid.XAxis.IsVisible = false;

            logger4 = myPlot4.Add.DataLogger();
            logger4.LineColor = ScottPlot.Colors.Blue; // Change line color here


            myPlot4.Axes.Bottom.IsVisible = false;
            //myPlot4.Axes.Left.IsVisible = false;

            WpfPlotA1Last.Plot.FigureBackground.Color = ScottPlot.Colors.DarkGray;  // entire canvas background
            WpfPlotA1Last.Plot.DataBackground.Color = ScottPlot.Colors.Black;

            // Set grid line colors
            WpfPlotA1Last.Plot.Grid.LineColor = ScottPlot.Colors.Gray;

            WpfPlotA1Last.Plot.Axes.Bottom.TickGenerator = new NumericFixedInterval(2000000000); // 10 units
            WpfPlotA1Last.Plot.Axes.Top.TickGenerator = new NumericFixedInterval(2000000000); // 10 units
            WpfPlotA1Last.Plot.Axes.Left.TickGenerator = new NumericFixedInterval(20);   // 20 units
            //WpfPlot4.Plot.Axes.Bottom.

            if (thresholdLine4 != null)
            {
                WpfPlotA1Last.Plot.Remove(thresholdLine4);
            }
            thresholdLine4 = WpfPlot4.Plot.Add.HorizontalLine(y: d1.LTH);
            thresholdLine4.LineWidth = 0.5f;
            thresholdLine4.Color = ScottPlot.Colors.Orange;

            if (thresholdLine5 != null)
            {
                WpfPlotA1Last.Plot.Remove(thresholdLine5);
            }
            thresholdLine5 = WpfPlotA1Last.Plot.Add.HorizontalLine(y: d1.UTH);
            thresholdLine5.LineWidth = 0.5f;
            thresholdLine5.Color = ScottPlot.Colors.Red;

            if (thresholdLine6 != null)
            {
                WpfPlotA1Last.Plot.Remove(thresholdLine6);
            }
            thresholdLine6 = WpfPlotA1Last.Plot.Add.HorizontalLine(y: d1.TH);
            thresholdLine6.LineWidth = 0.5f;
            thresholdLine6.Color = ScottPlot.Colors.White;

            WpfPlotA1Last.Plot.Grid.LineWidth = 1;
            WpfPlotA1Last.Refresh();
        }

        private void dispatcherTimerui_Tick(object sender, EventArgs e)
        {
            UIUpdates();
        }

        private ConcurrentQueue<byte[]> processingQueue = new();

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            if (!receiver._udpClient.Client.Connected)
            {
                receiver.StartReceiving();
            }

            if (DeviceCOM.receiveBytes != null && DeviceCOM.receiveBytes.Length > 0)
            {
                var data = DeviceCOM.receiveBytes.ToArray();
                DeviceCOM.receiveBytes = null;
                processingQueue.Enqueue(data);
                TryStartProcessing();
                //Task.Run(() => ProcessPortDataTest(data));
                //Task.Run(() => ProcessPortData(data));
            }
        }


        private void PollLoop()
        {
            while (true)
            {
                if (DeviceCOM.receiveBytes != null && DeviceCOM.receiveBytes.Length > 0)
                {
                    var data = DeviceCOM.receiveBytes.ToArray();
                    DeviceCOM.receiveBytes = null;

                    processingQueue.Enqueue(data);
                    TryStartProcessing(); // same logic as before
                }

                //Thread.Sleep(1); // adjust this depending on how fast you want to pol
            }
        }

        private void UIUpdateLoop()
        {
            while (true)
            {
                UIUpdates();

            }
        }

        private bool isProcessing = false;

        private void TryStartProcessing()
        {
            if (isProcessing || processingQueue.IsEmpty)
                return;

            isProcessing = true;

            Task.Run(() =>
            {
                while (processingQueue.TryDequeue(out var data))
                {
                    ProcessPortData(data);
                }

                isProcessing = false;
            });
        }

        private void StopTude(bool result)
        {
            try
            {
                System.Threading.Thread.Sleep(10);
                myPlot4.Clear();
                logger4 = myPlot4.Add.DataLogger();
                logger4.Clear();

                DeviceCOM.graphData.Result = result;
                var Ld = DeviceCOM.graphData.AmpD1.ToList();
                string ImageName = Guid.NewGuid().ToString() + ".jpeg";
                // Add Log
                if (DeviceCOM.IsLogEnable)
                {
                    // DeviceCOM.Configuration
                    //DeviceCOM.part
                    //DeviceCOM.graphData
                    /// result 
                    //DeviceCOM.part.Name
                    try
                    {
                        using (var con = new NpgsqlConnection(DeviceCOM.DBConnection))
                        {
                            con.Open();
                            DeviceCOM.part.ImagePath = ImageName;

                            string partJson = JsonConvert.SerializeObject(DeviceCOM.part);
                            string configJson = JsonConvert.SerializeObject(DeviceCOM.Configuration);
                            string graphJson = JsonConvert.SerializeObject(DeviceCOM.graphData);

                            string sql = @"
                                INSERT INTO ""Logs"" 
                                (""TimeStamp"", ""PartJson"", ""ConfigurationJson"", ""GraphDataJson"", ""BatchName"", ""Result"")
                                VALUES 
                                (@time, @part, @config, @graph, @batch, @result)";

                            using (var cmd = new NpgsqlCommand(sql, con))
                            {
                                cmd.Parameters.AddWithValue("@time", DateTime.Now);
                                cmd.Parameters.AddWithValue("@part", NpgsqlTypes.NpgsqlDbType.Jsonb, partJson);
                                cmd.Parameters.AddWithValue("@config", NpgsqlTypes.NpgsqlDbType.Jsonb, configJson);
                                cmd.Parameters.AddWithValue("@graph", NpgsqlTypes.NpgsqlDbType.Jsonb, graphJson);
                                cmd.Parameters.AddWithValue("@batch", DeviceCOM.part.Name ?? "");
                                cmd.Parameters.AddWithValue("@result", NpgsqlTypes.NpgsqlDbType.Boolean, result);

                                cmd.ExecuteNonQuery();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Something went wrong. Please try again");
                    }
                }

                var limits = new ScottPlot.AxisLimits(0, Ld.Count + 5, 0, DeviceCOM.Factor);
                var rule = new ScottPlot.AxisRules.MinimumBoundary(
                    xAxis: WpfPlot4.Plot.Axes.Bottom,
                    yAxis: WpfPlot4.Plot.Axes.Left,
                    limits: limits
                );

                WpfPlot4.Plot.Axes.Rules.Clear();
                WpfPlot4.Plot.Axes.Rules.Add(rule);

                var t = Ld.Count - 1;
                for (var i = 0; i <= t; i++)
                {
                    var d = Ld[i];
                    //}
                    //foreach (var d in Ld)
                    //{
                    var AmpF = 0;
                    if (d.Amp != 0)
                    {
                        AmpF = (DeviceCOM.Factor * d.Amp) / DeviceCOM.MaxValue;
                    }


                    var d1 = DeviceCOM.Configuration.Frequency.FD.FirstOrDefault(d => d.FN == 1);

                    if (thresholdLine4 != null)
                    {
                        WpfPlot4.Plot.Remove(thresholdLine4);
                    }
                    thresholdLine4 = WpfPlot4.Plot.Add.HorizontalLine(y: d1.LTH);
                    thresholdLine4.LineWidth = 0.5f;
                    thresholdLine4.Color = ScottPlot.Colors.Orange;

                    if (thresholdLine5 != null)
                    {
                        WpfPlot4.Plot.Remove(thresholdLine5);
                    }
                    thresholdLine5 = WpfPlot4.Plot.Add.HorizontalLine(y: d1.UTH);
                    thresholdLine5.LineWidth = 0.5f;
                    thresholdLine5.Color = ScottPlot.Colors.Red;

                    if (thresholdLine6 != null)
                    {
                        WpfPlot4.Plot.Remove(thresholdLine6);
                    }
                    thresholdLine6 = WpfPlot4.Plot.Add.HorizontalLine(y: d1.TH);
                    thresholdLine6.LineWidth = 0.5f;
                    thresholdLine6.Color = ScottPlot.Colors.White;

                    logger4.Add(AmpF);

                    //if (d.IsMarked)
                    //{
                    //    int index = Ld.IndexOf(d);
                    //    var thresholdLine10 = WpfPlot4.Plot.Add.VerticalLine(x: index);
                    //    thresholdLine10.LineWidth = 1.5f;
                    //    thresholdLine10.Color = ScottPlot.Colors.Red;
                    //}
                }


                var d2 = Ld.FirstOrDefault(d => d.IsMarked);
                if (d2 != null)
                {
                    int index = Ld.IndexOf(d2);

                    var thresholdLine10 = WpfPlot4.Plot.Add.VerticalLine(x: index);
                    thresholdLine10.LineWidth = 1.5f;
                    thresholdLine10.Color = ScottPlot.Colors.Red;
                }

                var d3 = Ld.LastOrDefault(d => d.IsMarked);
                if (d3 != null)
                {
                    int index = Ld.IndexOf(d3);
                    var thresholdLine10 = WpfPlot4.Plot.Add.VerticalLine(x: index);
                    thresholdLine10.LineWidth = 1.5f;
                    thresholdLine10.Color = ScottPlot.Colors.Red;
                }

                WpfPlot4.Refresh();

                if (result)
                {
                    DeviceCOM.Ok = DeviceCOM.Ok + 1;
                }
                else
                {
                    DeviceCOM.NoOk = DeviceCOM.NoOk + 1;
                }

                // Write counter file

                wpCounter.Plot.Clear();

                List<PieSlice> pieSlices = new List<PieSlice>();

                PieSlice pieSliceG = new PieSlice(DeviceCOM.Ok, ScottPlot.Colors.Green, "Ok");
                pieSlices.Add(pieSliceG);

                PieSlice pieSliceN = new PieSlice(DeviceCOM.NoOk, ScottPlot.Colors.Red, "Not Ok");
                pieSlices.Add(pieSliceN);

                var pie = wpCounter.Plot.Add.Pie(pieSlices);

                wpCounter.Plot.Axes.Bottom.IsVisible = false;
                wpCounter.Plot.Axes.Left.IsVisible = false;

                //wpCounter.Plot.Title("Counter Distribution");
                wpCounter.Refresh();

                //lblOk.Content = "Ok Count-" + DeviceCOM.Ok.ToString();
                //lblNotOk.Content = "Not Ok Count-" + DeviceCOM.NoOk.ToString();
                //lblTotal.Content = "Total Count-" + (DeviceCOM.Ok + DeviceCOM.NoOk).ToString();

                DeviceCOM.graphData.AmpD1 = new List<Fdata>();

                string imagePath = ConfigurationManager.AppSettings["ImagePath"];

                if (DeviceCOM.IsLogEnable && !string.IsNullOrWhiteSpace(imagePath))
                {
                    try
                    {
                        string fullPath = System.IO.Path.Combine(imagePath, ImageName);

                        // Ensure directory exists
                        Directory.CreateDirectory(imagePath);

                        WpfPlot4.Plot.SaveJpeg(fullPath, 600, 400);
                    }
                    catch (Exception ex)
                    {
                        // Log properly instead of silent failure
                        // Example: Logger.LogError(ex);
                    }
                }

            }
            catch (Exception e)
            {

            }
        }

        private void UIUpdates()
        {
            try
            {

                lblResultStatus.Content = resultStatus;
                Canvas2.Children.Clear();


                if (DeviceCOM.Configuration != null)
                {
                    var d1 = DeviceCOM.Configuration.Frequency.FD.FirstOrDefault(f => f.FN == 1);

                    var elW = (470 * d1.LTH) / DeviceCOM.Factor;
                    Ellipse el1 = new Ellipse();
                    el1.Height = elW;
                    el1.Width = elW;
                    el1.Stroke = new SolidColorBrush(Colors.Orange);
                    el1.StrokeThickness = 1; // You can adjust this as needed
                    Canvas.SetLeft(el1, (-1 * (elW / 2)));
                    Canvas.SetTop(el1, (-1 * (elW / 2)));

                    Canvas2.Children.Add(el1);

                    var elW1 = (470 * d1.UTH) / DeviceCOM.Factor;
                    Ellipse el2 = new Ellipse();
                    el2.Height = elW1;
                    el2.Width = elW1;
                    el2.Stroke = new SolidColorBrush(Colors.Red);
                    el2.StrokeThickness = 1; // You can adjust this as needed
                    Canvas.SetLeft(el2, -1 * (elW1 / 2));
                    Canvas.SetTop(el2, -1 * (elW1 / 2));

                    Canvas2.Children.Add(el2);

                }

                var data = DeviceCOM.graphData.AmpD1.ToList();
                if (data.Count > 0)
                {
                    var dotsVisual = CreateDotsVisual(data);
                    var host = new VisualHost(dotsVisual);
                    Canvas2.Children.Add(host);
                }

                if (DeviceCOM.IsTubeSatart)
                {
                    btnTestStatus.Background = new SolidColorBrush(Colors.Green);
                    lblTest.Content = "Test On";
                }
                else
                {
                    btnTestStatus.Background = new SolidColorBrush(Colors.Gray);
                    lblTest.Content = "Test Off";
                }


                if (DeviceCOM.IsCalibarationStart)
                {
                    btnCali.Background = new SolidColorBrush(Colors.Orange);
                }
                else
                {
                    btnCali.Background = new SolidColorBrush(Colors.Gray);
                }

                var tContent = "Total Count-" + (DeviceCOM.Ok + DeviceCOM.NoOk).ToString();
                if (tContent != lblTotal.Content.ToString())
                {
                    lblOk.Content = "Ok Count-" + DeviceCOM.Ok.ToString();
                    lblNotOk.Content = "Not Ok Count-" + DeviceCOM.NoOk.ToString();
                    lblTotal.Content = "Total Count-" + (DeviceCOM.Ok + DeviceCOM.NoOk).ToString();
                }
            }
            catch (Exception e)
            {

            }
            //InitialGraphSetting();
        }


        private DrawingVisual CreateDotsVisual(IEnumerable<Fdata> data)
        {
            DrawingVisual visual = new DrawingVisual();
            using (DrawingContext dc = visual.RenderOpen())
            {
                foreach (var d in data)
                {
                    double phaseRadians = d.phase * Math.PI / 180.0;
                    double AmpF1 = (235 * d.Amp) / DeviceCOM.MaxValue;
                    double x1 = AmpF1 * Math.Cos(phaseRadians);
                    double y1 = AmpF1 * Math.Sin(phaseRadians);

                    // Draw a small blue dot (1.5 radius circle)
                    dc.DrawEllipse(Brushes.Blue, null, new Point(x1, y1), 1.5, 1.5);
                }
            }
            return visual;
        }

        private void ProcessPortDataTest(byte[] indata)
        {
            try
            {
                List<string> strings = new List<string>();
                strings.Add(indata[3].ToString() + "-" + indata[5].ToString() + "-" + indata.Length);
                File.AppendAllLines("DataLog.txt", strings);
            }
            catch (Exception ex)
            { }
        }
        private void ProcessPortData(byte[] indata)
        {
            try
            {
                //List<string> strings = new List<string>();
                //strings.Add(indata[3].ToString() + "-" + indata[5].ToString() + "-" + indata.Length);
                //File.AppendAllLines("DataLog.txt", strings);

                //if (indata.Length == 2)
                //{
                if (indata[0] == 52 || indata[0] == 53 || indata[0] == 54 || indata[0] == 56 || indata[0] == 61)
                {
                    // 5 ==> Start Test // 56 Stop Test  ==> Busy/Fee
                    if (indata[0] == 61)
                    {
                        resultStatus = string.Empty;
                        if (indata[1] == 1)
                        {
                            DeviceCOM.IsCalibarationStart = true;
                            //btnCali.Background = new SolidColorBrush(Colors.Orange);                            
                        }
                        else if (indata[1] == 2)
                        {
                            DeviceCOM.IsCalibarationStart = false;
                            //btnCali.Background = new SolidColorBrush(Colors.Gray);
                        }
                    }

                    // 53 ==> Start Test // 56 Stop Test  ==> Busy/Fee
                    if (indata[0] == 53)
                    {
                        //D1Seeting();
                        DeviceCOM.IsTestOn = true;
                        resultStatus = string.Empty;
                        DeviceCOM.graphData.AmpD1 = new List<Fdata>();
                    }
                    else if (indata[0] == 56)
                    {
                        DeviceCOM.IsTestOn = false;
                        DeviceCOM.IsTubeSatart = false;
                        resultStatus = string.Empty;
                        DeviceCOM.graphData.AmpD1 = new List<Fdata>();
                    }

                    //if (indata[0] == 52 || indata[0] == 54)
                    //{
                    //    //myPlot1.Clear();
                    //    //logger1 = myPlot1.Add.DataLogger();
                    //    //logger1.Clear();

                    //    if (indata[0] == 54)
                    //    {
                    //        DeviceCOM.IsTubeSatart = true;
                    //        //btnTestStatus.Background = new SolidColorBrush(Colors.Orange);
                    //        DeviceCOM.graphData.AmpD1 = new List<Fdata>();
                    //        D1Seeting();
                    //    }

                    //    if (indata[0] == 52)
                    //    {
                    //        DeviceCOM.IsTubeSatart = false;
                    //        //btnTestStatus.Background = new SolidColorBrush(Colors.Gray);
                    //        StopTude(indata[1] == 0);
                    //        //dispatcherTimerui.Stop();

                    //    }
                    //}
                }
                //}
                else if (indata[0] == 55)
                {
                    int NoOfSamples = (indata[2] * 256) + indata[1];
                    int startIndex = indata[3];
                    int errCode = 0;//indata[6];
                    if (errCode == 0)
                    {
                        if (startIndex == 4)
                        {
                            DeviceCOM.IsTubeSatart = true;
                            //btnTestStatus.Background = new SolidColorBrush(Colors.Orange);
                            resultStatus = string.Empty;
                            DeviceCOM.graphData.AmpD1 = new List<Fdata>();
                        }


                        // C1 AMP Data
                        int Ch1NoIndex = 14;
                        int FN1 = indata[Ch1NoIndex];
                        int C1length = indata[15] + (indata[16] * 256);

                        int fStartIndex1 = 17;
                        int fEndIndex1 = fStartIndex1 + C1length - 1;

                        var C1ArrayCompress = new byte[C1length];

                        for (int i = 0; i < C1length; i++)
                        {
                            C1ArrayCompress[i] = indata[fStartIndex1 + i];
                        }

                        // C1 Phase data  
                        int C2length = indata[fEndIndex1 + 1] + (indata[fEndIndex1 + 2] * 256);
                        int fStartIndex2 = fEndIndex1 + 3;
                        int fEndIndex2 = fStartIndex2 + C2length - 1;

                        var C2ArrayCompress = new byte[C2length];

                        for (int i = 0; i < C2length; i++)
                        {
                            C2ArrayCompress[i] = indata[fStartIndex2 + i];
                        }

                        Int32 indexTudeState = ((indata[10] + (indata[11] << 8) + (indata[12] << 16) + (indata[13] << 24)) * 2);
                        Int32 indexPrePostState = ((indata[4] + (indata[5] << 8) + (indata[6] << 16) + (indata[7] << 24)) * 2);
                        var d1 = DeviceCOM.Configuration.Frequency.FD.FirstOrDefault(f => f.FN == 1);
                        for (int i = 0; i < C1ArrayCompress.Length; i = i + 2)
                        {
                            bool IsMark = false;
                            Int32 amp = C1ArrayCompress[i] + (C1ArrayCompress[i + 1] << 8);
                            Int32 phase = C2ArrayCompress[i] + (C2ArrayCompress[i + 1] << 8);
                            int rPhase = ((phase + d1.PP) > 360 ? (phase + d1.PP) - 360 : (phase + d1.PP));
                            double phaseRadians = (rPhase) * Math.PI / 180.0;

                            // Calculate Cartesian coordinates
                            double x = (amp * Math.Cos(phaseRadians));
                            double y = amp * Math.Sin(phaseRadians);

                            if (DeviceCOM.IsTubeSatart == true)
                            {

                                if (indata[9] == 1)
                                {
                                    if (i >= indexTudeState)
                                    {
                                        IsMark = true;
                                    }
                                }
                                else if (indata[9] == 3)
                                {
                                    if (i <= indexTudeState)
                                    {
                                        IsMark = true;
                                    }
                                }


                                if (startIndex == 4)
                                {
                                    if (i >= indexPrePostState)
                                    {
                                        DeviceCOM.graphData.AmpD1.Add(new Fdata() { Amp = amp, phase = rPhase, x = x, y = y, IsMarked = IsMark });
                                    }
                                }
                                else if (startIndex == 6)
                                {
                                    if (i <= indexPrePostState)
                                    {
                                        DeviceCOM.graphData.AmpD1.Add(new Fdata() { Amp = amp, phase = phase, x = x, y = y, IsMarked = IsMark });
                                    }
                                }
                                else if (startIndex == 2)
                                {
                                    DeviceCOM.graphData.AmpD1.Add(new Fdata() { Amp = amp, phase = phase, x = x, y = y, IsMarked = IsMark });
                                }
                            }

                            var AmpF = 0;
                            if (amp != 0)
                            {
                                AmpF = ((DeviceCOM.Factor * amp) / DeviceCOM.MaxValue);
                            }

                            logger1.Add(AmpF);

                            WpfPlot1.Refresh();
                        }


                        // C3 AMP Data
                        //int Ch3NoIndex = fEndIndex2 + 1;
                        //int FN3 = indata[Ch3NoIndex];
                        //int C3length = indata[Ch3NoIndex + 1] + (indata[Ch3NoIndex + 2] * 256);

                        //int fStartIndex3 = Ch3NoIndex + 3;
                        //int fEndIndex3 = fStartIndex3 + C3length - 1;

                        //var C3ArrayCompress = new byte[C3length];

                        //for (int i = 0; i < C3length; i++)
                        //{
                        //    C3ArrayCompress[i] = indata[fStartIndex3 + i];
                        //}

                        //for (int i = 0; i < C3ArrayCompress.Length; i = i + 2)
                        //{
                        //    Int32 amp = C3ArrayCompress[i] + (C3ArrayCompress[i + 1] << 8);                        
                        //}

                        if (startIndex == 6)
                        {
                            DeviceCOM.IsTubeSatart = false;
                            //btnTestStatus.Background = new SolidColorBrush(Colors.Gray);
                            if (indata[8] == 0 || indata[8] == 1)
                            {
                                resultStatus = string.Empty;
                                StopTude(indata[8] == 0);
                            }
                            else
                            {
                                resultStatus = "Invalid Test!!";
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show("Something went wrong, kindly restart the full system to resolve the issue!!", "Information");
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }
        HorizontalLine thresholdLine1;
        HorizontalLine thresholdLine2;
        HorizontalLine thresholdLine3;
        public void D1Seeting()
        {
            //WpfPlot1.Plot.Clear();
            var limits = new ScottPlot.AxisLimits(0, (DeviceCOM.Configuration.TestTime * DeviceCOM.Configuration.SamplePerSecond), 0, DeviceCOM.Factor);
            var rule = new ScottPlot.AxisRules.MinimumBoundary(
                xAxis: WpfPlot1.Plot.Axes.Bottom,
                yAxis: WpfPlot1.Plot.Axes.Left,
                limits: limits
            );

            WpfPlot1.Plot.Axes.Rules.Clear();
            WpfPlot1.Plot.Axes.Rules.Add(rule);

            var d1 = DeviceCOM.Configuration.Frequency.FD.FirstOrDefault(d => d.FN == 1);

            if (thresholdLine1 != null)
            {
                WpfPlot1.Plot.Remove(thresholdLine1);
            }
            thresholdLine1 = WpfPlot1.Plot.Add.HorizontalLine(y: d1.LTH);
            thresholdLine1.LineWidth = 0.5f;
            thresholdLine1.Color = ScottPlot.Colors.Orange;

            if (thresholdLine2 != null)
            {
                WpfPlot1.Plot.Remove(thresholdLine2);
            }
            thresholdLine2 = WpfPlot1.Plot.Add.HorizontalLine(y: d1.UTH);
            thresholdLine2.LineWidth = 0.5f;
            thresholdLine2.Color = ScottPlot.Colors.Red;

            if (thresholdLine3 != null)
            {
                WpfPlot1.Plot.Remove(thresholdLine3);
            }
            thresholdLine3 = WpfPlot1.Plot.Add.HorizontalLine(y: d1.TH);
            thresholdLine3.LineWidth = 0.5f;
            thresholdLine3.Color = ScottPlot.Colors.White;

            D1SeetingAPS();
        }


        public void D1SeetingAPS()
        {
            //WpfPlot1.Plot.Clear();
            var limits = new ScottPlot.AxisLimits(0, (DeviceCOM.Configuration.TestTime * DeviceCOM.Configuration.SamplePerSecond), 0, DeviceCOM.Factor);
            var rule = new ScottPlot.AxisRules.MinimumBoundary(
                xAxis: WpfPlotA1.Plot.Axes.Bottom,
                yAxis: WpfPlotA1.Plot.Axes.Left,
                limits: limits
            );

            WpfPlotA1.Plot.Axes.Rules.Clear();
            WpfPlotA1.Plot.Axes.Rules.Add(rule);

            var d1 = DeviceCOM.Configuration.Frequency.FD.FirstOrDefault(d => d.FN == 1);

            if (thresholdLine1 != null)
            {
                WpfPlotA1.Plot.Remove(thresholdLine1);
            }
            thresholdLine1 = WpfPlotA1.Plot.Add.HorizontalLine(y: d1.LTH);
            thresholdLine1.LineWidth = 0.5f;
            thresholdLine1.Color = ScottPlot.Colors.Orange;

            if (thresholdLine2 != null)
            {
                WpfPlotA1.Plot.Remove(thresholdLine2);
            }
            thresholdLine2 = WpfPlotA1.Plot.Add.HorizontalLine(y: d1.UTH);
            thresholdLine2.LineWidth = 0.5f;
            thresholdLine2.Color = ScottPlot.Colors.Red;

            if (thresholdLine3 != null)
            {
                WpfPlotA1.Plot.Remove(thresholdLine3);
            }
            thresholdLine3 = WpfPlotA1.Plot.Add.HorizontalLine(y: d1.TH);
            thresholdLine3.LineWidth = 0.5f;
            thresholdLine3.Color = ScottPlot.Colors.White;
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

        private void btnClear_MouseDown(object sender, MouseButtonEventArgs e)
        {
            DeviceCOM.Ok = 0;
            DeviceCOM.NoOk = 0;

            lblOk.Content = "Ok Count-" + DeviceCOM.Ok.ToString();
            lblNotOk.Content = "Not Ok Count-" + DeviceCOM.NoOk.ToString();
            lblTotal.Content = "Total Count-" + (DeviceCOM.Ok + DeviceCOM.NoOk).ToString();

            wpCounter.Plot.Clear();

            wpCounter.Refresh();

            // Delete counter file 

        }

        private void btnLog_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (DeviceCOM.IsLogEnable)
            {
                DeviceCOM.IsLogEnable = false;
                lblLog.Content = "Start Log";
            }
            else
            {
                partConfig = new PartConfig();
                partConfig.Closing += partConfig_Closing;
                partConfig.Owner = this;
                partConfig.ShowDialog();

            }
        }

        private void partConfig_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (DeviceCOM.IsLogEnable)
            {
                lblLog.Content = "Stop Log";
                //lblPartLogs.Content = DeviceCOM.part.BatchName + " => " + DeviceCOM.part.Name;
            }
            else
            {
                //lblPartLogs.Content = "";
            }
        }

        private void ddlTT_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var text = e.AddedItems[0].ToString();
            DeviceCOM.Configuration.TestTime = Convert.ToInt32(text);

            D1Seeting();

            System.IO.File.WriteAllText("Config.txt", JsonConvert.SerializeObject(DeviceCOM.Configuration));
        }

        private void btnCali_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (DeviceCOM.IsTubeSatart || DeviceCOM.IsCalibarationStart)
            {
                MessageBox.Show("The tube/calibration is in progress, no calibration are allowed!", "Information");
            }
            else
            {
                Status status = new Status() { FC = 61 };
                deviceCOM.WriteData(JsonConvert.SerializeObject(status));
            }
        }
    }


}