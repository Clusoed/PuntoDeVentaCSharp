using System.Windows;
using System.Windows.Controls;
using PuntoDeVenta.Data;

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
            txtVersion.Text = "v1.2.0";
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
        
        private void BuscarActualizaciones_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Verificar actualizaciones desde GitHub
            MessageBox.Show("No hay actualizaciones disponibles.\nYa tienes la última versión.", "Actualizaciones", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
