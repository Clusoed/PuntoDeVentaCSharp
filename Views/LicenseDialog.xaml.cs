using System.Windows;
using PuntoDeVenta.Services;

namespace PuntoDeVenta.Views
{
    public partial class LicenseDialog : Window
    {
        public bool LicenseActivated { get; private set; } = false;
        
        public LicenseDialog()
        {
            InitializeComponent();
        }
        
        private void Activate_Click(object sender, RoutedEventArgs e)
        {
            var licenseKey = txtLicenseKey.Text.Trim();
            
            if (string.IsNullOrWhiteSpace(licenseKey))
            {
                ShowStatus("⚠️ Por favor ingrese una clave de licencia", false);
                return;
            }
            
            if (LicenseService.Instance.ActivateLicense(licenseKey))
            {
                ShowStatus("✅ ¡Licencia activada correctamente!", true);
                LicenseActivated = true;
                
                MessageBox.Show("¡Licencia activada correctamente!\n\nGracias por adquirir el software.", 
                    "✅ Activación Exitosa", MessageBoxButton.OK, MessageBoxImage.Information);
                
                DialogResult = true;
                Close();
            }
            else
            {
                ShowStatus("❌ Clave de licencia inválida", false);
            }
        }
        
        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
        
        private void ShowStatus(string message, bool isSuccess)
        {
            borderStatus.Visibility = Visibility.Visible;
            borderStatus.Background = isSuccess 
                ? System.Windows.Media.Brushes.DarkGreen 
                : System.Windows.Media.Brushes.DarkRed;
            txtStatus.Text = message;
            txtStatus.Foreground = System.Windows.Media.Brushes.White;
        }
    }
}
