using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using PuntoDeVenta.Data;
using PuntoDeVenta.Models;

namespace PuntoDeVenta.Views
{
    public class ItemCarrito
    {
        public int ProductoId { get; set; }
        public string Codigo { get; set; } = "";
        public string Nombre { get; set; } = "";
        public decimal Precio { get; set; }
        public int Cantidad { get; set; } = 1;
        public decimal Subtotal => Precio * Cantidad;
        public string SubtotalFormateado => $"$ {Subtotal:N2}";
    }
    
    public partial class VentasView : UserControl
    {
        private ObservableCollection<ProductoModel> _productos = new();
        private ObservableCollection<ItemCarrito> _carrito = new();
        private List<Contacto> _clientes = new();
        
        public VentasView()
        {
            InitializeComponent();
            LoadProductos();
            LoadClientes();
            
            lstProductos.ItemsSource = _productos;
            lstCarrito.ItemsSource = _carrito;
        }
        
        private void LoadProductos()
        {
            // Cargar desde base de datos SQLite
            var productos = Database.Instance.ObtenerProductos();
            _productos = new ObservableCollection<ProductoModel>(productos.Where(p => p.Stock > 0));
            lstProductos.ItemsSource = _productos;
        }
        
        private void LoadClientes()
        {
            _clientes = Database.Instance.ObtenerContactos()
                .Where(c => c.Tipo == "Cliente" || c.Tipo == "Ambos")
                .ToList();
            
            // Agregar opción por defecto
            var clienteGeneral = new Contacto { Id = 0, Nombre = "Cliente General" };
            _clientes.Insert(0, clienteGeneral);
            
            cmbCliente.ItemsSource = _clientes;
            cmbCliente.SelectedIndex = 0;
        }
        
        private void TxtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Mostrar/ocultar placeholder
            txtBuscarPlaceholder.Visibility = string.IsNullOrEmpty(txtBuscar.Text) 
                ? Visibility.Visible : Visibility.Collapsed;
            
            var filtro = txtBuscar.Text.ToLower();
            if (string.IsNullOrWhiteSpace(filtro))
            {
                lstProductos.ItemsSource = _productos;
                return;
            }
            
            var filtrados = _productos.Where(p => 
                (p.Nombre ?? "").ToLower().Contains(filtro) || 
                (p.Codigo ?? "").ToLower().Contains(filtro));
                
            lstProductos.ItemsSource = filtrados;
        }
        
        private void AgregarProducto_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is ProductoModel producto)
            {
                var existente = _carrito.FirstOrDefault(x => x.ProductoId == producto.Id);
                if (existente != null)
                {
                    existente.Cantidad++;
                }
                else
                {
                    _carrito.Add(new ItemCarrito
                    {
                        ProductoId = producto.Id,
                        Codigo = producto.Codigo ?? "",
                        Nombre = producto.Nombre,
                        Precio = producto.PrecioVenta,
                        Cantidad = 1
                    });
                }
                
                lstCarrito.ItemsSource = null;
                lstCarrito.ItemsSource = _carrito;
                ActualizarTotales();
            }
        }
        
        private void ActualizarTotales()
        {
            decimal subtotal = _carrito.Sum(x => x.Subtotal);
            decimal porcentajeIva = decimal.TryParse(
                Database.Instance.ObtenerConfiguracion("IVA", "16"), out var iva) ? iva : 16m;
            decimal ivaAmount = subtotal * (porcentajeIva / 100);
            decimal total = subtotal + ivaAmount;
            decimal tasaCambio = Database.Instance.ObtenerTasaCambio();
            
            txtSubtotal.Text = $"$ {subtotal:N2}";
            txtIva.Text = $"$ {ivaAmount:N2}";
            txtTotal.Text = $"$ {total:N2}";
            txtTotalBs.Text = $"Bs {total * tasaCambio:N2}";
        }
        
        private void ProcesarVenta_Click(object sender, RoutedEventArgs e)
        {
            if (_carrito.Count == 0)
            {
                MessageBox.Show("El carrito está vacío", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            try
            {
                decimal subtotal = _carrito.Sum(x => x.Subtotal);
                decimal porcentajeIva = decimal.TryParse(
                    Database.Instance.ObtenerConfiguracion("IVA", "16"), out var iva) ? iva : 16m;
                decimal ivaAmount = subtotal * (porcentajeIva / 100);
                decimal total = subtotal + ivaAmount;
                
                // Obtener cliente seleccionado
                var clienteSeleccionado = cmbCliente.SelectedItem as Contacto;
                int? clienteId = clienteSeleccionado?.Id > 0 ? clienteSeleccionado.Id : null;
                string clienteNombre = clienteSeleccionado?.Nombre ?? "Cliente General";
                
                // Obtener método de pago
                string metodoPago = "Efectivo USD";
                if (cmbMetodoPago.SelectedItem is ComboBoxItem item)
                {
                    metodoPago = item.Content?.ToString() ?? "Efectivo USD";
                }
                
                // Crear venta
                var venta = new VentaModel
                {
                    Fecha = DateTime.Now,
                    ClienteId = clienteId,
                    ClienteNombre = clienteNombre,
                    Subtotal = subtotal,
                    Total = total,
                    MetodoPago = metodoPago,
                    Estado = "Completada"
                };
                
                // Preparar detalles
                var detalles = _carrito.Select(item => new VentaDetalle
                {
                    ProductoId = item.ProductoId,
                    ProductoNombre = item.Nombre,
                    Cantidad = item.Cantidad,
                    PrecioUnitario = item.Precio
                }).ToList();
                
                // Guardar venta en BD
                Database.Instance.InsertarVenta(venta, detalles);
                
                // Actualizar stock (restar)
                foreach (var cartItem in _carrito)
                {
                    Database.Instance.ActualizarStock(cartItem.ProductoId, -cartItem.Cantidad);
                }
                
                MessageBox.Show($"¡Venta procesada exitosamente!\nCliente: {clienteNombre}\nPago: {metodoPago}\nTotal: $ {total:N2}", 
                    "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                
                // Limpiar carrito
                _carrito.Clear();
                lstCarrito.ItemsSource = null;
                lstCarrito.ItemsSource = _carrito;
                ActualizarTotales();
                
                // Recargar productos para actualizar stock
                LoadProductos();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al procesar venta: {ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void AumentarCantidad_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is ItemCarrito item)
            {
                item.Cantidad++;
                RefrescarCarrito();
            }
        }
        
        private void DisminuirCantidad_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is ItemCarrito item)
            {
                if (item.Cantidad > 1)
                {
                    item.Cantidad--;
                    RefrescarCarrito();
                }
                else
                {
                    _carrito.Remove(item);
                    RefrescarCarrito();
                }
            }
        }
        
        private void EliminarDelCarrito_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is ItemCarrito item)
            {
                _carrito.Remove(item);
                RefrescarCarrito();
            }
        }
        
        private void RefrescarCarrito()
        {
            lstCarrito.ItemsSource = null;
            lstCarrito.ItemsSource = _carrito;
            ActualizarTotales();
        }
    }
}
