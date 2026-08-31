using System.Windows;
using System.Windows.Input;

namespace _8F
{
    public partial class SaveProfileDialog : Window
    {
        public string ProfileName { get; private set; } = string.Empty;

        public SaveProfileDialog(string defaultName = "")
        {
            InitializeComponent();
            txtProfileName.Text = defaultName;
            Loaded += (s, e) =>
            {
                txtProfileName.Focus();
                txtProfileName.SelectAll();
            };
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                try { DragMove(); } catch { }
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            string name = txtProfileName.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Please enter a valid profile name.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ProfileName = name;
            DialogResult = true;
            Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void txtProfileName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                btnSave_Click(sender, e);
            }
            else if (e.Key == Key.Escape)
            {
                btnCancel_Click(sender, e);
            }
        }
    }
}
