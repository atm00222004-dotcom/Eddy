using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace _8F
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string selectedEllipe;
        public MainWindow()
        {
            selectedEllipe = null;
            InitializeComponent();
        }

        private void SelectEllipse(object sender)
        {
            if (((Border)sender).Name == "br1")
            {
                selectedEllipe = ((Border)sender).Name;
                txtHeight.Text = ellipse1.Height.ToString();
                txtWidth.Text = ellipse1.Width.ToString();
                txtXShift.Text = tt1.X.ToString();
                txtYShift.Text = tt1.Y.ToString();
                txtAngel.Text = rtAngel1.Angle.ToString();
            }
            else if (((Border)sender).Name == "br2")
            {
                selectedEllipe = ((Border)sender).Name;
                txtHeight.Text = ellipse2.Height.ToString();
                txtWidth.Text = ellipse2.Width.ToString();
                txtXShift.Text = tt2.X.ToString();
                txtYShift.Text = tt2.Y.ToString();
                txtAngel.Text = rtAngel2.Angle.ToString();
            }
            else if (((Border)sender).Name == "br3")
            {
                selectedEllipe = ((Border)sender).Name;
                txtHeight.Text = ellipse3.Height.ToString();
                txtWidth.Text = ellipse3.Width.ToString();
                txtXShift.Text = tt3.X.ToString();
                txtYShift.Text = tt3.Y.ToString();
                txtAngel.Text = rtAngel3.Angle.ToString();
            }
            else if (((Border)sender).Name == "br4")
            {
                selectedEllipe = ((Border)sender).Name;
                txtHeight.Text = ellipse4.Height.ToString();
                txtWidth.Text = ellipse4.Width.ToString();
                txtXShift.Text = tt4.X.ToString();
                txtYShift.Text = tt4.Y.ToString();
                txtAngel.Text = rtAngel4.Angle.ToString();
            }
        }

        private void br1_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            SelectEllipse(sender);
        }

        private void btnApply_Click(object sender, RoutedEventArgs e)
        {
            if (selectedEllipe == "br1")
            {
                ellipse1.Height = Convert.ToInt32(txtHeight.Text);
                ellipse1.Width = Convert.ToInt32(txtWidth.Text);
                tt1.X = Convert.ToInt32(txtXShift.Text);
                tt1.Y = Convert.ToInt32(txtYShift.Text);
                rtAngel1.Angle = Convert.ToInt32(txtAngel.Text);
            }
            else if (selectedEllipe == "br2")
            {
                ellipse2.Height = Convert.ToInt32(txtHeight.Text);
                ellipse2.Width = Convert.ToInt32(txtWidth.Text);
                tt2.X = Convert.ToInt32(txtXShift.Text);
                tt2.Y = Convert.ToInt32(txtYShift.Text);
                rtAngel2.Angle = Convert.ToInt32(txtAngel.Text);
            }
            else if (selectedEllipe == "br3")
            {
                ellipse3.Height = Convert.ToInt32(txtHeight.Text);
                ellipse3.Width = Convert.ToInt32(txtWidth.Text);
                tt3.X = Convert.ToInt32(txtXShift.Text);
                tt3.Y = Convert.ToInt32(txtYShift.Text);
                rtAngel3.Angle = Convert.ToInt32(txtAngel.Text);
            }
            else if (selectedEllipe == "br4")
            {
                ellipse4.Height = Convert.ToInt32(txtHeight.Text);
                ellipse4.Width = Convert.ToInt32(txtWidth.Text);
                tt4.X = Convert.ToInt32(txtXShift.Text);
                tt4.Y = Convert.ToInt32(txtYShift.Text);
                rtAngel4.Angle = Convert.ToInt32(txtAngel.Text);
            }
        }
    }
}