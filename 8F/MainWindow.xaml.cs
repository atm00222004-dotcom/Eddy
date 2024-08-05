using System;
using System.Collections.ObjectModel;
using System.Text;
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

namespace _8F
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    { 
        public ObservableCollection<MenuItemViewModel> MenuItems { get; set; }
        public MainWindow()
        {
            InitializeComponent();

            MenuItems = new ObservableCollection<MenuItemViewModel>
            {
                new MenuItemViewModel { Header = "File",
                    MenuItems = new ObservableCollection<MenuItemViewModel>
                        {
                            new MenuItemViewModel { Header = "New" },
                            new MenuItemViewModel { Header = "Open" },
                            new MenuItemViewModel { Header = "Save" },
                            new MenuItemViewModel { Header = "Save As" },
                            new MenuItemViewModel { Header = "Exit" }
                        }
                },
                new MenuItemViewModel { Header = "Configuration",
                    MenuItems = new ObservableCollection<MenuItemViewModel>
                        {
                            new MenuItemViewModel { Header = "Change Configuration", mainWindow = this },
                        }
                },
            };
            DataContext = this;

            InitialGraphData();
            ImplementChanges();

            //PortCOM portCOM = new PortCOM();
            //portCOM.InitialPort("COM4");
            //portCOM.ReadFreqAndGain();

            //portCOM.WriteBalance();
            //portCOM.ReadGraphData();
            //portCOM.WriteFreqAndGain("1", "03590", "45");
        }

        public void InitialGraphData()
        {

            for(int i = 10; i < 248; i = i+10)
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

            PortCOM.graphDatas = new List<GraphData>();

            GraphData graphD1 = new GraphData();
            graphD1.Id = 1;
            graphD1.Name = "D1";
            PortCOM.graphDatas.Add(graphD1);

            GraphData graphD2 = new GraphData();
            graphD2.Id = 2;
            graphD2.Name = "D2";
            PortCOM.graphDatas.Add(graphD2);

            GraphData graphD3 = new GraphData();
            graphD3.Id = 3;
            graphD3.Name = "D3";
            PortCOM.graphDatas.Add(graphD3);

            GraphData graphD4 = new GraphData();
            graphD4.Id = 4;
            graphD4.Name = "D4";
            PortCOM.graphDatas.Add(graphD4);

            GraphData graphD5 = new GraphData();
            graphD5.Id = 5;
            graphD5.Name = "D5";
            PortCOM.graphDatas.Add(graphD5);

            GraphData graphD6 = new GraphData();
            graphD6.Id = 6;
            graphD6.Name = "D6";
            PortCOM.graphDatas.Add(graphD6);

            GraphData graphD7 = new GraphData();
            graphD7.Id = 7;
            graphD7.Name = "D7";
            PortCOM.graphDatas.Add(graphD7);

            GraphData graphD8 = new GraphData();
            graphD8.Id = 8;
            graphD8.Name = "D8";
            PortCOM.graphDatas.Add(graphD8);

        }

        public void ImplementChanges()
        {
            foreach(GraphData graphData in PortCOM.graphDatas)
            {
                if (graphData.Id ==1 )
                {
                    lblFreq1.Text = graphData.Name +"-" + graphData.freq + "Hz," + graphData.gain + "dBP," + graphData.phase + "bD";
                    
                    el1.Height = graphData.height;
                    el1.Width = graphData.width;
                    tt1.X = graphData.ex;
                    tt1.Y = graphData.ey;
                    rtAngel1.Angle = graphData.angel;

                    // Data to Port;
                }
                else if (graphData.Id == 2)
                {
                    lblFreq2.Text = graphData.Name +"-" + graphData.freq + "Hz," + graphData.gain + "dBP," + graphData.phase + "bD";

                    el2.Height = graphData.height;
                    el2.Width = graphData.width;
                    tt2.X = graphData.ex;
                    tt2.Y = graphData.ey;
                    rtAngel2.Angle = graphData.angel;

                    // Data to Port;
                }

                else if (graphData.Id == 3)
                {
                    lblFreq3.Text = graphData.Name + "-" + graphData.freq + "Hz," + graphData.gain + "dBP," + graphData.phase + "bD";

                    el3.Height = graphData.height;
                    el3.Width = graphData.width;
                    tt3.X = graphData.ex;
                    tt3.Y = graphData.ey;
                    rtAngel3.Angle = graphData.angel;

                    // Data to Port;
                }

                else if (graphData.Id == 4)
                {
                    lblFreq4.Text = graphData.Name + "-" + graphData.freq + "Hz," + graphData.gain + "dBP," + graphData.phase + "bD";

                    el4.Height = graphData.height;
                    el4.Width = graphData.width;
                    tt4.X = graphData.ex;
                    tt4.Y = graphData.ey;
                    rtAngel4.Angle = graphData.angel;

                    // Data to Port;
                }

                else if (graphData.Id == 5)
                {
                    lblFreq5.Text = graphData.Name + "-" + graphData.freq + "Hz," + graphData.gain + "dBP," + graphData.phase + "bD";

                    el5.Height = graphData.height;
                    el5.Width = graphData.width;
                    tt5.X = graphData.ex;
                    tt5.Y = graphData.ey;
                    rtAngel5.Angle = graphData.angel;

                    // Data to Port;
                }
                else if (graphData.Id == 6)
                {
                    lblFreq6.Text = graphData.Name + "-" + graphData.freq + "Hz," + graphData.gain + "dBP," + graphData.phase + "bD";

                    el6.Height = graphData.height;
                    el6.Width = graphData.width;
                    tt6.X = graphData.ex;
                    tt6.Y = graphData.ey;
                    rtAngel6.Angle = graphData.angel;

                    // Data to Port;
                }
                else if (graphData.Id == 7)
                {
                    lblFreq7.Text = graphData.Name + "-" + graphData.freq + "Hz," + graphData.gain + "dBP," + graphData.phase + "bD";

                    el7.Height = graphData.height;
                    el7.Width = graphData.width;
                    tt7.X = graphData.ex;
                    tt7.Y = graphData.ey;
                    rtAngel7.Angle = graphData.angel;

                    // Data to Port;
                }
                else if (graphData.Id == 8)
                {
                    lblFreq8.Text = graphData.Name + "-" + graphData.freq + "Hz," + graphData.gain + "dBP," + graphData.phase + "bD";

                    el8.Height = graphData.height;
                    el8.Width = graphData.width;
                    tt8.X = graphData.ex;
                    tt8.Y = graphData.ey;
                    rtAngel8.Angle = graphData.angel;

                    // Data to Port;
                }
                else if (graphData.Id == 2)
                {
                    lblFreq1.Text = graphData.Name + "-" + graphData.freq + "Hz," + graphData.gain + "dBP," + graphData.phase + "bD";

                    el1.Height = graphData.height;
                    el1.Width = graphData.width;
                    tt1.X = graphData.ex;
                    tt1.Y = graphData.ey;
                    rtAngel1.Angle = graphData.angel;

                    // Data to Port;
                }

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
                freqPop.ShowDialog();
            }
        }

        private void freqPop_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if(freqPop.IsSaved)
            {
                mainWindow.ImplementChanges();
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

