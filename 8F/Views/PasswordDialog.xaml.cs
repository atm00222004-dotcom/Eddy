using System.Windows;
using System.Windows.Input;

namespace _8F
{
    /// <summary>
    /// Interaction logic for PasswordDialog.xaml
    /// </summary>
    public partial class PasswordDialog : Window
    {
        private readonly string _expectedPassword;

        public PasswordDialog(string expectedPassword)
        {
            InitializeComponent();
            _expectedPassword = expectedPassword;
            Loaded += (s, e) => txtPassword.Focus();
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

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            ValidatePassword();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void txtPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ValidatePassword();
            }
        }

        private void ValidatePassword()
        {
            if (txtPassword.Password == _expectedPassword)
            {
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Invalid password.", "Authentication Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                txtPassword.SelectAll();
                txtPassword.Focus();
            }
        }
    }
}
