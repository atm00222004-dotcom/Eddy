using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.IO.Ports;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Channels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
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
        public int chNo;
        int factor = 20;

        int ScreenId = 1;
        int BoxSize1 = 430;
        int BoxSize2 = 0;
        int BoxSize3 = 0;
        int BoxSize4 = 0;
        int seqLength = 0;
        int CommunicationType = 0;
        int FrequencyNo = 8; 
        public string WebPage;
        //bool IsBalanceAll = false;
        public SolidColorBrush disableColor = new SolidColorBrush(Colors.DarkGray);
        public SolidColorBrush enableColor = new SolidColorBrush(Colors.White);
        bool IsSerialmatch = true;
        public MainWindow()
        {
            
            InitializeComponent();
            //if (imgLogo.Visibility == Visibility.Visible)
            //{
            //    string LogoPath = Convert.ToString(System.Configuration.ConfigurationSettings.AppSettings["LogoPath"]);
            //    imgLogo.Source = new BitmapImage(new Uri(LogoPath));
            //}

            WebPage = Convert.ToString(System.Configuration.ConfigurationSettings.AppSettings["WebPage"]);

            ScreenId = Convert.ToInt32(System.Configuration.ConfigurationSettings.AppSettings["ScreenId"]);
            BoxSize1 = Convert.ToInt32(System.Configuration.ConfigurationSettings.AppSettings["BoxSize1"]);
            BoxSize2 = Convert.ToInt32(System.Configuration.ConfigurationSettings.AppSettings["BoxSize2"]);
            BoxSize3 = Convert.ToInt32(System.Configuration.ConfigurationSettings.AppSettings["BoxSize3"]);
            BoxSize4 = Convert.ToInt32(System.Configuration.ConfigurationSettings.AppSettings["BoxSize4"]);
            FrequencyNo = Convert.ToInt32(System.Configuration.ConfigurationSettings.AppSettings["FrequencyNo"]);

            var LogEnabled = Convert.ToBoolean(System.Configuration.ConfigurationSettings.AppSettings["LogEnable"]);
            if (!LogEnabled)
            {
                btnLog.Visibility = Visibility.Hidden;
                btnLog1.Visibility = Visibility.Hidden;
                btnLog2.Visibility = Visibility.Hidden;
                LogWidth.Width = new GridLength(0.0, GridUnitType.Star);
                LogHeight.Height = new GridLength(0.0, GridUnitType.Star);
            }

            if (ScreenId == 1)
            {
                seqLength = BoxSize1;
                menuHeight.Height = new GridLength(0.5, GridUnitType.Star);
                chennelHeight.Height = new GridLength(0.7, GridUnitType.Star);
                buttonBarHeight.Height = new GridLength(0.0, GridUnitType.Star);
                buttonBarWidth.Width = new GridLength(.38, GridUnitType.Star);
                LogoWidth.Width = new GridLength(1.0, GridUnitType.Star);
            }
            else if(ScreenId == 2)
            {
                seqLength = BoxSize2;
                menuHeight.Height = new GridLength(0.8, GridUnitType.Star);
                chennelHeight.Height = new GridLength(0.6, GridUnitType.Star);
                buttonBarHeight.Height = new GridLength(2, GridUnitType.Star);
                buttonBarWidth.Width = new GridLength(0.0, GridUnitType.Star);
                LogoWidth.Width = new GridLength(1.3, GridUnitType.Star);

            }
            else if (ScreenId == 3)
            {
                seqLength = BoxSize3;
                menuHeight.Height = new GridLength(0.5, GridUnitType.Star);
                chennelHeight.Height = new GridLength(0.7, GridUnitType.Star);
                buttonBarHeight.Height = new GridLength(0.0, GridUnitType.Star);
                buttonBarWidth.Width = new GridLength(.38, GridUnitType.Star);
                LogoWidth.Width = new GridLength(1.0, GridUnitType.Star);
            }
            else if (ScreenId == 4)
            {
                seqLength = BoxSize4;
                menuHeight.Height = new GridLength(0.8, GridUnitType.Star);
                chennelHeight.Height = new GridLength(0.6, GridUnitType.Star);
                buttonBarHeight.Height = new GridLength(2, GridUnitType.Star);
                buttonBarWidth.Width = new GridLength(0.0, GridUnitType.Star);
                LogoWidth.Width = new GridLength(1.7, GridUnitType.Star);

                if (FrequencyNo == 4 || FrequencyNo == 1)
                {
                    Grid.SetRow(br3, 1);
                    Grid.SetColumn(br3, 1);
                    Grid.SetRow(br4, 1);
                    Grid.SetColumn(br4, 2);

                    buttonbar2.Visibility = Visibility.Visible;
                    buttonbar1.Visibility = Visibility.Hidden;
                    counterbar2.Visibility = Visibility.Visible;
                    counterbar1.Visibility = Visibility.Hidden;

                    menuHeight.Height = new GridLength(0.35, GridUnitType.Star);
                    chennelHeight.Height = new GridLength(0.5, GridUnitType.Star);
                    buttonBarWidth.Width = new GridLength(1, GridUnitType.Star);
                    FrequencySpaceCol3.Width = new GridLength(0, GridUnitType.Star);
                    FrequencySpaceCol4.Width = new GridLength(0, GridUnitType.Star);
                    buttonBarHeight.Height = new GridLength(0, GridUnitType.Star);

                    br5.Visibility = Visibility.Hidden;
                    br6.Visibility = Visibility.Hidden;
                    br7.Visibility = Visibility.Hidden;
                    br8.Visibility = Visibility.Hidden;
                    
                    if (FrequencyNo == 1)
                    {
                        br2.Visibility = Visibility.Hidden;
                        br3.Visibility = Visibility.Hidden;
                        br4.Visibility = Visibility.Hidden;
                    }
                    
                }
            }

            portCOM = new DeviceCOM();

            factor = Convert.ToInt16(System.Configuration.ConfigurationSettings.AppSettings["Factor"]);
            DeviceCOM.DefaultWidth = Convert.ToInt16(System.Configuration.ConfigurationSettings.AppSettings["Width"]);
            DeviceCOM.DefaultHeight = Convert.ToInt16(System.Configuration.ConfigurationSettings.AppSettings["Height"]);
            CommunicationType = Convert.ToInt16(System.Configuration.ConfigurationSettings.AppSettings["CommunicationType"]);
            int baudRate = Convert.ToInt32(System.Configuration.ConfigurationSettings.AppSettings["BaudRate"]);
            string portName = Convert.ToString(System.Configuration.ConfigurationSettings.AppSettings["PortName"]);

            string IpAddress = Convert.ToString(System.Configuration.ConfigurationSettings.AppSettings["IP"]);
            int Port = Convert.ToInt32(System.Configuration.ConfigurationSettings.AppSettings["Port"]);

            DeviceCOM.ConnectionString = System.Configuration.ConfigurationSettings.AppSettings["ConnectionString"];

            portCOM.InitialPort(CommunicationType, portName, baudRate, IpAddress, Port);

            DeviceCOM.responses = new List<Response>();
            chNo = Convert.ToInt16(System.Configuration.ConfigurationSettings.AppSettings["Channel"]);
            DeviceCOM.ChannelNo = chNo;
            if (chNo == 1)
            {
                btnCh1.Visibility = Visibility.Hidden;
                btnCh2.Visibility = Visibility.Hidden;
                btnCh3.Visibility = Visibility.Hidden;
                btnCh4.Visibility = Visibility.Hidden;
            }
            else if (chNo == 2)
            {
                btnCh1.Visibility = Visibility.Visible;
                btnCh2.Visibility = Visibility.Visible;
                btnCh3.Visibility = Visibility.Hidden;
                btnCh4.Visibility = Visibility.Hidden;
            }
            else if (chNo == 3)
            {
                btnCh1.Visibility = Visibility.Visible;
                btnCh2.Visibility = Visibility.Visible;
                btnCh3.Visibility = Visibility.Visible;
                btnCh4.Visibility = Visibility.Hidden;
            }
            else if (chNo == 4)
            {
                btnCh1.Visibility = Visibility.Visible;
                btnCh2.Visibility = Visibility.Visible;
                btnCh3.Visibility = Visibility.Visible;
                btnCh4.Visibility = Visibility.Visible;
            }

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
                            new MenuItemViewModel { Header = "Copy Channel-1 Configuration", mainWindow = this },
                            new MenuItemViewModel { Header = "Data Log", mainWindow = this }
                        } : 
                        new ObservableCollection<MenuItemViewModel>
                        {
                            new MenuItemViewModel { Header = "Change Configuration", mainWindow = this },
                            new MenuItemViewModel { Header = "Threshold Setting", mainWindow = this },
                            new MenuItemViewModel { Header = "Write Configuration", mainWindow = this },
                            new MenuItemViewModel { Header = "Copy Channel-1 Configuration", mainWindow = this }
                        }
                },
                new MenuItemViewModel { Header = "View Log" },
            };
            DataContext = this;

            InitialGraphData(true);

            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(10000000);
            dispatcherTimer.Start();

            Status status = new Status() { FC = 23 };
            var rat = portCOM.GetSystemStatus(JsonConvert.SerializeObject(status));

            if (DeviceCOM.IsSystemBusy || !rat)
            {
                ImplementChanges(1);
            }
            else
            {
                ImplementChanges(0);
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
            if (!IsSerialmatch)
            {
                CheckSerailNumber();
            }

            if(DeviceCOM.IsSystemBusy)
            {
                brStatus.Background = new SolidColorBrush(Colors.Red);

                if (DeviceCOM.busyStamp.AddSeconds(20) < System.DateTime.Now)
                {
                    DeviceCOM.IsSystemBusy = false;
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

                var cnt =  DeviceCOM.counter.FirstOrDefault(c => c.Id == SChId);

                lblTCount.Content = "Total Count - " + cnt.ResultCount.ToString();
                lblOkCount.Content = "OK Count - " + cnt.ResultOkCount.ToString();
                lblNotOkCount.Content = "Not Ok Count - " + cnt.ResultOkNotCount.ToString();

                lblTCount1.Content = "Total Count - " + cnt.ResultCount.ToString();
                lblOkCount1.Content = "OK Count - " + cnt.ResultOkCount.ToString();
                lblNotOkCount1.Content = "Not Ok Count - " + cnt.ResultOkNotCount.ToString();

                lblTCount2.Content = "Total Count - " + cnt.ResultCount.ToString();
                lblOkCount2.Content = "OK Count - " + cnt.ResultOkCount.ToString();
                lblNotOkCount2.Content = "Not Ok Count - " + cnt.ResultOkNotCount.ToString();

                DeviceCOM.IsResponseRefreshRequired = false;
            }

            if (DeviceCOM.IsResponseClearRequired)
            {
                //ClearGraphData();

                //foreach (var ch in DeviceCOM.channelDatas)
                //{
                //    var rData = "{\"FC\":20,\"CN\":1,\"OR\":0,\"FD\":[{\"FN\":1,\"R\":0,\"X\":0,\"Y\":0},{\"FN\":2,\"R\":0,\"X\":0,\"Y\":0},{\"FN\":3,\"R\":0,\"X\":0,\"Y\":0},{\"FN\":4,\"R\":0,\"X\":0,\"Y\":0},{\"FN\":5,\"R\":0,\"X\":0,\"Y\":0},{\"FN\":6,\"R\":0,\"X\":0,\"Y\":0},{\"FN\":7,\"R\":0,\"X\":0,\"Y\":0},{\"FN\":8,\"R\":0,\"X\":0,\"Y\":0}]}";
                //    var res = JsonConvert.DeserializeObject<Response>(rData);
                //    res.CN = ch.Id;
                //    res.IsBalacenced = true;
                //    DeviceCOM.responses.Add(res);
                //}

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
        }

        public void InitialGraphData(bool IsPayLaod )
        {
            if (IsPayLaod)
            {
                ClearGraphData();

                br1_rec1.Height = seqLength;
                br1_rec1.Width = seqLength;
                Canvas.SetLeft(br1_can1, seqLength);
                br1_rec2.Width = seqLength;
                Canvas.SetTop(br1_rec2, seqLength/2);
                br1_rec3.Height = seqLength;
                Canvas.SetLeft(br1_rec3, seqLength / 2);
                Canvas.SetLeft(rResult1, seqLength -25);
                Canvas.SetTop(rResult1, seqLength - 25);
                Canvas.SetLeft(cnBr1, seqLength / 2);
                Canvas.SetTop(cnBr1, seqLength / 2);
                Canvas.SetTop(D1, seqLength - 25);

                br2_rec1.Height = seqLength;
                br2_rec1.Width = seqLength;
                Canvas.SetLeft(br2_can1, seqLength);
                br2_rec2.Width = seqLength;
                Canvas.SetTop(br2_rec2, seqLength / 2);
                br2_rec3.Height = seqLength;
                Canvas.SetLeft(br2_rec3, seqLength / 2);
                Canvas.SetLeft(rResult2, seqLength - 25);
                Canvas.SetTop(rResult2, seqLength - 25);
                Canvas.SetLeft(cnBr2, seqLength / 2);
                Canvas.SetTop(cnBr2, seqLength / 2);
                Canvas.SetTop(D2, seqLength - 25);

                br3_rec1.Height = seqLength;
                br3_rec1.Width = seqLength;
                Canvas.SetLeft(br3_can1, seqLength);
                br3_rec2.Width = seqLength;
                Canvas.SetTop(br3_rec2, seqLength / 2);
                br3_rec3.Height = seqLength;
                Canvas.SetLeft(br3_rec3, seqLength / 2);
                Canvas.SetLeft(rResult3, seqLength - 25);
                Canvas.SetTop(rResult3, seqLength - 25);
                Canvas.SetLeft(cnBr3, seqLength / 2);
                Canvas.SetTop(cnBr3, seqLength / 2);
                Canvas.SetTop(D3, seqLength - 25);

                br4_rec1.Height = seqLength;
                br4_rec1.Width = seqLength;
                Canvas.SetLeft(br4_can1, seqLength);
                br4_rec2.Width = seqLength;
                Canvas.SetTop(br4_rec2, seqLength / 2);
                br4_rec3.Height = seqLength;
                Canvas.SetLeft(br4_rec3, seqLength / 2);
                Canvas.SetLeft(rResult4, seqLength - 25);
                Canvas.SetTop(rResult4, seqLength - 25);
                Canvas.SetLeft(cnBr4, seqLength / 2);
                Canvas.SetTop(cnBr4, seqLength / 2);
                Canvas.SetTop(D4, seqLength - 25);

                br5_rec1.Height = seqLength;
                br5_rec1.Width = seqLength;
                Canvas.SetLeft(br5_can1, seqLength);
                br5_rec2.Width = seqLength;
                Canvas.SetTop(br5_rec2, seqLength / 2);
                br5_rec3.Height = seqLength;
                Canvas.SetLeft(br5_rec3, seqLength / 2);
                Canvas.SetLeft(rResult5, seqLength - 25);
                Canvas.SetTop(rResult5, seqLength - 25);
                Canvas.SetLeft(cnBr5, seqLength / 2);
                Canvas.SetTop(cnBr5, seqLength / 2);
                Canvas.SetTop(D5, seqLength - 25);

                br6_rec1.Height = seqLength;
                br6_rec1.Width = seqLength;
                Canvas.SetLeft(br6_can1, seqLength);
                br6_rec2.Width = seqLength;
                Canvas.SetTop(br6_rec2, seqLength / 2);
                br6_rec3.Height = seqLength;
                Canvas.SetLeft(br6_rec3, seqLength / 2);
                Canvas.SetLeft(rResult6, seqLength - 25);
                Canvas.SetTop(rResult6, seqLength - 25);
                Canvas.SetLeft(cnBr6, seqLength / 2);
                Canvas.SetTop(cnBr6, seqLength / 2);
                Canvas.SetTop(D6, seqLength - 25);

                br7_rec1.Height = seqLength;
                br7_rec1.Width = seqLength;
                Canvas.SetLeft(br7_can1, seqLength);
                br7_rec2.Width = seqLength;
                Canvas.SetTop(br7_rec2, seqLength / 2);
                br7_rec3.Height = seqLength;
                Canvas.SetLeft(br7_rec3, seqLength / 2);
                Canvas.SetLeft(rResult7, seqLength - 25);
                Canvas.SetTop(rResult7, seqLength - 25);
                Canvas.SetLeft(cnBr7, seqLength / 2);
                Canvas.SetTop(cnBr7, seqLength / 2);
                Canvas.SetTop(D7, seqLength - 25);

                br8_rec1.Height = seqLength;
                br8_rec1.Width = seqLength;
                Canvas.SetLeft(br8_can1, seqLength);
                br8_rec2.Width = seqLength;
                Canvas.SetTop(br8_rec2, seqLength / 2);
                br8_rec3.Height = seqLength;
                Canvas.SetLeft(br8_rec3, seqLength / 2);
                Canvas.SetLeft(rResult8, seqLength - 25);
                Canvas.SetTop(rResult8, seqLength - 25);
                Canvas.SetLeft(cnBr8, seqLength / 2);
                Canvas.SetTop(cnBr8, seqLength / 2);
                Canvas.SetTop(D8, seqLength - 25);

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

                    Rectangle r2 = new Rectangle();
                    r2.Height = .2;
                    r2.Width = seqLength;
                    Canvas.SetLeft(r2, 0);
                    Canvas.SetTop(r2, i);
                    r2.Stroke = new SolidColorBrush(Colors.Black);
                    r2.Fill = new SolidColorBrush(Colors.LightGray);
                    Canvas2.Children.Add(r2);

                    Rectangle r3 = new Rectangle();
                    r3.Height = .2;
                    r3.Width = seqLength;
                    Canvas.SetLeft(r3, 0);
                    Canvas.SetTop(r3, i);
                    r3.Stroke = new SolidColorBrush(Colors.Black);
                    r3.Fill = new SolidColorBrush(Colors.LightGray);
                    Canvas3.Children.Add(r3);

                    Rectangle r4 = new Rectangle();
                    r4.Height = .2;
                    r4.Width = seqLength;
                    Canvas.SetLeft(r4, 0);
                    Canvas.SetTop(r4, i);
                    r4.Stroke = new SolidColorBrush(Colors.Black);
                    r4.Fill = new SolidColorBrush(Colors.LightGray);
                    Canvas4.Children.Add(r4);

                    Rectangle r5 = new Rectangle();
                    r5.Height = .2;
                    r5.Width = seqLength;
                    Canvas.SetLeft(r5, 0);
                    Canvas.SetTop(r5, i);
                    r5.Stroke = new SolidColorBrush(Colors.Black);
                    r5.Fill = new SolidColorBrush(Colors.LightGray);
                    Canvas5.Children.Add(r5);

                    Rectangle r6 = new Rectangle();
                    r6.Height = .2;
                    r6.Width = seqLength;
                    Canvas.SetLeft(r6, 0);
                    Canvas.SetTop(r6, i);
                    r6.Stroke = new SolidColorBrush(Colors.Black);
                    r6.Fill = new SolidColorBrush(Colors.LightGray);
                    Canvas6.Children.Add(r6);

                    Rectangle r7 = new Rectangle();
                    r7.Height = .2;
                    r7.Width = seqLength;
                    Canvas.SetLeft(r7, 0);
                    Canvas.SetTop(r7, i);
                    r7.Stroke = new SolidColorBrush(Colors.Black);
                    r7.Fill = new SolidColorBrush(Colors.LightGray);
                    Canvas7.Children.Add(r7);

                    Rectangle r8 = new Rectangle();
                    r8.Height = .2;
                    r8.Width = seqLength;
                    Canvas.SetLeft(r8, 0);
                    Canvas.SetTop(r8, i);
                    r8.Stroke = new SolidColorBrush(Colors.Black);
                    r8.Fill = new SolidColorBrush(Colors.LightGray);
                    Canvas8.Children.Add(r8);

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

                    Rectangle rr2 = new Rectangle();
                    rr2.Height = seqLength;
                    rr2.Width = .2;
                    Canvas.SetLeft(rr2, i);
                    Canvas.SetTop(rr2, 0);
                    rr2.Stroke = new SolidColorBrush(Colors.Black);
                    rr2.Fill = new SolidColorBrush(Colors.LightGray);
                    Canvas2.Children.Add(rr2);

                    Rectangle rr3 = new Rectangle();
                    rr3.Height = seqLength;
                    rr3.Width = .2;
                    Canvas.SetLeft(rr3, i);
                    Canvas.SetTop(rr3, 0);
                    rr3.Stroke = new SolidColorBrush(Colors.Black);
                    rr3.Fill = new SolidColorBrush(Colors.LightGray);
                    Canvas3.Children.Add(rr3);

                    Rectangle rr4 = new Rectangle();
                    rr4.Height = seqLength;
                    rr4.Width = .2;
                    Canvas.SetLeft(rr4, i);
                    Canvas.SetTop(rr4, 0);
                    rr4.Stroke = new SolidColorBrush(Colors.Black);
                    rr4.Fill = new SolidColorBrush(Colors.LightGray);
                    Canvas4.Children.Add(rr4);

                    Rectangle rr5 = new Rectangle();
                    rr5.Height = seqLength;
                    rr5.Width = .2;
                    Canvas.SetLeft(rr5, i);
                    Canvas.SetTop(rr5, 0);
                    rr5.Stroke = new SolidColorBrush(Colors.Black);
                    rr5.Fill = new SolidColorBrush(Colors.LightGray);
                    Canvas5.Children.Add(rr5);

                    Rectangle rr6 = new Rectangle();
                    rr6.Height = seqLength;
                    rr6.Width = .2;
                    Canvas.SetLeft(rr6, i);
                    Canvas.SetTop(rr6, 0);
                    rr6.Stroke = new SolidColorBrush(Colors.Black);
                    rr6.Fill = new SolidColorBrush(Colors.LightGray);
                    Canvas6.Children.Add(rr6);

                    Rectangle rr7 = new Rectangle();
                    rr7.Height = seqLength;
                    rr7.Width = .2;
                    Canvas.SetLeft(rr7, i);
                    Canvas.SetTop(rr7, 0);
                    rr7.Stroke = new SolidColorBrush(Colors.Black);
                    rr7.Fill = new SolidColorBrush(Colors.LightGray);
                    Canvas7.Children.Add(rr7);

                    Rectangle rr8 = new Rectangle();
                    rr8.Height = seqLength;
                    rr8.Width = .2;
                    Canvas.SetLeft(rr8, i);
                    Canvas.SetTop(rr8, 0);
                    rr8.Stroke = new SolidColorBrush(Colors.Black);
                    rr8.Fill = new SolidColorBrush(Colors.LightGray);
                    Canvas8.Children.Add(rr8);
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

            btnCh1.Background = new SolidColorBrush(Colors.DarkGray);
            btnCh2.Background = new SolidColorBrush(Colors.DarkGray);
            btnCh3.Background = new SolidColorBrush(Colors.DarkGray);
            btnCh4.Background = new SolidColorBrush(Colors.DarkGray);

            btnCh1.Background = new SolidColorBrush(Colors.Green);
        }

        public List<GraphData> IniGdata()
        {
            List<GraphData> graphDatas = new List<GraphData>();

            GraphData graphD1 = new GraphData();
            graphD1.Id = 1;
            graphD1.Name = "D1";
            graphDatas.Add(graphD1);

            GraphData graphD2 = new GraphData();
            graphD2.Id = 2;
            graphD2.Name = "D2";
            graphDatas.Add(graphD2);

            GraphData graphD3 = new GraphData();
            graphD3.Id = 3;
            graphD3.Name = "D3";
            graphDatas.Add(graphD3);

            GraphData graphD4 = new GraphData();
            graphD4.Id = 4;
            graphD4.Name = "D4";
            graphDatas.Add(graphD4);

            GraphData graphD5 = new GraphData();
            graphD5.Id = 5;
            graphD5.Name = "D5";
            graphDatas.Add(graphD5);

            GraphData graphD6 = new GraphData();
            graphD6.Id = 6;
            graphD6.Name = "D6";
            graphDatas.Add(graphD6);

            GraphData graphD7 = new GraphData();
            graphD7.Id = 7;
            graphD7.Name = "D7";
            graphDatas.Add(graphD7);

            GraphData graphD8 = new GraphData();
            graphD8.Id = 8;
            graphD8.Name = "D8";
            graphDatas.Add(graphD8);

            return graphDatas;
        }

        public void ImplementChanges(int ChangeType)
        {
            if (ChangeType== 0 )
            {
                FrequencyCount frequencyCount = new FrequencyCount() { FC=1, C = FrequencyNo, NC = chNo };
                portCOM.WriteData(JsonConvert.SerializeObject(frequencyCount));

                Mode mode = new Mode() { FC = 2, M = 0 };
                portCOM.WriteData(JsonConvert.SerializeObject(mode));
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
                    ellipseWrite.ED = new List<Elliplse>();

                    foreach (GraphData graphData in ch.graphDatas)
                    {
                        if (ch.IsSeleted == true)
                        {
                            // Gdata.isEnable enable/disable the frequency graph 

                            if (graphData.Id == 1)
                            { 
                                if (graphData.isEnable)
                                {
                                    lblFreq1.Text = graphData.Name + "-" + graphData.freq + "Hz";

                                    el1.Height = graphData.height / factor;
                                    el1.Width = graphData.width / factor;
                                    tt1.X = ((graphData.ex - (graphData.width / 2)) / factor);
                                    tt1.Y = (((graphData.ey * -1) - (graphData.height / 2)) / factor);

                                    rtAngel1.CenterX = (el1.Width / 2);
                                    rtAngel1.CenterY = (el1.Height / 2);
                                    rtAngel1.Angle = graphData.angel;

                                    br1_rec1.Fill = enableColor;
                                    D1.IsEnabled = true;
                                    br1.IsEnabled = true;


                                }
                                else
                                {
                                    br1_rec1.Fill = disableColor;
                                    D1.IsEnabled = false;
                                    br1.IsEnabled = false;
                                }
                            }
                            else if (graphData.Id == 2 && graphData.Id <= FrequencyNo)
                            {
                                if (graphData.isEnable)
                                {
                                    lblFreq2.Text = graphData.Name + "-" + graphData.freq + "Hz";

                                    el2.Height = graphData.height / factor;
                                    el2.Width = graphData.width / factor;
                                    tt2.X = (graphData.ex - (graphData.width / 2)) / factor;
                                    tt2.Y = ((graphData.ey * -1) - (graphData.height / 2)) / factor;

                                    rtAngel2.CenterX = (el2.Width / 2);
                                    rtAngel2.CenterY = (el2.Height / 2);
                                    rtAngel2.Angle = graphData.angel;

                                    br2_rec1.Fill = enableColor;
                                    D2.IsEnabled = true;
                                    br2.IsEnabled = true;
                                }
                                else
                                {
                                    br2_rec1.Fill = disableColor;
                                    D2.IsEnabled = false;
                                    br2.IsEnabled = false;
                                }
                            }
                            else if (graphData.Id == 3 && graphData.Id <= FrequencyNo)
                            {
                                if (graphData.isEnable)
                                {
                                    lblFreq3.Text = graphData.Name + "-" + graphData.freq + "Hz";

                                    el3.Height = graphData.height / factor;
                                    el3.Width = graphData.width / factor;
                                    tt3.X = (graphData.ex - (graphData.width / 2)) / factor;
                                    tt3.Y = ((graphData.ey * -1) - (graphData.height / 2)) / factor;

                                    rtAngel3.CenterX = (el3.Width / 2);
                                    rtAngel3.CenterY = (el3.Height / 2);
                                    rtAngel3.Angle = graphData.angel;

                                    br3_rec1.Fill = enableColor;
                                    D3.IsEnabled = true;
                                    br3.IsEnabled = true;
                                }
                                else
                                {
                                    br3_rec1.Fill = disableColor;
                                    D3.IsEnabled = false;
                                    br3.IsEnabled = false;
                                }
                            }
                            else if (graphData.Id == 4 && graphData.Id <= FrequencyNo)
                            {
                                if (graphData.isEnable)
                                {
                                    lblFreq4.Text = graphData.Name + "-" + graphData.freq + "Hz";

                                    el4.Height = graphData.height / factor;
                                    el4.Width = graphData.width / factor;
                                    tt4.X = (graphData.ex - (graphData.width / 2)) / factor;
                                    tt4.Y = ((graphData.ey * -1) - (graphData.height / 2)) / factor;

                                    rtAngel4.CenterX = (el4.Width / 2);
                                    rtAngel4.CenterY = (el4.Height / 2);
                                    rtAngel4.Angle = graphData.angel;

                                    br4_rec1.Fill = enableColor;
                                    D4.IsEnabled = true;
                                    br4.IsEnabled = true;
                                }
                                else
                                {
                                    br4_rec1.Fill = disableColor;
                                    D4.IsEnabled = false;
                                    br4.IsEnabled = false;
                                }
                            }
                            else if (graphData.Id == 5 && graphData.Id <= FrequencyNo)
                            {
                                if (graphData.isEnable)
                                {

                                    lblFreq5.Text = graphData.Name + "-" + graphData.freq + "Hz";

                                    el5.Height = graphData.height / factor;
                                    el5.Width = graphData.width / factor;
                                    tt5.X = (graphData.ex - (graphData.width / 2)) / factor;
                                    tt5.Y = ((graphData.ey * -1) - (graphData.height / 2)) / factor;

                                    rtAngel5.CenterX = (el5.Width / 2);
                                    rtAngel5.CenterY = (el5.Height / 2);
                                    rtAngel5.Angle = graphData.angel;

                                    br5_rec1.Fill = enableColor;
                                    D5.IsEnabled = true;
                                    br5.IsEnabled = true;
                                }
                                else
                                {
                                    br5_rec1.Fill = disableColor;
                                    D5.IsEnabled = false;
                                    br5.IsEnabled = false;
                                }
                            }
                            else if (graphData.Id == 6 && graphData.Id <= FrequencyNo)
                            {
                                if (graphData.isEnable)
                                {
                                    lblFreq6.Text = graphData.Name + "-" + graphData.freq + "Hz";

                                    el6.Height = graphData.height / factor;
                                    el6.Width = graphData.width / factor;
                                    tt6.X = (graphData.ex - (graphData.width / 2)) / factor;
                                    tt6.Y = ((graphData.ey * -1) - (graphData.height / 2)) / factor;

                                    rtAngel6.CenterX = (el6.Width / 2);
                                    rtAngel6.CenterY = (el6.Height / 2);
                                    rtAngel6.Angle = graphData.angel;

                                    br6_rec1.Fill = enableColor;
                                    D6.IsEnabled = true;
                                    br6.IsEnabled = true;
                                }
                                else
                                {
                                    br6_rec1.Fill = disableColor;
                                    D6.IsEnabled = false;
                                    br6.IsEnabled = false;
                                }
                            }
                            else if (graphData.Id == 7 && graphData.Id <= FrequencyNo)
                            {
                                if (graphData.isEnable)
                                {
                                    lblFreq7.Text = graphData.Name + "-" + graphData.freq + "Hz";

                                    el7.Height = graphData.height / factor;
                                    el7.Width = graphData.width / factor;
                                    tt7.X = (graphData.ex - (graphData.width / 2)) / factor;
                                    tt7.Y = ((graphData.ey * -1) - (graphData.height / 2)) / factor;

                                    rtAngel7.CenterX = (el7.Width / 2);
                                    rtAngel7.CenterY = (el7.Height / 2);
                                    rtAngel7.Angle = graphData.angel;

                                    br7_rec1.Fill = enableColor;
                                    D7.IsEnabled = true;
                                    br7.IsEnabled = true;
                                }
                                else
                                {
                                    br7_rec1.Fill = disableColor;
                                    D7.IsEnabled = false;
                                    br7.IsEnabled = false;
                                }
                            }
                            else if (graphData.Id == 8 && graphData.Id <= FrequencyNo)
                            {
                                if (graphData.isEnable)
                                {
                                    lblFreq8.Text = graphData.Name + "-" + graphData.freq + "Hz";

                                    el8.Height = graphData.height / factor;
                                    el8.Width = graphData.width / factor;
                                    tt8.X = (graphData.ex - (graphData.width / 2)) / factor;
                                    tt8.Y = ((graphData.ey * -1) - (graphData.height / 2)) / factor;

                                    rtAngel8.CenterX = (el8.Width / 2);
                                    rtAngel8.CenterY = (el8.Height / 2);
                                    rtAngel8.Angle = graphData.angel;

                                    br8_rec1.Fill = enableColor;
                                    D8.IsEnabled = true;
                                    br8.IsEnabled = true;
                                }
                                else
                                {
                                    br8_rec1.Fill = disableColor;
                                    D8.IsEnabled = false;
                                    br8.IsEnabled = false;
                                }
                            }
                        }

                        if (ChangeType == 0 && graphData.Id <= FrequencyNo)
                        {
                            // write data to port for freq and setting
                            Frequency frequency = new Frequency() { FN = graphData.Id, F = graphData.freq, G = graphData.gain, P = graphData.phase, E = graphData.isEnable ? 1 : 0 };
                            frequencyWrite.FD.Add(frequency);


                            Elliplse elliplse = new Elliplse() { FN = graphData.Id, EId = graphData.Id, a = graphData.height, b = graphData.width, t = graphData.angel, x = graphData.ex, y = graphData.ey };
                            ellipseWrite.ED.Add(elliplse);
                        }
                    }

                    if (ChangeType == 0)
                    {
                        portCOM.WriteData(JsonConvert.SerializeObject(frequencyWrite));
                        //System.Threading.Thread.Sleep(500);
                        portCOM.WriteData(JsonConvert.SerializeObject(ellipseWrite));
                    }
                }
            }
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
                btnCh1.Background = new SolidColorBrush(Colors.DarkGray);
                btnCh2.Background = new SolidColorBrush(Colors.DarkGray);
                btnCh3.Background = new SolidColorBrush(Colors.DarkGray);
                btnCh4.Background = new SolidColorBrush(Colors.DarkGray);

                btnCh1.Background = new SolidColorBrush(Colors.Green);
                
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
                btnCh1.Background = new SolidColorBrush(Colors.DarkGray);
                btnCh2.Background = new SolidColorBrush(Colors.DarkGray);
                btnCh3.Background = new SolidColorBrush(Colors.DarkGray);
                btnCh4.Background = new SolidColorBrush(Colors.DarkGray);
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
                if (DeviceCOM.IsLogEnable)
                {
                    MessageBox.Show("While logging you can not perform this command, please stop the log.", "Command Conflict");
                }
                else
                {
                    var IsBalaneAll = (((Border)sender).Name == "btnBalance1All") || (((Border)sender).Name == "btnBalanceAll" ) || (((Border)sender).Name == "btnBalance2All");
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
                }
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
                    DeviceCOM.IsLogDisable = true;
                }
            }
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            var IsClearAll = (((Border)sender).Name == "btnClear1All") || (((Border)sender).Name == "btnClearAll") || (((Border)sender).Name == "btnClear2All");
            ClearGraphDataWithoutBalance(IsClearAll);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (CommunicationType == 0)
            {
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

            cn2.Children.Clear();
            rResult2.Fill = new SolidColorBrush(Colors.White);

            cn3.Children.Clear();
            rResult3.Fill = new SolidColorBrush(Colors.White);

            cn4.Children.Clear();
            rResult4.Fill = new SolidColorBrush(Colors.White);

            cn5.Children.Clear();
            rResult5.Fill = new SolidColorBrush(Colors.White);

            cn6.Children.Clear();
            rResult6.Fill = new SolidColorBrush(Colors.White);

            cn7.Children.Clear();
            rResult7.Fill = new SolidColorBrush(Colors.White);

            cn8.Children.Clear();
            rResult8.Fill = new SolidColorBrush(Colors.White);

            btnOverallResult.Background = new SolidColorBrush(Colors.LightBlue);
            btnOverallResult1.Background = new SolidColorBrush(Colors.LightBlue);

            lblGraphXY1.Text = "";
            lblGraphXY2.Text = "";
            lblGraphXY3.Text = "";
            lblGraphXY4.Text = "";
            lblGraphXY5.Text = "";
            lblGraphXY6.Text = "";
            lblGraphXY7.Text = "";
            lblGraphXY8.Text = "";
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
            else if (chId == 2)
            {
                cn2.Children.Clear();
                rResult2.Fill = new SolidColorBrush(Colors.White);
                lblGraphXY2.Text = "";
            }
            else if (chId == 3)
            {
                cn3.Children.Clear();
                rResult3.Fill = new SolidColorBrush(Colors.White);
                lblGraphXY3.Text = "";
            }
            else if (chId == 4)
            {
                cn4.Children.Clear();
                rResult4.Fill = new SolidColorBrush(Colors.White);
                lblGraphXY4.Text = "";
            }
            else if (chId == 5)
            {
                cn5.Children.Clear();
                rResult5.Fill = new SolidColorBrush(Colors.White);
                lblGraphXY5.Text = "";
            }
            else if (chId == 6)
            {
                cn6.Children.Clear();
                rResult6.Fill = new SolidColorBrush(Colors.White);
                lblGraphXY6.Text = "";
            }
            else if (chId == 7)
            {
                cn7.Children.Clear();
                rResult7.Fill = new SolidColorBrush(Colors.White);
                lblGraphXY7.Text = "";
            }
            else if (chId == 8)
            {
                cn8.Children.Clear();
                rResult8.Fill = new SolidColorBrush(Colors.White);
                lblGraphXY8.Text = "";
            }

            btnOverallResult.Background = new SolidColorBrush(Colors.LightBlue);
            btnOverallResult1.Background = new SolidColorBrush(Colors.LightBlue);
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
                    var left = fd.X / factor;
                    var top = (fd.Y * -1) / factor;
                    if (left > (seqLength /2))
                    {
                        left = (seqLength / 2);
                    }
                    if (top > (seqLength / 2))
                    {
                        top = (seqLength / 2);
                    }

                    if (left < ((seqLength / 2)* -1))
                    {
                        left = ((seqLength / 2) * -1);
                    }
                    if (top < ((seqLength / 2) * -1))
                    {
                        top = ((seqLength / 2) * -1);
                    } 
                    Canvas.SetLeft(el1, left-2);
                    Canvas.SetTop(el1, top-2);
                    //r1.Stroke = new SolidColorBrush(Colors.Black);
                    if (selectedChannelData.IndexOf(item) == selectedChannelData.Count-1)
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
                                btnOverallResult.Background = new SolidColorBrush(Colors.Green);
                                btnOverallResult1.Background = new SolidColorBrush(Colors.Green);
                            }
                            else
                            {
                                btnOverallResult.Background = new SolidColorBrush(Colors.Red);
                                btnOverallResult1.Background = new SolidColorBrush(Colors.Red);
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
                    else if (fd.FN == 2)
                    {
                        cn2.Children.Add(el1);
                        
                        if (!item.IsBalacenced)
                        {
                            lblGraphXY2.Text = fd.X.ToString() + "," + fd.Y.ToString();
                            if (fd.R == 1)
                            {
                                rResult2.Fill = new SolidColorBrush(Colors.Green);
                            }
                            else
                            {
                                rResult2.Fill = new SolidColorBrush(Colors.Red);
                            }
                        }
                    }
                    else if (fd.FN == 3)
                    {
                        cn3.Children.Add(el1);
                        
                        if (!item.IsBalacenced)
                        {
                            lblGraphXY3.Text = fd.X.ToString() + "," + fd.Y.ToString();
                            if (fd.R == 1)
                            {
                                rResult3.Fill = new SolidColorBrush(Colors.Green);
                            }
                            else
                            {
                                rResult3.Fill = new SolidColorBrush(Colors.Red);
                            }
                        }
                    }
                    else if (fd.FN == 4)
                    {
                        cn4.Children.Add(el1);
                        
                        if (!item.IsBalacenced)
                        {
                            lblGraphXY4.Text = fd.X.ToString() + "," + fd.Y.ToString();
                            if (fd.R == 1)
                            {
                                rResult4.Fill = new SolidColorBrush(Colors.Green);
                            }
                            else
                            {
                                rResult4.Fill = new SolidColorBrush(Colors.Red);
                            }
                        }
                    }
                    else if (fd.FN == 5)
                    {
                        cn5.Children.Add(el1);
                        
                        if (!item.IsBalacenced)
                        {
                            lblGraphXY5.Text = fd.X.ToString() + "," + fd.Y.ToString();
                            if (fd.R == 1)
                            {
                                rResult5.Fill = new SolidColorBrush(Colors.Green);
                            }
                            else
                            {
                                rResult5.Fill = new SolidColorBrush(Colors.Red);
                            }
                        }
                    }
                    else if (fd.FN == 6)
                    {
                        cn6.Children.Add(el1);
                        
                        if (!item.IsBalacenced)
                        {
                            lblGraphXY6.Text = fd.X.ToString() + "," + fd.Y.ToString();
                            if (fd.R == 1)
                            {
                                rResult6.Fill = new SolidColorBrush(Colors.Green);
                            }
                            else
                            {
                                rResult6.Fill = new SolidColorBrush(Colors.Red);
                            }
                        }
                    }
                    else if (fd.FN == 7)
                    {
                        cn7.Children.Add(el1);
                        
                        if (!item.IsBalacenced)
                        {
                            lblGraphXY7.Text = fd.X.ToString() + "," + fd.Y.ToString();
                            if (fd.R == 1)
                            {
                                rResult7.Fill = new SolidColorBrush(Colors.Green);
                            }
                            else
                            {
                                rResult7.Fill = new SolidColorBrush(Colors.Red);
                            }
                        }
                    }
                    else if (fd.FN == 8)
                    {
                        cn8.Children.Add(el1);
                        
                        if (!item.IsBalacenced)
                        {
                            lblGraphXY8.Text = fd.X.ToString() + "," + fd.Y.ToString();
                            if (fd.R == 1)
                            {
                                rResult8.Fill = new SolidColorBrush(Colors.Green);
                            }
                            else
                            {
                                rResult8.Fill = new SolidColorBrush(Colors.Red);
                            }
                        }
                    }
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
            
            lblTCount.Content = "Total Count - " + cnt.ResultCount.ToString();
            lblOkCount.Content = "OK Count - " + cnt.ResultOkCount.ToString();
            lblNotOkCount.Content = "Not Ok Count - " + cnt.ResultOkNotCount.ToString();

            lblTCount1.Content = "Total Count - " + cnt.ResultCount.ToString();
            lblOkCount1.Content = "OK Count - " + cnt.ResultOkCount.ToString();
            lblNotOkCount1.Content = "Not Ok Count - " + cnt.ResultOkNotCount.ToString();

            lblTCount2.Content = "Total Count - " + cnt.ResultCount.ToString();
            lblOkCount2.Content = "OK Count - " + cnt.ResultOkCount.ToString();
            lblNotOkCount2.Content = "Not Ok Count - " + cnt.ResultOkNotCount.ToString();

        }

        private void btnLog_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (DeviceCOM.IsLogEnable)
            {
                DeviceCOM.IsLogEnable = false;
                lblLog.Content = "Start Log";
                lblLog1.Content = "Start Log";
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
                lblLog.Content = "Stop Log";
                lblLog1.Content = "Stop Log";
                lblLog2.Content = "Stop Log";
                lblPartLogs.Content = DeviceCOM.part.Name + ", " + DeviceCOM.part.Grade + ", " + DeviceCOM.part.CompanyName;
            }
            else
            {
                lblPartLogs.Content = "";
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
                            mainWindow.ImplementChanges(0);
                            MessageBox.Show("Configuation Write successfully!!", "Information");
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
                        mainWindow.ImplementChanges(0);
                        MessageBox.Show("Channel-1 Configuration copied to others successfully!!", "Information");

                    }
                    else if ( Header == "Data Log")
                    {
                        //mainWindow.report = new Report();
                        //mainWindow.report.ShowDialog();

                        System.Diagnostics.Process.Start(new ProcessStartInfo
                        {
                            FileName = this.mainWindow.WebPage,
                            UseShellExecute = true
                        });

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
                                    this.mainWindow.btnLog.Visibility = Visibility.Visible;
                                    this.mainWindow.btnLog1.Visibility = Visibility.Visible;
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

                                mainWindow.ImplementChanges(0);
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
                        mainWindow.ImplementChanges(0);
                        DeviceCOM.IsLogEnable = false;
                        this.mainWindow.lblLog.Content = "Start Log";
                        this.mainWindow.lblLog1.Content = "Start Log";
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
                    else if (Header == "View Log")
                    {
                        Logs logs = new Logs();
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

}

