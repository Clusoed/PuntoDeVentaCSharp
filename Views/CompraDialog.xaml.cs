using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using PuntoDeVenta.Data;
using PuntoDeVenta.Models;

namespace PuntoDeVenta.Views
{
    // Clase local para la UI del carrito de compra
    public class ItemCompra
    {
        public int? ProductoId { get; set; }
        public string Nombre { get; set; } = "";
        public string TipoCompra { get; set; } = "Unidad"; // "Bulto" o "Unidad"
        public int UnidadesPorBulto { get; set; } = 1;
        public int Cantidad { get; set; } = 1;
        public decimal Precio { get; set; }
        public decimal Subtotal => Precio * Cantidad;
        public string PrecioFormateado => $"$ {Precio:N2}";
        public string SubtotalFormateado => $"$ {Subtotal:N2}";
        public string InfoUnidades => UnidadesPorBulto > 1 && TipoCompra == "Bulto" 
            ? $"({UnidadesPorBulto * Cantidad} und)" : "";
    }
    
    public partial class CompraDialog : Window
    {
        private ObservableCollection<ItemCompra> _itemsCompra = new();
        private List<ProductoModel> _productosDisponibles = new();
        private List<Contacto> _proveedores = new();
        public CompraModel? CompraGuardada { get; private set; }
        
        public CompraDialog()
        {
            InitializeComponent();
            dpFecha.SelectedDate = DateTime.Now;
            CargarProductos();
            CargarProveedores();
            txtNumeroFactura.Focus();
            lstProductos.ItemsSource = _itemsCompra;
        }
        
        private void CargarProductos()
        {
            _productosDisponibles = Database.Instance.ObtenerProductos();
            cmbProducto.ItemsSource = _productosDisponibles;
            cmbProducto.DisplayMemberPath = "Nombre";
        }
        
        private void CargarProveedores()
        {
            _proveedores = Database.Instance.ObtenerContactos("Proveedor");
            cmbProveedor.ItemsSource = _proveedores;
            cmbProveedor.DisplayMemberPath = "Nombre";
        }
        
        private void CmbProducto_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Cuando se selecciona un producto, ajustar el tipo de compra según UnidadesPorBulto
            if (cmbProducto.SelectedItem is ProductoModel producto)
            {
                if (producto.UnidadesPorBulto > 1)
                {
                    cmbTipoCompra.SelectedIndex = 1; // Bulto
                }
                else
                {
                    cmbTipoCompra.SelectedIndex = 0; // Unidad
                }
            }
        }
        
        private void AgregarProducto_Click(object sender, RoutedEventArgs e)
        {
            string nombreProducto = "";
            int? productoId = null;
            int unidadesPorBulto = 1;
            
            if (cmbProducto.SelectedItem is ProductoModel productoSeleccionado)
            {
                nombreProducto = productoSeleccionado.Nombre;
                productoId = productoSeleccionado.Id;
                unidadesPorBulto = productoSeleccionado.UnidadesPorBulto;
            }
            else if (!string.IsNullOrWhiteSpace(cmbProducto.Text))
            {
                nombreProducto = cmbProducto.Text.Trim();
            }
            else
            {
                MessageBox.Show("Seleccione o ingrese el nombre del producto", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            if (!int.TryParse(txtCantidad.Text, out int cantidad) || cantidad <= 0)
            {
                MessageBox.Show("Cantidad inválida", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            if (!decimal.TryParse(txtPrecioUnitario.Text, out decimal precio) || precio < 0)
            {
                MessageBox.Show("Precio inválido", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            string tipoCompra = (cmbTipoCompra.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Unidad";
            
            _itemsCompra.Add(new ItemCompra
            {
                ProductoId = productoId,
                Nombre = nombreProducto,
                TipoCompra = tipoCompra,
                UnidadesPorBulto = unidadesPorBulto,
                Cantidad = cantidad,
                Precio = precio
            });
            
            ActualizarTotal();
            
            // Limpiar campos
            cmbProducto.SelectedItem = null;
            cmbProducto.Text = "";
            txtCantidad.Text = "1";
            txtPrecioUnitario.Text = "0.00";
            cmbTipoCompra.SelectedIndex = 0;
            cmbProducto.Focus();
        }
        
        private void EliminarProducto_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is ItemCompra item)
            {
                _itemsCompra.Remove(item);
                ActualizarTotal();
            }
        }
        
        private void ActualizarTotal()
        {
            decimal total = _itemsCompra.Sum(p => p.Subtotal);
            txtTotal.Text = $"$ {total:N2}";
        }
        
        private void Guardar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtNumeroFactura.Text))
            {
                MessageBox.Show("El número de factura es requerido", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            if (string.IsNullOrWhiteSpace(cmbProveedor.Text))
            {
                MessageBox.Show("El proveedor es requerido", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            if (_itemsCompra.Count == 0)
            {
                MessageBox.Show("Agregue al menos un producto", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            try
            {
                decimal totalUsd = _itemsCompra.Sum(p => p.Subtotal);
                
                // Obtener proveedor
                int? proveedorId = null;
                string proveedorNombre = cmbProveedor.Text.Trim();
                if (cmbProveedor.SelectedItem is Contacto proveedor)
                {
                    proveedorId = proveedor.Id;
                    proveedorNombre = proveedor.Nombre;
                }
                
                var compra = new CompraModel
                {
                    NumeroFactura = txtNumeroFactura.Text.Trim(),
                    ProveedorId = proveedorId,
                    ProveedorNombre = proveedorNombre,
                    Fecha = dpFecha.SelectedDate ?? DateTime.Now,
                    TotalUsd = totalUsd,
                    Estado = "Completada"
                };
                
                var detalles = _itemsCompra.Select(item => new CompraDetalle
                {
                    ProductoId = item.ProductoId,
                    ProductoNombre = item.Nombre,
                    TipoCompra = item.TipoCompra,
                    Cantidad = item.Cantidad,
                    PrecioUnitario = item.Precio
                }).ToList();
                
                // Guardar en base de datos
                Database.Instance.InsertarCompra(compra, detalles);
                CompraGuardada = compra;
                
                MessageBox.Show(
                    $"Compra registrada exitosamente.\n\n" +
                    $"Se actualizaron los precios y stock de {detalles.Count(d => d.ProductoId.HasValue)} producto(s).",
                    "Éxito", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Information);
                
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar la compra: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void Cancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
