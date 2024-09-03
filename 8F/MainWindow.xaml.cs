using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.IO.Ports;
using System.Reflection;
using System.Text;
using System.Threading.Channels;
using System.Windows;
using System.Windows.Controls;
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
        public PortCOM portCOM;
        DispatcherTimer dispatcherTimer;
        int chNo;
        int factor = 20;
        public MainWindow()
        {
            InitializeComponent();
            portCOM = new PortCOM();
            string portName = Convert.ToString(System.Configuration.ConfigurationSettings.AppSettings["PortName"]);
            int baudRate = Convert.ToInt32(System.Configuration.ConfigurationSettings.AppSettings["BaudRate"]);
            portCOM.InitialPort(portName, baudRate);
            PortCOM.responses = new List<Response>();
            chNo = Convert.ToInt16(System.Configuration.ConfigurationSettings.AppSettings["Channel"]);
            PortCOM.ChannelNo = chNo;
            if (chNo == 1)
            {
                btnCh1.Visibility = Visibility.Visible;
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
                            new MenuItemViewModel { Header = "Save",  },
                            new MenuItemViewModel { Header = "Save As" },
                            new MenuItemViewModel { Header = "Exit" ,mainWindow =this }
                        }
                },
                new MenuItemViewModel { Header = "Configuration",
                    MenuItems = new ObservableCollection<MenuItemViewModel>
                        {
                            new MenuItemViewModel { Header = "Change Configuration", mainWindow = this },
                            new MenuItemViewModel { Header = "Threshold Setting", mainWindow = this },
                            new MenuItemViewModel { Header = "Write Configuration", mainWindow = this },
                        }
                },
            };
            DataContext = this;

            InitialGraphData(true);
            ImplementChanges(0);

            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(10000000);
            dispatcherTimer.Start();
           
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            if (PortCOM.IsResponseRefreshRequired)
            {
                RefreshResponse();
                lblTCount.Content = "Total Count - " + PortCOM.ResultCount.ToString();
                lblOkCount.Content = "OK Count - " + PortCOM.ResultOkCount.ToString();
                lblNotOkCount.Content = "Not Ok Count - " + PortCOM.ResultOkNotCount.ToString();
                PortCOM.IsResponseRefreshRequired = false;
            }

            if (PortCOM.IsResponseClearRequired)
            {
                ClearGraphData();
            }
        }

        public void InitialGraphData(bool IsPayLaod )
        {
            if (IsPayLaod)
            {
                ClearGraphData();

                for (int i = 10; i < 248; i = i + 10)
                {
                    Rectangle r1 = new Rectangle();
                    r1.Height = .2;
                    r1.Width = 248;
                    Canvas.SetLeft(r1, 0);
                    Canvas.SetTop(r1, i);
                    r1.Stroke = new SolidColorBrush(Colors.Black);
                    r1.Fill = new SolidColorBrush(Colors.LightGray);
                    Canvas1.Children.Add(r1);

                    Rectangle r2 = new Rectangle();
                    r2.Height = .2;
                    r2.Width = 248;
                    Canvas.SetLeft(r2, 0);
                    Canvas.SetTop(r2, i);
                    r2.Stroke = new SolidColorBrush(Colors.Black);
                    r2.Fill = new SolidColorBrush(Colors.LightGray);
                    Canvas2.Children.Add(r2);

                    Rectangle r3 = new Rectangle();
                    r3.Height = .2;
                    r3.Width = 248;
                    Canvas.SetLeft(r3, 0);
                    Canvas.SetTop(r3, i);
                    r3.Stroke = new SolidColorBrush(Colors.Black);
                    r3.Fill = new SolidColorBrush(Colors.LightGray);
                    Canvas3.Children.Add(r3);

                    Rectangle r4 = new Rectangle();
                    r4.Height = .2;
                    r4.Width = 248;
                    Canvas.SetLeft(r4, 0);
                    Canvas.SetTop(r4, i);
                    r4.Stroke = new SolidColorBrush(Colors.Black);
                    r4.Fill = new SolidColorBrush(Colors.LightGray);
                    Canvas4.Children.Add(r4);

                    Rectangle r5 = new Rectangle();
                    r5.Height = .2;
                    r5.Width = 248;
                    Canvas.SetLeft(r5, 0);
                    Canvas.SetTop(r5, i);
                    r5.Stroke = new SolidColorBrush(Colors.Black);
                    r5.Fill = new SolidColorBrush(Colors.LightGray);
                    Canvas5.Children.Add(r5);

                    Rectangle r6 = new Rectangle();
                    r6.Height = .2;
                    r6.Width = 248;
                    Canvas.SetLeft(r6, 0);
                    Canvas.SetTop(r6, i);
                    r6.Stroke = new SolidColorBrush(Colors.Black);
                    r6.Fill = new SolidColorBrush(Colors.LightGray);
                    Canvas6.Children.Add(r6);

                    Rectangle r7 = new Rectangle();
                    r7.Height = .2;
                    r7.Width = 248;
                    Canvas.SetLeft(r7, 0);
                    Canvas.SetTop(r7, i);
                    r7.Stroke = new SolidColorBrush(Colors.Black);
                    r7.Fill = new SolidColorBrush(Colors.LightGray);
                    Canvas7.Children.Add(r7);

                    Rectangle r8 = new Rectangle();
                    r8.Height = .2;
                    r8.Width = 248;
                    Canvas.SetLeft(r8, 0);
                    Canvas.SetTop(r8, i);
                    r8.Stroke = new SolidColorBrush(Colors.Black);
                    r8.Fill = new SolidColorBrush(Colors.LightGray);
                    Canvas8.Children.Add(r8);

                    Rectangle rectangle1 = new Rectangle();
                    rectangle1.Height = 250;
                    rectangle1.Width = .1;
                    Canvas.SetLeft(rectangle1, i);
                    Canvas.SetTop(rectangle1, 0);
                    rectangle1.Stroke = new SolidColorBrush(Colors.Black);
                    rectangle1.Fill = new SolidColorBrush(Colors.LightGray);


                    Rectangle rr1 = new Rectangle();
                    rr1.Height = 250;
                    rr1.Width = .1;
                    Canvas.SetLeft(rr1, i);
                    Canvas.SetTop(rr1, 0);
                    rr1.Stroke = new SolidColorBrush(Colors.Black);
                    rr1.Fill = new SolidColorBrush(Colors.LightGray);
                    Canvas1.Children.Add(rr1);

                    Rectangle rr2 = new Rectangle();
                    rr2.Height = 250;
                    rr2.Width = .1;
                    Canvas.SetLeft(rr2, i);
                    Canvas.SetTop(rr2, 0);
                    rr2.Stroke = new SolidColorBrush(Colors.Black);
                    rr2.Fill = new SolidColorBrush(Colors.LightGray);
                    Canvas2.Children.Add(rr2);

                    Rectangle rr3 = new Rectangle();
                    rr3.Height = 250;
                    rr3.Width = .1;
                    Canvas.SetLeft(rr3, i);
                    Canvas.SetTop(rr3, 0);
                    rr3.Stroke = new SolidColorBrush(Colors.Black);
                    rr3.Fill = new SolidColorBrush(Colors.LightGray);
                    Canvas3.Children.Add(rr3);

                    Rectangle rr4 = new Rectangle();
                    rr4.Height = 250;
                    rr4.Width = .1;
                    Canvas.SetLeft(rr4, i);
                    Canvas.SetTop(rr4, 0);
                    rr4.Stroke = new SolidColorBrush(Colors.Black);
                    rr4.Fill = new SolidColorBrush(Colors.LightGray);
                    Canvas4.Children.Add(rr4);

                    Rectangle rr5 = new Rectangle();
                    rr5.Height = 250;
                    rr5.Width = .1;
                    Canvas.SetLeft(rr5, i);
                    Canvas.SetTop(rr5, 0);
                    rr5.Stroke = new SolidColorBrush(Colors.Black);
                    rr5.Fill = new SolidColorBrush(Colors.LightGray);
                    Canvas5.Children.Add(rr5);

                    Rectangle rr6 = new Rectangle();
                    rr6.Height = 250;
                    rr6.Width = .1;
                    Canvas.SetLeft(rr6, i);
                    Canvas.SetTop(rr6, 0);
                    rr6.Stroke = new SolidColorBrush(Colors.Black);
                    rr6.Fill = new SolidColorBrush(Colors.LightGray);
                    Canvas6.Children.Add(rr6);

                    Rectangle rr7 = new Rectangle();
                    rr7.Height = 250;
                    rr7.Width = .1;
                    Canvas.SetLeft(rr7, i);
                    Canvas.SetTop(rr7, 0);
                    rr7.Stroke = new SolidColorBrush(Colors.Black);
                    rr7.Fill = new SolidColorBrush(Colors.LightGray);
                    Canvas7.Children.Add(rr7);

                    Rectangle rr8 = new Rectangle();
                    rr8.Height = 250;
                    rr8.Width = .1;
                    Canvas.SetLeft(rr8, i);
                    Canvas.SetTop(rr8, 0);
                    rr8.Stroke = new SolidColorBrush(Colors.Black);
                    rr8.Fill = new SolidColorBrush(Colors.LightGray);
                    Canvas8.Children.Add(rr8);
                }
            }
            PortCOM.channelDatas = new List<ChannelData>();

            ChannelData channelData = new ChannelData();
            channelData.Id = 1;
            channelData.IsSeleted = true;
            channelData.graphDatas = IniGdata();
            PortCOM.channelDatas.Add(channelData);

            ChannelData channelData1 = new ChannelData();
            channelData1.Id = 2;
            channelData1.graphDatas = IniGdata();
            PortCOM.channelDatas.Add(channelData1);

            ChannelData channelData2 = new ChannelData();
            channelData2.Id = 3;
            channelData2.graphDatas = IniGdata();
            PortCOM.channelDatas.Add(channelData2);

            ChannelData channelData3 = new ChannelData();
            channelData3.Id = 4;
            channelData3.graphDatas = IniGdata();
            PortCOM.channelDatas.Add(channelData3);

            btnCh1.Background = new SolidColorBrush(Colors.DarkGray);
            btnCh2.Background = new SolidColorBrush(Colors.DarkGray);
            btnCh3.Background = new SolidColorBrush(Colors.DarkGray);
            btnCh4.Background = new SolidColorBrush(Colors.DarkGray);

            btnCh1.Background = new SolidColorBrush(Colors.LightGreen);
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
            if (ChangeType== 0)
            {
                FrequencyCount frequencyCount = new FrequencyCount() { FC=1, C = 8, NC = chNo };
                portCOM.WriteData(JsonConvert.SerializeObject(frequencyCount));

                Mode mode = new Mode() { FC = 2, M = 0 };
                portCOM.WriteData(JsonConvert.SerializeObject(mode));
            }

            foreach (var ch in PortCOM.channelDatas)
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
                            if (graphData.Id == 1)
                            {
                                lblFreq1.Text = graphData.Name + "-" + graphData.freq + "Hz";

                                el1.Height = graphData.height / factor;
                                el1.Width = graphData.width / factor;
                                tt1.X = ((graphData.ex - (graphData.width / 2)) / factor);
                                tt1.Y = (((graphData.ey * -1) - (graphData.height / 2)) / factor);
                                
                                rtAngel1.CenterX = (el1.Width / 2);
                                rtAngel1.CenterY = (el1.Height / 2);
                                rtAngel1.Angle = graphData.angel;
                            }
                            else if (graphData.Id == 2)
                            {
                                lblFreq2.Text = graphData.Name + "-" + graphData.freq + "Hz";

                                el2.Height = graphData.height / factor;
                                el2.Width = graphData.width / factor;
                                tt2.X = (graphData.ex - (graphData.width / 2)) / factor;
                                tt2.Y = ((graphData.ey * -1) - (graphData.height / 2)) / factor;
                                
                                rtAngel2.CenterX = (el2.Width / 2);
                                rtAngel2.CenterY = (el2.Height / 2);
                                rtAngel2.Angle = graphData.angel;
                            }
                            else if (graphData.Id == 3)
                            {
                                lblFreq3.Text = graphData.Name + "-" + graphData.freq + "Hz";

                                el3.Height = graphData.height / factor;
                                el3.Width = graphData.width / factor;
                                tt3.X = (graphData.ex - (graphData.width / 2)) / factor;
                                tt3.Y = ((graphData.ey * -1) - (graphData.height / 2)) / factor;
                                
                                rtAngel3.CenterX = (el3.Width / 2);
                                rtAngel3.CenterY = (el3.Height / 2);
                                rtAngel3.Angle = graphData.angel;
                            }
                            else if (graphData.Id == 4)
                            {
                                lblFreq4.Text = graphData.Name + "-" + graphData.freq + "Hz";

                                el4.Height = graphData.height / factor;
                                el4.Width = graphData.width / factor;
                                tt4.X = (graphData.ex - (graphData.width / 2)) / factor;
                                tt4.Y = ((graphData.ey * -1) - (graphData.height / 2)) / factor;
                                
                                rtAngel4.CenterX = (el4.Width / 2);
                                rtAngel4.CenterY = (el4.Height / 2);
                                rtAngel4.Angle = graphData.angel;
                            }
                            else if (graphData.Id == 5)
                            {
                                lblFreq5.Text = graphData.Name + "-" + graphData.freq + "Hz";

                                el5.Height = graphData.height / factor;
                                el5.Width = graphData.width / factor;
                                tt5.X = (graphData.ex - (graphData.width / 2)) / factor;
                                tt5.Y = ((graphData.ey * -1) - (graphData.height / 2)) / factor;
                                
                                rtAngel5.CenterX = (el5.Width / 2);
                                rtAngel5.CenterY = (el5.Height / 2);
                                rtAngel5.Angle = graphData.angel;
                            }
                            else if (graphData.Id == 6)
                            {
                                lblFreq6.Text = graphData.Name + "-" + graphData.freq + "Hz";

                                el6.Height = graphData.height / factor;
                                el6.Width = graphData.width / factor;
                                tt6.X = (graphData.ex - (graphData.width / 2)) / factor;
                                tt6.Y = ((graphData.ey * -1) - (graphData.height / 2)) / factor;
                                
                                rtAngel6.CenterX = (el6.Width / 2);
                                rtAngel6.CenterY = (el6.Height / 2);
                                rtAngel6.Angle = graphData.angel;
                            }
                            else if (graphData.Id == 7)
                            {
                                lblFreq7.Text = graphData.Name + "-" + graphData.freq + "Hz";

                                el7.Height = graphData.height / factor;
                                el7.Width = graphData.width / factor;
                                tt7.X = (graphData.ex - (graphData.width / 2)) / factor;
                                tt7.Y = ((graphData.ey * -1) - (graphData.height / 2)) / factor;
                                
                                rtAngel7.CenterX = (el7.Width / 2);
                                rtAngel7.CenterY = (el7.Height / 2);
                                rtAngel7.Angle = graphData.angel;
                            }
                            else if (graphData.Id == 8)
                            {
                                lblFreq8.Text = graphData.Name + "-" + graphData.freq + "Hz";

                                el8.Height = graphData.height / factor;
                                el8.Width = graphData.width / factor;
                                tt8.X = (graphData.ex - (graphData.width / 2)) / factor;
                                tt8.Y = ((graphData.ey * -1) - (graphData.height / 2)) / factor;
                                
                                rtAngel8.CenterX = (el8.Width / 2);
                                rtAngel8.CenterY = (el8.Height / 2);
                                rtAngel8.Angle = graphData.angel;
                            }
                        }

                        if (ChangeType == 0)
                        {
                            // write data to port for freq and setting
                            Frequency frequency = new Frequency() { FN = graphData.Id, F = graphData.freq, G = graphData.gain, P = graphData.phase };
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
            

            var currentChannel = PortCOM.channelDatas.FirstOrDefault(c => c.IsSeleted == true);
            if (currentChannel?.Id != 1)
            {
                currentChannel.IsSeleted = false;
                var nextCh = PortCOM.channelDatas.FirstOrDefault(c => c.Id == 1);
                nextCh.IsSeleted = true;
                btnCh1.Background = new SolidColorBrush(Colors.DarkGray);
                btnCh2.Background = new SolidColorBrush(Colors.DarkGray);
                btnCh3.Background = new SolidColorBrush(Colors.DarkGray);
                btnCh4.Background = new SolidColorBrush(Colors.DarkGray);

                btnCh1.Background = new SolidColorBrush(Colors.LightGreen);
                
            }
        }

        private void btnCh_Click(object sender, RoutedEventArgs e)
        {
            var chId = Convert.ToUInt32(((Button)sender).Tag);
            var currentChannel = PortCOM.channelDatas.FirstOrDefault(c => c.IsSeleted == true);
            if (currentChannel?.Id != chId)
            {
                currentChannel.IsSeleted = false;
                var nextCh = PortCOM.channelDatas.FirstOrDefault(c => c.Id == chId);
                nextCh.IsSeleted = true;
                btnCh1.Background = new SolidColorBrush(Colors.DarkGray);
                btnCh2.Background = new SolidColorBrush(Colors.DarkGray);
                btnCh3.Background = new SolidColorBrush(Colors.DarkGray);
                btnCh4.Background = new SolidColorBrush(Colors.DarkGray);
                ((Button)sender).Background = new SolidColorBrush(Colors.LightGreen);
                ImplementChanges(1);
                PortCOM.IsResponseRefreshRequired = true;
            }
        }

        private void btnBalance_Click(object sender, RoutedEventArgs e)
        {
            BalanceTest balanceTest = new BalanceTest() { FC = 16, CN = 0 };
            portCOM.WriteData(JsonConvert.SerializeObject(balanceTest));

            ClearGraphData();
        }

        private void btnTest_Click(object sender, RoutedEventArgs e)
        {
            BalanceTest balanceTest = new BalanceTest() { FC = 17, CN = 0 };
            portCOM.WriteData(JsonConvert.SerializeObject(balanceTest));
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            ClearGraphData();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (portCOM.port.IsOpen)
                portCOM.port.Close();
        }
        
        public void ClearGraphData(bool IsDataClear = true)
        {
            if (IsDataClear)
            {
                PortCOM.responses = new List<Response>();
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

            lblGraphXY1.Text = "";
            lblGraphXY2.Text = "";
            lblGraphXY3.Text = "";
            lblGraphXY4.Text = "";
            lblGraphXY5.Text = "";
            lblGraphXY6.Text = "";
            lblGraphXY7.Text = "";
            lblGraphXY8.Text = "";
        }

        public void RefreshResponse()
        {
            ClearGraphData(false);
            var selectedChannel = PortCOM.channelDatas.FirstOrDefault(c => c.IsSeleted);
            var selectedChannelData = PortCOM.responses.Where(r => r.CN == selectedChannel.Id).ToList();
            foreach (var item in selectedChannelData)
            {
                foreach (var fd in item.FD)
                {
                    Ellipse el1 = new Ellipse();
                    el1.Height = 4;
                    el1.Width = 4;
                    var left = fd.X / factor;
                    var top = (fd.Y * -1) / factor;
                    if (left >125 )
                    {
                        left = 125;
                    }
                    if (top > 125)
                    {
                        top = 125;
                    }
                    Canvas.SetLeft(el1, left);
                    Canvas.SetTop(el1, top);
                    //r1.Stroke = new SolidColorBrush(Colors.Black);
                    if (selectedChannelData.IndexOf(item) == selectedChannelData.Count-1)
                    {
                        el1.Fill = new SolidColorBrush(Colors.Blue);
                        if (item.OR == 1)
                        {
                            btnOverallResult.Background = new SolidColorBrush(Colors.Green);
                        }
                        else
                        {
                            btnOverallResult.Background = new SolidColorBrush(Colors.Red);
                        }
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
                    
                    if (fd.FN == 1)
                    {
                        cn1.Children.Add(el1);
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
                    else if (fd.FN == 2)
                    {
                        cn2.Children.Add(el1);
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
                    else if (fd.FN == 3)
                    {
                        cn3.Children.Add(el1);
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
                    else if (fd.FN == 4)
                    {
                        cn4.Children.Add(el1);
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
                    else if (fd.FN == 5)
                    {
                        cn5.Children.Add(el1);
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
                    else if (fd.FN == 6)
                    {
                        cn6.Children.Add(el1);
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
                    else if (fd.FN == 7)
                    {
                        cn7.Children.Add(el1);
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
                    else if (fd.FN == 8)
                    {
                        cn8.Children.Add(el1);
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

        private void btnResetCounter_Click(object sender, RoutedEventArgs e)
        {
            PortCOM.ResultCount = 0;
            PortCOM.ResultOkCount = 0;
            PortCOM.ResultOkNotCount = 0;

            lblTCount.Content = "Total Count - " + PortCOM.ResultCount.ToString();
            lblOkCount.Content = "OK Count - " + PortCOM.ResultOkCount.ToString();
            lblNotOkCount.Content = "Not Ok Count - " + PortCOM.ResultOkNotCount.ToString();

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
            if (Header == "Change Configuration")
            {
                freqPop = new Freq();
                freqPop.Closing += freqPop_Closing;
                freqPop.portCOM = mainWindow.portCOM;
                freqPop.ShowDialog();
            }
            else if (Header == "Threshold Setting")
            {
                ellipsesPop = new CircleSetting("D1");
                ellipsesPop.Closing += ellipsesPop_Closing;
                ellipsesPop.portCOM = mainWindow.portCOM;
                ellipsesPop.ShowDialog();
            }
            else if (Header == "Write Configuration")
            {
                try
                {
                    mainWindow.ImplementChanges(0);
                    MessageBox.Show("Configuation Write successfully!!");
                }
                catch(Exception ex)
                {
                    MessageBox.Show("Error while writing the configuration!!!!");
                }
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

                            string conecnt = JsonConvert.SerializeObject(PortCOM.channelDatas);
                            File.WriteAllText(filename, conecnt);

                            //MessageBox.Show("Configuation changes saved at '" + filename + "'!!!!");
                        }

                    } else
                    {
                        string conecnt = JsonConvert.SerializeObject(PortCOM.channelDatas);
                        File.WriteAllText(filename, conecnt);

                        //MessageBox.Show("Configuation changes saved at '" + filename + "'!!!!");
                    }
                    
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error while saving the configation file!!!!");
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

                        string conecnt = JsonConvert.SerializeObject(PortCOM.channelDatas);
                        File.WriteAllText(filename, conecnt);

                        //MessageBox.Show("Configuation changes saved at '" + filename + "'!!!!");
                    }

                    
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error while saving the configuration file!!!!");
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
                        PortCOM.channelDatas = JsonConvert.DeserializeObject<List<ChannelData>>(data);
                        // Open document
                        filename = dialog.FileName;
                        mainWindow.SelectCh1();
                        mainWindow.ClearGraphData();
                        
                        mainWindow.ImplementChanges(0);
                    }

                    
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error while loading the configuration file!!!!");
                }
            }

            else if (Header == "New")
            {
                filename = null;
                mainWindow.InitialGraphData(false);
                mainWindow.ClearGraphData();                
                mainWindow.ImplementChanges(0);
            }

            else if (Header == "Exit")
            {
                mainWindow.Close();
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

