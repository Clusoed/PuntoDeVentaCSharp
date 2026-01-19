using System.Windows;
using System.Windows.Controls;
using PuntoDeVenta.Data;
using PuntoDeVenta.Services;

namespace PuntoDeVenta.Views
{
    public partial class ConfigView : UserControl
    {
        public ConfigView()
        {
            InitializeComponent();
            LoadConfig();
        }
        
        private void LoadConfig()
        {
            // Cargar desde base de datos SQLite
            txtNombreTienda.Text = Database.Instance.ObtenerConfiguracion("NombreNegocio", "Mi Tienda");
            txtRif.Text = Database.Instance.ObtenerConfiguracion("RIF", "");
            txtDireccion.Text = Database.Instance.ObtenerConfiguracion("Direccion", "");
            txtTelefono.Text = Database.Instance.ObtenerConfiguracion("Telefono", "");
            txtTasaCambio.Text = Database.Instance.ObtenerConfiguracion("TasaCambio", "45.50");
            txtIva.Text = Database.Instance.ObtenerConfiguracion("IVA", "16");
            txtVersion.Text = $"v{UpdateService.Instance.GetCurrentVersion()}";
        }
        
        private void GuardarConfig_Click(object sender, RoutedEventArgs e)
        {
            // Guardar en base de datos SQLite
            Database.Instance.GuardarConfiguracion("NombreNegocio", txtNombreTienda.Text);
            Database.Instance.GuardarConfiguracion("RIF", txtRif.Text);
            Database.Instance.GuardarConfiguracion("Direccion", txtDireccion.Text);
            Database.Instance.GuardarConfiguracion("Telefono", txtTelefono.Text);
            Database.Instance.GuardarConfiguracion("TasaCambio", txtTasaCambio.Text);
            Database.Instance.GuardarConfiguracion("IVA", txtIva.Text);
            
            MessageBox.Show("Configuración guardada exitosamente", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        
        private async void BuscarActualizaciones_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var updateInfo = await UpdateService.Instance.CheckForUpdatesAsync();
                
                if (updateInfo.IsNewVersionAvailable)
                {
                    var updateDialog = new UpdateDialog(updateInfo);
                    updateDialog.ShowDialog();
                }
                else
                {
                    MessageBox.Show($"Ya tienes la última versión (v{UpdateService.Instance.GetCurrentVersion()}).", "Actualizaciones", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch
            {
                MessageBox.Show("No se pudo verificar actualizaciones.\nVerifica tu conexión a internet.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
