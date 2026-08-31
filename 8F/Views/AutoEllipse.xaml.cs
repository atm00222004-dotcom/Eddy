using System;
using System.Windows;
using _8F.ViewModels;

namespace _8F
{
    /// <summary>
    /// Interaction logic for AutoEllipse.xaml
    /// </summary>
    public partial class AutoEllipse : Window
    {
        public AutoEllipseViewModel ViewModel { get; }

        public bool IsSaved
        {
            get => ViewModel.IsSaved;
            set => ViewModel.IsSaved = value;
        }

        public DeviceCOM? portCOM
        {
            get => ViewModel.PortCOM;
            set => ViewModel.PortCOM = value;
        }

        public AutoEllipse()
        {
            InitializeComponent();
            ViewModel = new AutoEllipseViewModel(dataGrid: dgTestResults);
            ViewModel.CloseAction = Close;
            DataContext = ViewModel;

            Loaded += (s, e) =>
            {
                ViewModel.OwnerWindow = Owner;
                ViewModel.LoadedCommand.Execute(null);
            };
            Unloaded += (s, e) => ViewModel.UnloadedCommand.Execute(null);
        }

        private void Window_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
            {
                try { DragMove(); } catch { }
            }
        }
    }
}
