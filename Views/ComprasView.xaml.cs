using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using PuntoDeVenta.Data;
using PuntoDeVenta.Models;

namespace PuntoDeVenta.Views
{
    public partial class ComprasView : UserControl
    {
        private ObservableCollection<CompraModel> _compras = new();
        
        public ComprasView()
        {
            InitializeComponent();
            LoadCompras();
        }
        
        private void LoadCompras()
        {
            var compras = Database.Instance.ObtenerCompras();
            _compras = new ObservableCollection<CompraModel>(compras);
            dgCompras.ItemsSource = _compras;
        }
        
        private void NuevaCompra_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CompraDialog();
            dialog.Owner = Window.GetWindow(this);
            
            if (dialog.ShowDialog() == true)
            {
                // Recargar desde la base de datos
                LoadCompras();
                MessageBox.Show("Compra registrada exitosamente", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        
        private void VerDetalleCompra_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is CompraModel compra)
            {
                var detalles = Database.Instance.ObtenerDetallesCompra(compra.Id);
                var detalleTexto = string.Join("\n", detalles.Select(d => 
                    $"• {d.ProductoNombre} x{d.Cantidad} ({d.TipoCompra}) = ${d.Subtotal:N2}"));
                
                MessageBox.Show(
                    $"📦 Compra: {compra.NumeroFactura}\n" +
                    $"📅 Fecha: {compra.FechaFormateada}\n" +
                    $"🏪 Proveedor: {compra.ProveedorNombre}\n\n" +
                    $"📋 Productos:\n{detalleTexto}\n\n" +
                    $"💰 Total: ${compra.TotalUsd:N2} USD",
                    "Detalle de Compra",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
    }
}
