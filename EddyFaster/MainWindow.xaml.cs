using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO.Ports;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using File = System.IO.File;

namespace _8F
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ObservableCollection<MenuItemViewModel> MenuItems { get; set; }
        public CircleSetting ellipsesPop { get; set; }
        public PartConfig partConfig { get; set; }
        public DeviceCOM portCOM;
        public Report report;
        DispatcherTimer dispatcherTimer;
        public int chNo = 1;
        double factor = 20;

        
        int seqLength = 720;
        int CommunicationType = 0;
        int FrequencyNo = 1;

        int modeApp = 1;
        int mode = 1;
        //bool IsBalanceAll = false;
        public SolidColorBrush disableColor = new SolidColorBrush(Colors.DarkGray);
        public SolidColorBrush enableColor = new SolidColorBrush(Colors.White);
        bool IsSerialmatch = true;
        private SerialPort _serialPort;

        DateTime CodeReadTime = DateTime.Now;

        UdpReceiver receiver;
        string IpAddress;
        int Port;
        public MainWindow()
        {

            InitializeComponent();
            
           
            DeviceCOM.cordinateQueue = new List<CordinateQueue>();

            var LogEnabled = Convert.ToBoolean(System.Configuration.ConfigurationSettings.AppSettings["LogEnable"]);           
            
            DeviceCOM.Mode = mode;

            if (!LogEnabled)
            {
                btnLog2.Visibility = Visibility.Hidden;               
            }                              

            portCOM = new DeviceCOM();

            factor = Convert.ToDouble(System.Configuration.ConfigurationSettings.AppSettings["Factor"]);
            DeviceCOM.DefaultWidth = Convert.ToInt16(System.Configuration.ConfigurationSettings.AppSettings["Width"]);
            DeviceCOM.DefaultHeight = Convert.ToInt16(System.Configuration.ConfigurationSettings.AppSettings["Height"]);
            DeviceCOM.DefaultWidth_O = Convert.ToInt16(System.Configuration.ConfigurationSettings.AppSettings["Width_O"]);
            DeviceCOM.DefaultHeight_O = Convert.ToInt16(System.Configuration.ConfigurationSettings.AppSettings["Height_O"]);
            DeviceCOM.DefaultAngel_O = Convert.ToInt16(System.Configuration.ConfigurationSettings.AppSettings["Angel_O"]);

            if (modeApp == 1)
            {
                el11.Visibility = Visibility.Visible;
            }
            else
            {
                el11.Visibility = Visibility.Hidden;
            }

            int baudRate = Convert.ToInt32(System.Configuration.ConfigurationSettings.AppSettings["BaudRate"]);
            string portName = Convert.ToString(System.Configuration.ConfigurationSettings.AppSettings["PortName"]);

            string IpAddress = Convert.ToString(System.Configuration.ConfigurationSettings.AppSettings["IP"]);
            int Port = Convert.ToInt32(System.Configuration.ConfigurationSettings.AppSettings["Port"]);

            DeviceCOM.ConnectionString = System.Configuration.ConfigurationSettings.AppSettings["ConnectionString"];

            portCOM.InitialPort(CommunicationType, portName, baudRate, IpAddress, Port);

            DeviceCOM.responses = new List<Response>();
            
            DeviceCOM.ChannelNo = chNo;
            

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
                    MenuItems = LogEnabled ? new ObservableCollection<MenuItemViewModel>
                        {
                            new MenuItemViewModel { Header = "Change Configuration", mainWindow = this },
                            new MenuItemViewModel { Header = "Threshold Setting", mainWindow = this },
                            new MenuItemViewModel { Header = "Write Configuration", mainWindow = this },
                            //new MenuItemViewModel { Header = "Copy Channel-1 Configuration", mainWindow = this },
                            //new MenuItemViewModel { Header = "Data Log", mainWindow = this }
                        } :
                        new ObservableCollection<MenuItemViewModel>
                        {
                            new MenuItemViewModel { Header = "Change Configuration", mainWindow = this },
                            new MenuItemViewModel { Header = "Threshold Setting", mainWindow = this },
                            new MenuItemViewModel { Header = "Write Configuration", mainWindow = this },
                            //new MenuItemViewModel { Header = "Copy Channel-1 Configuration", mainWindow = this }
                        }
                },
                new MenuItemViewModel { Header = "View Log",
                        MenuItems = new ObservableCollection<MenuItemViewModel>
                        {
                            new MenuItemViewModel { Header = "Batch Wise Log", mainWindow =this },
                            //new MenuItemViewModel { Header = "Serial Number Log" ,mainWindow =this },
                        }
                },
            };
            DataContext = this;

            InitialGraphData(true);

            
            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = TimeSpan.FromMilliseconds(50);
            dispatcherTimer.Start();

            Status status = new Status() { FC = 23 };
            var rat = portCOM.GetSystemStatus(JsonConvert.SerializeObject(status));

            if (DeviceCOM.IsSystemBusy || !rat)
            {
                ImplementChanges(1);
            }
            else
            {
                var ratval = ImplementChanges(0);
            }

            IpAddress = Convert.ToString(System.Configuration.ConfigurationSettings.AppSettings["IP"]);
            Port = Convert.ToInt32(System.Configuration.ConfigurationSettings.AppSettings["Port"]);

            receiver = new UdpReceiver(Port);
            receiver.StartReceiving();


            Task.Run(() => PollLoop());

            var IsManualTest = Convert.ToBoolean(System.Configuration.ConfigurationSettings.AppSettings["IsManualTest"]);
            if (IsManualTest)
            {
                btnTest2.Visibility = Visibility.Visible;
                btnStop.Visibility = Visibility.Visible;
            }
            else
            {
                btnTest2.Visibility = Visibility.Hidden;
                btnStop.Visibility = Visibility.Hidden;
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

        private bool isProcessing = false;
        private ConcurrentQueue<byte[]> processingQueue = new();
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
        short lastX = 0;
        short lastY = 0;

        private void ProcessPortData(byte[] indata)
        {
            try
            {
                if (indata == null || indata.Length < 3)
                    return;

                List<Cordinate> cordinates = new List<Cordinate>();

                // Sample count (little-endian)
                int noOfSample = BitConverter.ToUInt16(indata, 1);

                int offset = 3;

                for (int i = 0; i < noOfSample; i++)
                {
                    // Validate packet length
                    if (offset + 4 > indata.Length)
                        break;

                    short x = BitConverter.ToInt16(indata, offset);
                    short y = BitConverter.ToInt16(indata, offset + 2);

                    offset += 4;

                    // Add only if value changed
                    if (lastX != x || lastY != y)
                    {
                        lastX = x;
                        lastY = y;

                        cordinates.Add(new Cordinate()
                        {
                            X = x,
                            Y = y
                        });
                    }
                }

                // Do not add empty batches
                if (cordinates.Count == 0)
                    return;

                DeviceCOM.cordinateQueue.Add(
                    new CordinateQueue() { cordinates = cordinates }
                );

                // Keep only last 10 batches
                if (DeviceCOM.cordinateQueue.Count > 10)
                {
                    DeviceCOM.cordinateQueue.RemoveRange(
                        0,
                        DeviceCOM.cordinateQueue.Count - 10
                    );
                }

                DeviceCOM.IsResponseRefreshRequired = true;
            }
            catch (Exception ex)
            {
                // Optional: log error
                // Logger.Log(ex);
            }
        }

        public string Reverse(string Input)
        {

            // Converting string to character array 
            char[] charArray = Input.ToCharArray();

            // Declaring an empty string
            string reversedString = String.Empty;

            int length, index;
            length = charArray.Length - 1;
            index = length;

            // Iterating the each character from right to left  
            while (index > -1)
            {

                // Appending character to the reversedstring.
                reversedString = reversedString + charArray[index];
                index--;
            }

            // Return the reversed string.
            return reversedString;
        }

        private void CheckSerailNumber()
        {
            // Get Serial Numner 
            var serial = portCOM.GetSeialNumber();
            string sNumber = System.Configuration.ConfigurationSettings.AppSettings["SerialNumber"]; ;

            sNumber = Reverse(serial.S1 + sNumber + serial.S2);

            if (sNumber == serial.S)
            {
                IsSerialmatch = true;
            }
            if (!IsSerialmatch)
            {
                MessageBox.Show("Serial number is mistmatch!", "System Information");
                this.Close();
            }
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            //if (!IsSerialmatch)
            //{
            //    CheckSerailNumber();
            //}

            if (DeviceCOM.IsSystemBusy)
            {
                brStatus.Background = new SolidColorBrush(Colors.Red);
                if (mode == 0)
                {
                    if (DeviceCOM.busyStamp.AddSeconds(30) < System.DateTime.Now)
                    {
                        DeviceCOM.IsSystemBusy = false;                        
                    }
                }
            }
            else
            {
                brStatus.Background = new SolidColorBrush(Colors.Green);
            }

            if (DeviceCOM.IsResponseRefreshRequired)
            {
                RefreshResponse();

                var SChId = DeviceCOM.channelDatas.FirstOrDefault(c => c.IsSeleted)?.Id;

                var cnt = DeviceCOM.counter.FirstOrDefault(c => c.Id == SChId);

                lblTCount2.Content = "Total Count - " + cnt.ResultCount.ToString();
                lblOkCount2.Content = "OK Count - " + cnt.ResultOkCount.ToString();
                lblNotOkCount2.Content = "Not Ok Count - " + cnt.ResultOkNotCount.ToString();

                DeviceCOM.IsResponseRefreshRequired = false;

            }

            if (DeviceCOM.IsResponseClearRequired)
            {
                
                var SChId = DeviceCOM.channelDatas.FirstOrDefault(c => c.IsSeleted)?.Id;
                if (DeviceCOM.IsBalanceAll)
                {
                    ClearGraphData();
                }
                else
                {
                    ClearGraphDataByChId(Convert.ToInt32(SChId));
                }

                foreach (var ch in DeviceCOM.channelDatas)
                {
                    if (DeviceCOM.IsBalanceAll || ch.IsSeleted)
                    {
                        var rData = "{\"FC\":20,\"CN\":1,\"OR\":0,\"FD\":[{\"FN\":1,\"R\":0,\"X\":0,\"Y\":0},{\"FN\":2,\"R\":0,\"X\":0,\"Y\":0},{\"FN\":3,\"R\":0,\"X\":0,\"Y\":0},{\"FN\":4,\"R\":0,\"X\":0,\"Y\":0},{\"FN\":5,\"R\":0,\"X\":0,\"Y\":0},{\"FN\":6,\"R\":0,\"X\":0,\"Y\":0},{\"FN\":7,\"R\":0,\"X\":0,\"Y\":0},{\"FN\":8,\"R\":0,\"X\":0,\"Y\":0}]}";
                        var res = JsonConvert.DeserializeObject<Response>(rData);
                        res.CN = ch.Id;
                        res.IsBalacenced = true;
                        DeviceCOM.responses.Add(res);
                    }
                }

                DeviceCOM.IsResponseRefreshRequired = true;
                DeviceCOM.IsResponseClearRequired = false;

            }

            if (DeviceCOM.ERRCode == 16)
            {
                DeviceCOM.ERRCode = 0;
                MessageBox.Show("Balance Operation failed, please reboot the board.", "Error Information");
            }
            else if (DeviceCOM.ERRCode == 17)
            {
                DeviceCOM.ERRCode = 0;
                MessageBox.Show("Test failed, please reconfigure and rebalance the board.", "Error Information");
            }
        }

        public void InitialGraphData(bool IsPayLaod)
        {
            if (IsPayLaod)
            {
                ClearGraphData();

                br1_rec1.Height = seqLength;
                br1_rec1.Width = seqLength;
                Canvas.SetLeft(br1_can1, seqLength);
                br1_rec2.Width = seqLength;
                Canvas.SetTop(br1_rec2, seqLength / 2);
                br1_rec3.Height = seqLength;
                Canvas.SetLeft(br1_rec3, seqLength / 2);
                Canvas.SetLeft(rResult1, seqLength - 25);
                Canvas.SetTop(rResult1, seqLength - 25);
                Canvas.SetLeft(cnBr1, seqLength / 2);
                Canvas.SetTop(cnBr1, seqLength / 2);
                Canvas.SetTop(D1, seqLength - 25);

                for (int i = 10; i < seqLength; i = i + 10)
                {
                    Rectangle r1 = new Rectangle();
                    r1.Height = .2;
                    r1.Width = seqLength;
                    Canvas.SetLeft(r1, 0);
                    Canvas.SetTop(r1, i);
                    r1.Stroke = new SolidColorBrush(Colors.Black);
                    r1.Fill = new SolidColorBrush(Colors.LightGray);
                    Canvas1.Children.Add(r1);

                    Rectangle rectangle1 = new Rectangle();
                    rectangle1.Height = seqLength;
                    rectangle1.Width = .1;
                    Canvas.SetLeft(rectangle1, i);
                    Canvas.SetTop(rectangle1, 0);
                    rectangle1.Stroke = new SolidColorBrush(Colors.Black);
                    rectangle1.Fill = new SolidColorBrush(Colors.LightGray);


                    Rectangle rr1 = new Rectangle();
                    rr1.Height = seqLength;
                    rr1.Width = .2;
                    Canvas.SetLeft(rr1, i);
                    Canvas.SetTop(rr1, 0);
                    rr1.Stroke = new SolidColorBrush(Colors.Black);
                    rr1.Fill = new SolidColorBrush(Colors.LightGray);
                    Canvas1.Children.Add(rr1);

                }
            }
            DeviceCOM.channelDatas = new List<ChannelData>();

            ChannelData channelData = new ChannelData();
            channelData.Id = 1;
            channelData.IsSeleted = true;
            channelData.graphDatas = IniGdata();
            DeviceCOM.channelDatas.Add(channelData);

            ChannelData channelData1 = new ChannelData();
            channelData1.Id = 2;
            channelData1.graphDatas = IniGdata();
            DeviceCOM.channelDatas.Add(channelData1);

            ChannelData channelData2 = new ChannelData();
            channelData2.Id = 3;
            channelData2.graphDatas = IniGdata();
            DeviceCOM.channelDatas.Add(channelData2);

            ChannelData channelData3 = new ChannelData();
            channelData3.Id = 4;
            channelData3.graphDatas = IniGdata();
            DeviceCOM.channelDatas.Add(channelData3);           
        }

        public List<GraphData> IniGdata()
        {
            List<GraphData> graphDatas = new List<GraphData>();

            GraphData graphD1 = new GraphData();
            graphD1.Id = 1;
            graphD1.Name = "D1";
            Ellips elliplse1 = new Ellips();
            elliplse1.Id = 1;
            graphD1.ellipses.Add(elliplse1);
            graphDatas.Add(graphD1);

            GraphData graphD2 = new GraphData();
            graphD2.Id = 2;
            graphD2.Name = "D2";
            Ellips elliplse2 = new Ellips();
            elliplse2.Id = 1;
            graphD2.ellipses.Add(elliplse2);
            graphDatas.Add(graphD2);

            GraphData graphD3 = new GraphData();
            graphD3.Id = 3;
            graphD3.Name = "D3";
            Ellips elliplse3 = new Ellips();
            elliplse3.Id = 1;
            graphD3.ellipses.Add(elliplse3);
            graphDatas.Add(graphD3);

            GraphData graphD4 = new GraphData();
            graphD4.Id = 4;
            graphD4.Name = "D4";
            Ellips elliplse4 = new Ellips();
            elliplse4.Id = 1;
            graphD4.ellipses.Add(elliplse4);
            graphDatas.Add(graphD4);

            GraphData graphD5 = new GraphData();
            graphD5.Id = 5;
            graphD5.Name = "D5";
            Ellips elliplse5 = new Ellips();
            elliplse5.Id = 1;
            graphD5.ellipses.Add(elliplse5);
            graphDatas.Add(graphD5);

            GraphData graphD6 = new GraphData();
            graphD6.Id = 6;
            graphD6.Name = "D6";
            Ellips elliplse6 = new Ellips();
            elliplse6.Id = 1;
            graphD6.ellipses.Add(elliplse6);
            graphDatas.Add(graphD6);

            GraphData graphD7 = new GraphData();
            graphD7.Id = 7;
            graphD7.Name = "D7";
            Ellips elliplse7 = new Ellips();
            elliplse7.Id = 1;
            graphD7.ellipses.Add(elliplse7);
            graphDatas.Add(graphD7);

            GraphData graphD8 = new GraphData();
            graphD8.Id = 8;
            graphD8.Name = "D8";
            Ellips elliplse8 = new Ellips();
            elliplse8.Id = 1;
            graphD8.ellipses.Add(elliplse8);
            graphDatas.Add(graphD8);

            return graphDatas;
        }

        public bool ImplementChanges(int ChangeType)
        {
            var rat = false;
            if (ChangeType == 0)
            {
                FrequencyCount frequencyCount = new FrequencyCount() { FC = 1, C = FrequencyNo, NC = chNo };
                portCOM.WriteData(JsonConvert.SerializeObject(frequencyCount));                
            }

            foreach (var ch in DeviceCOM.channelDatas)
            {
                if (ch.Id <= chNo)
                {
                    FrequencyWrite frequencyWrite = new FrequencyWrite();
                    frequencyWrite.FC = 4;
                    frequencyWrite.CN = ch.Id;
                    frequencyWrite.FD = new List<Frequency>();

                    ElliplseWrite ellipseWrite = new ElliplseWrite();
                    ellipseWrite.FC = 5;
                    ellipseWrite.CN = ch.Id;
                    ellipseWrite.FD = new List<Frequ>();
                    Mode _mode = new Mode() { FC = 2, M = mode };



                    foreach (GraphData graphData in ch.graphDatas)
                    {
                        if (ch.IsSeleted == true)
                        {
                            

                            if (_mode.M == 1)
                            {
                                _mode.OE = new OuterElliplse { a = graphData.height_O, b = graphData.width_O, t = graphData.angel_O };
                            }                            

                            // Gdata.isEnable enable/disable the frequency graph                             
                            if (graphData.Id == 1)
                            {
                                if (graphData.isEnable)
                                {
                                    lblFreq1.Text = graphData.Name + "-" + graphData.freq + "Hz";

                                    AddEllipses(cnBr1, graphData);

                                    br1_rec1.Fill = enableColor;
                                    D1.IsEnabled = true;
                                   // br1.IsEnabled = true;
                                }
                                else
                                {
                                    br1_rec1.Fill = disableColor;
                                    D1.IsEnabled = false;
                                    //br1.IsEnabled = false;
                                }
                                frequencyWrite.S = graphData.sol;
                            }                           
                        }

                        if (ChangeType == 0 && graphData.Id <= FrequencyNo)
                        {
                            // write data to port for freq and setting
                            Frequency frequency = new Frequency() { FN = graphData.Id, F = graphData.freq, G = graphData.gain, P = graphData.phase, E = graphData.isEnable ? 1 : 0 };
                            Frequ frequ = new Frequ() { FN = graphData.Id, ED = new List<Elliplse>() };
                            foreach (var el in graphData.ellipses)
                            {
                                Elliplse elliplse = new Elliplse() { FN = graphData.Id, EId = el.Id, a = el.height, b = el.width, t = el.angel, x = (int)Math.Round(el.ex, MidpointRounding.AwayFromZero), y = (int)Math.Round(el.ey, MidpointRounding.AwayFromZero) };
                                frequ.ED.Add(elliplse);
                            }
                            frequencyWrite.FD.Add(frequency);

                            ellipseWrite.FD.Add(frequ);
                        }
                    }

                    if (ChangeType == 0)
                    {
                        portCOM.WriteData(JsonConvert.SerializeObject(_mode));

                        var rat1 = portCOM.WriteData(JsonConvert.SerializeObject(frequencyWrite));
                        System.Threading.Thread.Sleep(500);
                        var rat2 = portCOM.WriteData(JsonConvert.SerializeObject(ellipseWrite));

                        rat = rat1 && rat2;
                    }
                }
            }

            return rat;
        }

        public void AddEllipses(Canvas cnBr1, GraphData graphData)
        {
            // cnBr1

            //cnBr1.Children.Clear();

            for (var i = 1; i < cnBr1.Children.Count;)
            {
                cnBr1.Children.RemoveAt(1);
            }


            if (modeApp == 1)
            {
                AddOuterCurcle(cnBr1, graphData);
            }



            foreach (var item in graphData.ellipses)
            {
                var index = graphData.ellipses.IndexOf(item);
                Ellipse el1 = new Ellipse() { Fill = Brushes.Transparent };
                el1.Height = item.height / factor;
                el1.Width = item.width / factor;
                el1.HorizontalAlignment = HorizontalAlignment.Center;
                el1.Stroke = new SolidColorBrush(MyColor.GetColor(index));
                el1.VerticalAlignment = VerticalAlignment.Center;
                Canvas.SetLeft(el1, 0);
                Canvas.SetTop(el1, 0);
                el1.RenderTransformOrigin = new Point(0, 0);

                TranslateTransform tt1 = new TranslateTransform();
                tt1.X = ((item.ex - (item.width / 2)) / factor);
                tt1.Y = (((item.ey * -1) - (item.height / 2)) / factor);

                RotateTransform rtAngel1 = new RotateTransform();
                rtAngel1.CenterX = (el1.Width / 2);
                rtAngel1.CenterY = (el1.Height / 2);
                rtAngel1.Angle = item.angel;

                TransformGroup transformGroup = new TransformGroup();
                transformGroup.Children.Add(rtAngel1);
                transformGroup.Children.Add(tt1);

                if (graphData.ellipses.Count == 1)
                {
                    el1.MouseLeftButtonDown += Ellipse_MouseLeftButtonDown;
                    el1.MouseLeftButtonUp += Ellipse_MouseLeftButtonUp;
                    el1.MouseMove += Ellipse_MouseMove;
                    el1.DataContext = graphData.Id;
                }

                el1.RenderTransform = transformGroup;
                cnBr1.Children.Add(el1);
            }

        }

        private void AddOuterCurcle(Canvas cnBr1, GraphData graphData)
        {
            el11.Visibility = Visibility.Visible;
            el11.Height = graphData.height_O / factor;
            el11.Width = graphData.width_O / factor;
            tt11.X = ((graphData.ex_O - (graphData.width_O / 2)) / factor);
            tt11.Y = (((graphData.ey_O * -1) - (graphData.height_O / 2)) / factor);
            el11.Stroke = new SolidColorBrush(Colors.DarkOrange);
            rtAngel11.CenterX = (el11.Width / 2);
            rtAngel11.CenterY = (el11.Height / 2);
            rtAngel11.Angle = graphData.angel_O;

            Ellipse el1_1 = new Ellipse();
            el1_1.Height = graphData.height_O / factor;
            el1_1.Width = graphData.width_O / factor;
            el1_1.HorizontalAlignment = HorizontalAlignment.Center;
            el1_1.Stroke = new SolidColorBrush(Colors.DarkOrange);
            el1_1.VerticalAlignment = VerticalAlignment.Center;
            Canvas.SetLeft(el1_1, 0);
            Canvas.SetTop(el1_1, 0);
            el1_1.RenderTransformOrigin = new Point(0, 0);

            TranslateTransform tt1_1 = new TranslateTransform();
            tt1_1.X = ((graphData.ex_O - (graphData.width_O / 2)) / factor);
            tt1_1.Y = (((graphData.ey_O * -1) - (graphData.height_O / 2)) / factor);

            RotateTransform rtAngel1_1 = new RotateTransform();
            rtAngel1_1.CenterX = (el1_1.Width / 2);
            rtAngel1_1.CenterY = (el1_1.Height / 2);
            rtAngel1_1.Angle = graphData.angel_O;

            TransformGroup transformGroup_1 = new TransformGroup();
            transformGroup_1.Children.Add(rtAngel1_1);
            transformGroup_1.Children.Add(tt1_1);

            el1_1.RenderTransform = transformGroup_1;
            cnBr1.Children.Add(el1_1);
        }

        private bool isDragging = false;
        private Point mouseStart;
        private TranslateTransform? dragTransform = null;

        private void Ellipse_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var ellipse = sender as Ellipse;
            if (ellipse == null) return;

            isDragging = true;
            mouseStart = e.GetPosition(cnBr1);
            ellipse.CaptureMouse();

            // find the TranslateTransform inside RenderTransform
            TransformGroup tg = ellipse.RenderTransform as TransformGroup;
            dragTransform = tg.Children.OfType<TranslateTransform>().FirstOrDefault();


        }

        private void Ellipse_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            isDragging = false;
            (sender as UIElement)?.ReleaseMouseCapture();
            dragTransform = null;


        }


        private void Ellipse_MouseMove(object sender, MouseEventArgs e)
        {
            if (!isDragging || dragTransform == null)
                return;

            Point currentPos = e.GetPosition(cnBr1);

            double dx = currentPos.X - mouseStart.X;
            double dy = currentPos.Y - mouseStart.Y;

            dragTransform.X += dx;
            dragTransform.Y += dy;

            mouseStart = currentPos;

            int FreqId = Convert.ToInt32(((Ellipse)sender).DataContext);

            DeviceCOM.channelDatas[chNo - 1].graphDatas[FreqId - 1].ellipses[0].ex = (dragTransform.X * factor) + DeviceCOM.channelDatas[0].graphDatas[0].width / 2;

            DeviceCOM.channelDatas[chNo - 1].graphDatas[FreqId - 1].ellipses[0].ey = (-1) * ((dragTransform.Y * factor) + DeviceCOM.channelDatas[0].graphDatas[0].height / 2);


        }


        private void D_Click(object sender, RoutedEventArgs e)
        {
            ellipsesPop = new CircleSetting(((Border)sender).Name);
            ellipsesPop.Closing += ellipsesPop_Closing;
            ellipsesPop.portCOM = portCOM;
            ellipsesPop.Owner = this;
            ellipsesPop.ShowDialog();
        }

        private void ellipsesPop_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (ellipsesPop.IsSaved)
            {
                ImplementChanges(2);
            }
        }

        public void SelectCh1()
        {


            var currentChannel = DeviceCOM.channelDatas.FirstOrDefault(c => c.IsSeleted == true);
            if (currentChannel?.Id != 1)
            {
                currentChannel.IsSeleted = false;
                var nextCh = DeviceCOM.channelDatas.FirstOrDefault(c => c.Id == 1);
                nextCh.IsSeleted = true;
               

            }
        }

        private void btnCh_Click(object sender, RoutedEventArgs e)
        {
            var chId = Convert.ToUInt32(((Border)sender).Tag);
            var currentChannel = DeviceCOM.channelDatas.FirstOrDefault(c => c.IsSeleted == true);
            if (currentChannel?.Id != chId)
            {
                currentChannel.IsSeleted = false;
                var nextCh = DeviceCOM.channelDatas.FirstOrDefault(c => c.Id == chId);
                nextCh.IsSeleted = true;               
                ((Border)sender).Background = new SolidColorBrush(Colors.Green);
                ImplementChanges(1);
                DeviceCOM.IsResponseRefreshRequired = true;
            }
        }

        private void btnBalance_Click(object sender, RoutedEventArgs e)
        {
            if (DeviceCOM.IsSystemBusy)
            {
                MessageBox.Show("System is busy so you can not perform this command, please wait...", "System Information");
            }
            else
            {
                //if (DeviceCOM.IsLogEnable)
                //{
                //    MessageBox.Show("While logging you can not perform this command, please stop the log.", "Command Conflict");
                //}
                //else
                //{
                var IsBalaneAll = (((Border)sender).Name == "btnBalance1All") || (((Border)sender).Name == "btnBalanceAll") || (((Border)sender).Name == "btnBalance2All");
                var SChId = DeviceCOM.channelDatas.FirstOrDefault(c => c.IsSeleted)?.Id;
                int ChId = IsBalaneAll ? 0 : Convert.ToInt32(SChId);
                BalanceTest balanceTest = new BalanceTest() { FC = 16, CN = ChId };
                var rat = portCOM.WriteData(JsonConvert.SerializeObject(balanceTest));
                if (rat)
                {
                    DeviceCOM.IsBalanceAll = IsBalaneAll;
                    DeviceCOM.IsBalanceBusyEnable = true;
                }
                else
                {
                    MessageBox.Show("Unable to balance due to the error in the communication!", "Error Information");
                }
                //}
            }

        }

        private void btnTest_Click(object sender, RoutedEventArgs e)
        {
            if (DeviceCOM.IsSystemBusy)
            {
                MessageBox.Show("System is busy so you can not perform this command, please wait...", "System Information");

            }
            else
            {
                var IsTestAll = (((Border)sender).Name == "btnTest1All") || (((Border)sender).Name == "btnTestAll") || (((Border)sender).Name == "btnTest2All");
                var SChId = DeviceCOM.channelDatas.FirstOrDefault(c => c.IsSeleted)?.Id;
                int ChId = IsTestAll ? 0 : Convert.ToInt32(SChId);

                BalanceTest balanceTest = new BalanceTest() { FC = 17, CN = ChId };
                var rat = portCOM.WriteData(JsonConvert.SerializeObject(balanceTest));
                if (!rat)
                {
                    MessageBox.Show("Unable to start test due to the error in the communication!", "Error Information");
                }
                else
                {
                    if (DeviceCOM.IsBalanceRequired)
                    {
                        MessageBox.Show("Unable to test because of balance command is required!", "Error Information");
                        DeviceCOM.IsBalanceRequired = false;
                    }
                    if (DeviceCOM.IsBinRequired)
                    {
                        MessageBox.Show("Please put the previous component to NG bin before starting the test!", "Error Information");
                        DeviceCOM.IsBinRequired = false;
                    }

                    if (!DeviceCOM.IsBalanceRequired && !DeviceCOM.IsBinRequired)
                    {
                        DeviceCOM.IsLogDisable = true;
                    }
                }
            }

        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            var IsClearAll = (((Border)sender).Name == "btnClear1All") || (((Border)sender).Name == "btnClearAll") || (((Border)sender).Name == "btnClear2All");
            ClearGraphDataWithoutBalance(IsClearAll);;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (CommunicationType == 0)
            {
                Status exitData = new Status() { FC = 24 };
                var rat = portCOM.WriteData(JsonConvert.SerializeObject(exitData));

                if (portCOM.port.IsOpen)
                    portCOM.port.Close();
            }
        }
        public void ClearGraphDataWithoutBalance(bool IsClearAll)
        {
            if (IsClearAll)
            {
                var balaceData = DeviceCOM.responses.Where(r => r.IsBalacenced).ToList();
                ClearGraphData();
                if (balaceData.Count > 0)
                {
                    DeviceCOM.responses.AddRange(balaceData);
                }
            }
            else
            {
                var SChId = DeviceCOM.channelDatas.FirstOrDefault(c => c.IsSeleted)?.Id;
                var balaceData = DeviceCOM.responses.Where(r => r.IsBalacenced && r.CN == SChId).ToList();
                ClearGraphDataByChId(Convert.ToInt32(SChId));
                if (balaceData.Count > 0)
                {
                    DeviceCOM.responses.AddRange(balaceData);
                }
            }
            DeviceCOM.IsResponseRefreshRequired = true;
        }
        public void ClearGraphData(bool IsDataClear = true)
        {
            if (IsDataClear)
            {
                DeviceCOM.responses = new List<Response>();
            }
            cn1.Children.Clear();
            rResult1.Fill = new SolidColorBrush(Colors.White);
        }
        public void ClearGraphDataByChId(int chId)
        {
            DeviceCOM.responses.RemoveAll(r => r.CN == chId);

            if (chId == 1)
            {
                cn1.Children.Clear();
                rResult1.Fill = new SolidColorBrush(Colors.White);
                lblGraphXY1.Text = "";
            }
        }
        public void RefreshResponse()
        {
            ClearGraphData(false);
            var selectedChannel = DeviceCOM.channelDatas.FirstOrDefault(c => c.IsSeleted);
            var selectedChannelData = DeviceCOM.responses.Where(r => r.CN == selectedChannel.Id).ToList();

            foreach (var item in selectedChannelData)
            {
                foreach (var fd in item.FD)
                {
                    Ellipse el1 = new Ellipse();
                    el1.Height = 4;
                    el1.Width = 4;
                    var left = fd.X / (factor);
                    var top = (fd.Y * -1) / (factor);
                    if (left > (seqLength / 2))
                    {
                        left = (seqLength / 2);
                    }
                    if (top > (seqLength / 2))
                    {
                        top = (seqLength / 2);
                    }

                    if (left < ((seqLength / 2) * -1))
                    {
                        left = ((seqLength / 2) * -1);
                    }
                    if (top < ((seqLength / 2) * -1))
                    {
                        top = ((seqLength / 2) * -1);
                    }
                    Canvas.SetLeft(el1, left - 2);
                    Canvas.SetTop(el1, top - 2);
                    //r1.Stroke = new SolidColorBrush(Colors.Black);
                    if (selectedChannelData.IndexOf(item) == selectedChannelData.Count - 1)
                    {
                        if (item.IsBalacenced)
                        {
                            el1.Fill = new SolidColorBrush(Colors.Brown);
                        }
                        else
                        {
                            el1.Fill = new SolidColorBrush(Colors.Blue);
                            if (item.OR == 1)
                            {                               
                                btnOverallResult2.Background = new SolidColorBrush(Colors.Green);
                            }
                            else
                            {
                                btnOverallResult2.Background = new SolidColorBrush(Colors.Red);
                            }
                        }
                    }
                    else
                    {
                        if (item.IsBalacenced)
                        {
                            el1.Fill = new SolidColorBrush(Colors.Brown);
                        }
                        else
                        {
                            if (fd.R == 1)
                            {
                                el1.Fill = new SolidColorBrush(Colors.Green);
                            }
                            else
                            {
                                el1.Fill = new SolidColorBrush(Colors.Red);
                            }
                        }
                    }

                    // Gdata.isEnable 
                    if (fd.FN == 1)
                    {
                        cn1.Children.Add(el1);

                        if (!item.IsBalacenced)
                        {
                            lblGraphXY1.Text = fd.X.ToString() + "," + fd.Y.ToString();
                            if (fd.R == 1)
                            {
                                rResult1.Fill = new SolidColorBrush(Colors.Green);
                            }
                            else
                            {
                                rResult1.Fill = new SolidColorBrush(Colors.Red);
                            }
                        }
                    }
                }
            }

            var cordinatesQ = DeviceCOM.cordinateQueue.ToList();

            // Add Data in Graph 
            foreach (var q in cordinatesQ)
            {
                foreach (var item in q.cordinates)
                {
                    Ellipse el1 = new Ellipse();
                    el1.Height = 4;
                    el1.Width = 4;
                    var left = (item.X / factor);
                    var top = (item.Y * -1) / (factor);
                    if (left > (seqLength / 2))
                    {
                        left = (seqLength / 2);
                    }
                    if (top > (seqLength / 2))
                    {
                        top = (seqLength / 2);
                    }

                    if (left < ((seqLength / 2) * -1))
                    {
                        left = ((seqLength / 2) * -1);
                    }
                    if (top < ((seqLength / 2) * -1))
                    {
                        top = ((seqLength / 2) * -1);
                    }
                    Canvas.SetLeft(el1, left - 2);
                    Canvas.SetTop(el1, top - 2);
                    el1.Fill = new SolidColorBrush(Colors.Orange);
                    cn1.Children.Add(el1);
                }
            }
            

        }

        private void btnResetCounter_Click(object sender, RoutedEventArgs e)
        {
            var SChId = DeviceCOM.channelDatas.FirstOrDefault(c => c.IsSeleted)?.Id;
            var cnt = DeviceCOM.counter.FirstOrDefault(c => c.Id == SChId);

            cnt.ResultCount = 0;
            cnt.ResultOkCount = 0;
            cnt.ResultOkNotCount = 0;            

            lblTCount2.Content = "Total Count - " + cnt.ResultCount.ToString();
            lblOkCount2.Content = "OK Count - " + cnt.ResultOkCount.ToString();
            lblNotOkCount2.Content = "Not Ok Count - " + cnt.ResultOkNotCount.ToString();

        }

        private void btnLog_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (DeviceCOM.IsLogEnable)
            {
                DeviceCOM.IsLogEnable = false;               
                lblLog2.Content = "Start Log";
                lblPartLogs.Content = "";
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
                lblLog2.Content = "Stop Log";
                lblPartLogs.Content = DeviceCOM.part.BatchName + " => " + DeviceCOM.part.Name;
            }
            else
            {
                lblPartLogs.Content = "";
            }
        }

        private void btnStop_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Status status = new Status() { FC = 18 };
            var rat = portCOM.WriteData(JsonConvert.SerializeObject(status));
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.B || e.Key == Key.Space || e.Key == Key.R)
            {
                if (e.Key == Key.B)
                {
                    if (DeviceCOM.IsSystemBusy)
                    {
                        MessageBox.Show("System is busy so you can not perform this command, please wait...", "System Information");
                    }
                    else
                    {
                        BalanceTest balanceTest = new BalanceTest() { FC = 16, CN = 0 };
                        var rat = portCOM.WriteData(JsonConvert.SerializeObject(balanceTest));
                        if (rat)
                        {
                            DeviceCOM.IsBalanceAll = true;
                            DeviceCOM.IsBalanceBusyEnable = true;
                        }
                        else
                        {
                            MessageBox.Show("Unable to balance due to the error in the communication!", "Error Information");
                        }
                    }

                }
                else if (e.Key == Key.R)
                {
                    var SChId = DeviceCOM.channelDatas.FirstOrDefault(c => c.IsSeleted)?.Id;
                    var cnt = DeviceCOM.counter.FirstOrDefault(c => c.Id == SChId);

                    cnt.ResultCount = 0;
                    cnt.ResultOkCount = 0;
                    cnt.ResultOkNotCount = 0;                    

                    lblTCount2.Content = "Total Count - " + cnt.ResultCount.ToString();
                    lblOkCount2.Content = "OK Count - " + cnt.ResultOkCount.ToString();
                    lblNotOkCount2.Content = "Not Ok Count - " + cnt.ResultOkNotCount.ToString();

                }
                else if (e.Key == Key.Space)
                {
                    if (DeviceCOM.IsSystemBusy)
                    {
                        MessageBox.Show("System is busy so you can not perform this command, please wait...", "System Information");

                    }
                    else
                    {

                        BalanceTest balanceTest = new BalanceTest() { FC = 17, CN = 0 };
                        var rat = portCOM.WriteData(JsonConvert.SerializeObject(balanceTest));
                        if (!rat)
                        {
                            MessageBox.Show("Unable to start test due to the error in the communication!", "Error Information");
                        }
                        else
                        {
                            if (DeviceCOM.IsBalanceRequired)
                            {
                                MessageBox.Show("Unable to test because of balance command is required!", "Error Information");
                                DeviceCOM.IsBalanceRequired = false;
                            }
                            if (DeviceCOM.IsBinRequired)
                            {
                                MessageBox.Show("Please put the previous component to NG bin before starting the test!", "Error Information");
                                DeviceCOM.IsBinRequired = false;
                            }

                            if (!DeviceCOM.IsBalanceRequired && !DeviceCOM.IsBinRequired)
                            {
                                DeviceCOM.IsLogDisable = true;
                            }
                        }
                    }
                }
            }
        }
    }

    public class UdpReceiver
    {
        public UdpClient _udpClient;
        private IPEndPoint _remoteIpEndPoint;

        // A structure to hold the state information for the asynchronous operation
        public struct UdpState
        {
            public UdpClient u;
            public IPEndPoint e;
        }

        public UdpReceiver(int port)
        {
            _remoteIpEndPoint = new IPEndPoint(IPAddress.Any, port);
            _udpClient = new UdpClient(_remoteIpEndPoint);

            Console.WriteLine($"Listening for UDP messages on port {port}...");
        }

        public void StartReceiving()
        {
            UdpState s = new UdpState();
            s.e = _remoteIpEndPoint;
            s.u = _udpClient;
            // Begin the asynchronous receive operation
            _udpClient.BeginReceive(new AsyncCallback(ReceiveCallback), s);
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            UdpClient u = ((UdpState)(ar.AsyncState)).u;
            IPEndPoint e = ((UdpState)(ar.AsyncState)).e;

            try
            {
                // Complete the asynchronous receive operation and get the data
                DeviceCOM.receiveBytes = u.EndReceive(ar, ref e);
            }
            catch (ObjectDisposedException)
            {
                // Handle cases where the UdpClient might have been closed
                Console.WriteLine("UdpClient was disposed.");
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during receive: {ex.Message}");
            }
            finally
            {
                // Restart listening for the next datagram
                UdpState s = new UdpState();
                s.e = e; // Use the updated IPEndPoint for the next receive
                s.u = u;
                u.BeginReceive(new AsyncCallback(ReceiveCallback), s);
            }
        }

        public void StopReceiving()
        {
            _udpClient.Close();
            _udpClient.Dispose();
            Console.WriteLine("UDP receiver stopped.");
        }
    }

    public class BarcodeScanner
    {
        private readonly StringBuilder _buffer = new();
        private readonly DispatcherTimer _timer;

        // Event raised when a full barcode is detected
        public event EventHandler<string>? BarcodeScanned;

        public BarcodeScanner()
        {
            // Timer resets when no keys come in for a short time (end of scan)
            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
            _timer.Tick += (s, e) =>
            {
                if (_buffer.Length > 0)
                {
                    string code = _buffer.ToString();
                    _buffer.Clear();
                    BarcodeScanned?.Invoke(this, code);
                }
                _timer.Stop();
            };
        }

        public void HandleKey(KeyEventArgs e)
        {
            // Convert key input into character
            char c = GetCharFromKey(e.Key);
            if (c == '\0')
                return;

            if (e.Key == Key.Enter)
            {
                string code = _buffer.ToString();
                _buffer.Clear();
                _timer.Stop();
                BarcodeScanned?.Invoke(this, code);
            }
            else
            {
                _buffer.Append(c);
                _timer.Stop();
                _timer.Start();
            }
        }

        private static char GetCharFromKey(Key key)
        {
            // Simple conversion for A-Z, 0-9 and common symbols
            if (key >= Key.A && key <= Key.Z)
                return (char)('A' + (key - Key.A));
            if (key >= Key.D0 && key <= Key.D9)
                return (char)('0' + (key - Key.D0));
            if (key == Key.OemMinus)
                return '-';
            if (key == Key.Space)
                return ' ';
            if (key == Key.Enter)
                return '\r';

            return '\0';
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
        public Freq freqPop { get; set; }
        string filename { get; set; }
        public CircleSetting ellipsesPop { get; set; }
        public MainWindow mainWindow { get; set; }
        public ObservableCollection<MenuItemViewModel> MenuItems { get; set; }

        public ICommand Command
        {
            get
            {
                return _command;
            }
        }

        private void Execute()
        {
            // (NOTE: In a view model, you normally should not use MessageBox.Show()).
            //MessageBox.Show("Clicked at " + Header);
            if (DeviceCOM.IsLogEnable)
            {
                MessageBox.Show("While logging you can not perform this command, please stop the log.", "Command Conflict");
            }
            else
            {
                if ((Header == "Open" || Header == "New" || Header == "Write Configuration") && DeviceCOM.IsSystemBusy)
                {
                    MessageBox.Show("System is busy so you can not perform this command, please wait...", "System Information");
                }
                else
                {
                    if (Header == "Change Configuration")
                    {
                        freqPop = new Freq();
                        freqPop.Closing += freqPop_Closing;
                        freqPop.portCOM = mainWindow.portCOM;
                        freqPop.Owner = mainWindow;
                        freqPop.ShowDialog();
                    }
                    else if (Header == "Threshold Setting")
                    {
                        ellipsesPop = new CircleSetting("D1");
                        ellipsesPop.Closing += ellipsesPop_Closing;
                        ellipsesPop.portCOM = mainWindow.portCOM;
                        ellipsesPop.Owner = mainWindow;
                        ellipsesPop.ShowDialog();
                    }
                    else if (Header == "Write Configuration")
                    {
                        try
                        {
                            var msg = "Configuation Write successfully!!";
                            var rat = mainWindow.ImplementChanges(0);
                            if (!rat)
                            {
                                msg = "No response from the system, please reboot the board";
                            }

                            MessageBox.Show(msg, "Information");
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Error while writing the configuration!!!!", "Information");
                        }
                    }
                    else if (Header == "Copy Channel-1 Configuration")
                    {
                        var chNo1 = DeviceCOM.channelDatas.FirstOrDefault(c => c.Id == 1);
                        foreach (var ch in DeviceCOM.channelDatas)
                        {
                            if (ch.Id <= mainWindow.chNo && ch.Id != 1)
                            {
                                foreach (var item in ch.graphDatas)
                                {
                                    var freq = chNo1?.graphDatas.FirstOrDefault(g => g.Id == item.Id);
                                    item.freq = freq.freq;
                                    item.gain = freq.gain;
                                    item.phase = freq.phase;
                                    item.height = freq.height;
                                    item.width = freq.width;
                                    item.ex = freq.ex;
                                    item.ey = freq.ey;
                                    item.angel = freq.angel;
                                }
                            }
                        }
                        var rat = mainWindow.ImplementChanges(0);
                        var msg = "Channel-1 Configuration copied to others successfully!!";
                        if (!rat)
                        {
                            msg = "No response from the system, please reboot the board";
                        }
                        MessageBox.Show(msg, "Information");

                    }
                    
                    else if (Header == "Save")
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

                                    string conecnt = JsonConvert.SerializeObject(DeviceCOM.channelDatas);
                                    File.WriteAllText(filename, conecnt);                                    
                                    //MessageBox.Show("Configuation changes saved at '" + filename + "'!!!!");
                                    this.mainWindow.lblConfigFileName.Content = filename;
                                }

                            }
                            else
                            {
                                string conecnt = JsonConvert.SerializeObject(DeviceCOM.channelDatas);
                                File.WriteAllText(filename, conecnt);
                                //this.mainWindow.btnLog.Visibility = Visibility.Visible;
                                //MessageBox.Show("Configuation changes saved at '" + filename + "'!!!!");
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

                                string conecnt = JsonConvert.SerializeObject(DeviceCOM.channelDatas);
                                File.WriteAllText(filename, conecnt);
                                //this.mainWindow.btnLog.Visibility = Visibility.Visible;
                                //MessageBox.Show("Configuation changes saved at '" + filename + "'!!!!");
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
                                DeviceCOM.channelDatas = JsonConvert.DeserializeObject<List<ChannelData>>(data);
                                // Open document
                                filename = dialog.FileName;
                                mainWindow.SelectCh1();
                                mainWindow.ClearGraphData();

                                var rat = mainWindow.ImplementChanges(0);
                                if (!rat)
                                {
                                    var msg = "No response from the system, please reboot the board";
                                    MessageBox.Show(msg, "Information");
                                }

                                //this.mainWindow.btnLog.Visibility = Visibility.Visible;
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
                        mainWindow.InitialGraphData(false);
                        mainWindow.ClearGraphData();
                        var rat = mainWindow.ImplementChanges(0);
                        if (!rat)
                        {
                            var msg = "No response from the system, please reboot the board";
                            MessageBox.Show(msg, "Information");
                        }
                        DeviceCOM.IsLogEnable = false;
                        this.mainWindow.lblLog2.Content = "Start Log";
                        DeviceCOM.part = new Part();
                        this.mainWindow.lblPartLogs.Content = "";
                        this.mainWindow.lblConfigFileName.Content = "";
                        //this.mainWindow.btnLog.Visibility = Visibility.Hidden;
                    }
                    else if (Header == "Exit")
                    {
                        //this.mainWindow.btnLog.Visibility = Visibility.Hidden;
                        mainWindow.Close();
                    }
                    else if (Header == "Batch Wise Log")
                    {
                        Logs logs = new Logs();
                        logs.ShowDialog();
                    }
                    else if (Header == "Serial Number Log")
                    {
                        LogAll logs = new LogAll();
                        logs.ShowDialog();
                    }
                }
            }
        }


        private void freqPop_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (freqPop.IsSaved)
            {
                mainWindow.ImplementChanges(1);
            }
        }

        private void ellipsesPop_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (ellipsesPop.IsSaved)
            {
                mainWindow.ImplementChanges(2);
            }
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

    public class TcpClientWithEvents
    {
        private readonly TcpClient _client = new TcpClient();
        private NetworkStream _stream;
        private CancellationTokenSource _cts;

        public event EventHandler<string>? DataReceived;
        public event EventHandler? Disconnected;

        public async Task ConnectAsync(string host, int port)
        {
            await _client.ConnectAsync(host, port);
            _stream = _client.GetStream();
            _cts = new CancellationTokenSource();
            _ = Task.Run(() => ReceiveLoopAsync(_cts.Token));
        }

        private async Task ReceiveLoopAsync(CancellationToken token)
        {
            byte[] buffer = new byte[1024];

            try
            {
                while (!token.IsCancellationRequested)
                {
                    int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length, token);
                    if (bytesRead == 0)
                    {
                        Disconnected?.Invoke(this, EventArgs.Empty);
                        break;
                    }

                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    DataReceived?.Invoke(this, message);
                }
            }
            catch
            {
                Disconnected?.Invoke(this, EventArgs.Empty);
            }
        }

        public async Task SendAsync(string message)
        {
            if (_stream == null) return;
            byte[] data = Encoding.UTF8.GetBytes(message);
            await _stream.WriteAsync(data, 0, data.Length);
        }

        public void Disconnect()
        {
            _cts?.Cancel();
            _stream?.Close();
            _client?.Close();
        }
    }
}

