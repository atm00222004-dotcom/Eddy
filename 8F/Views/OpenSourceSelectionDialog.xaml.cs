using System.Windows;
using System.Windows.Input;

namespace _8F
{
    public enum OpenSourceType
    {
        None,
        File,
        Database
    }

    /// <summary>
    /// Interaction logic for OpenSourceSelectionDialog.xaml
    /// </summary>
    public partial class OpenSourceSelectionDialog : Window
    {
        public OpenSourceType SelectedSource { get; private set; } = OpenSourceType.None;

        public OpenSourceSelectionDialog()
        {
            InitializeComponent();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                try
                {
                    DragMove();
                }
                catch (InvalidOperationException)
                {
                    // Ignore DragMove exceptions if mouse state changes mid-click
                }
            }
        }

        private void btnFile_Click(object sender, RoutedEventArgs e)
        {
            SelectedSource = OpenSourceType.File;
            DialogResult = true;
            Close();
        }

        private void btnDatabase_Click(object sender, RoutedEventArgs e)
        {
            SelectedSource = OpenSourceType.Database;
            DialogResult = true;
            Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            SelectedSource = OpenSourceType.None;
            DialogResult = false;
            Close();
        }
    }
}
