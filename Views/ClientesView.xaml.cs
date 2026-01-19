using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using PuntoDeVenta.Data;
using PuntoDeVenta.Models;

namespace PuntoDeVenta.Views
{
    public partial class ClientesView : UserControl
    {
        private ObservableCollection<Contacto> _contactos = new();
        private List<Contacto> _todosLosContactos = new();
        
        public ClientesView()
        {
            InitializeComponent();
            CargarContactos();
        }
        
        private void CargarContactos()
        {
            // Recalcular operaciones basándose en ventas/compras reales
            Database.Instance.RecalcularOperacionesContactos();
            
            _todosLosContactos = Database.Instance.ObtenerContactos();
            AplicarFiltros();
        }
        
        private void AplicarFiltros()
        {
            var filtro = txtBuscar?.Text?.ToLower() ?? "";
            var tipoIndex = cmbFiltroTipo?.SelectedIndex ?? 0;
            
            var resultado = _todosLosContactos.AsEnumerable();
            
            // Filtrar por tipo
            if (tipoIndex == 1) // Clientes
                resultado = resultado.Where(c => c.Tipo == "Cliente");
            else if (tipoIndex == 2) // Proveedores
                resultado = resultado.Where(c => c.Tipo == "Proveedor");
            
            // Filtrar por búsqueda
            if (!string.IsNullOrWhiteSpace(filtro))
            {
                resultado = resultado.Where(c =>
                    (c.Nombre ?? "").ToLower().Contains(filtro) ||
                    (c.Cedula ?? "").ToLower().Contains(filtro) ||
                    (c.Telefono ?? "").ToLower().Contains(filtro) ||
                    (c.Email ?? "").ToLower().Contains(filtro));
            }
            
            _contactos = new ObservableCollection<Contacto>(resultado);
            dgClientes.ItemsSource = _contactos;
        }
        
        private void TxtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Mostrar/ocultar placeholder
            txtBuscarPlaceholder.Visibility = string.IsNullOrEmpty(txtBuscar.Text) 
                ? Visibility.Visible : Visibility.Collapsed;
            
            AplicarFiltros();
        }
        
        private void CmbFiltroTipo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Evitar ejecutar antes de que el control esté completamente cargado
            if (!IsLoaded) return;
            AplicarFiltros();
        }
        
        private void NuevoCliente_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ClienteDialog();
            if (dialog.ShowDialog() == true && dialog.ContactoCreado != null)
            {
                Database.Instance.GuardarContacto(dialog.ContactoCreado);
                CargarContactos();
            }
        }
        
        private void EditarContacto_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.DataContext is Contacto contacto)
            {
                var dialog = new ClienteDialog(contacto);
                if (dialog.ShowDialog() == true && dialog.ContactoCreado != null)
                {
                    Database.Instance.GuardarContacto(dialog.ContactoCreado);
                    CargarContactos();
                }
            }
        }
        
        private void EliminarContacto_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.DataContext is Contacto contacto)
            {
                var result = MessageBox.Show(
                    $"¿Está seguro de eliminar el contacto '{contacto.Nombre}'?",
                    "Confirmar eliminación",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    Database.Instance.EliminarContacto(contacto.Id);
                    CargarContactos();
                }
            }
        }
    }
}
