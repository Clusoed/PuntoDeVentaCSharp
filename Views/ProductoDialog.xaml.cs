using System.Windows;
using System.Windows.Controls;
using PuntoDeVenta.Models;
using PuntoDeVenta.Data;

namespace PuntoDeVenta.Views
{
    public partial class ProductoDialog : Window
    {
        public ProductoModel? ProductoCreado { get; private set; }
        private bool _isInitialized = false;
        private int _productoId = 0;
        
        public ProductoDialog(ProductoModel? productoExistente = null)
        {
            InitializeComponent();
            _isInitialized = true;
            
            if (productoExistente != null)
            {
                // Modo edición
                _productoId = productoExistente.Id;
                Title = "Editar Producto";
                txtCodigo.Text = productoExistente.Codigo;
                txtNombre.Text = productoExistente.Nombre;
                SeleccionarCategoria(productoExistente.Categoria ?? "Otros");
                SeleccionarUnidad(productoExistente.UnidadMedida ?? "Unidad");
                txtUnidadesPorBulto.Text = productoExistente.UnidadesPorBulto.ToString();
                txtPrecioCosto.Text = productoExistente.PrecioCosto.ToString("F2");
                txtStock.Text = productoExistente.Stock.ToString();
                txtDescripcion.Text = productoExistente.Descripcion;
            }
            else
            {
                GenerarCodigoAutomatico();
            }
            
            txtNombre.Focus();
            CalcularPrecios();
        }
        
        private void SeleccionarCategoria(string categoria)
        {
            foreach (ComboBoxItem item in cmbCategoria.Items)
            {
                if (item.Content?.ToString() == categoria)
                {
                    cmbCategoria.SelectedItem = item;
                    break;
                }
            }
        }
        
        private void SeleccionarUnidad(string unidad)
        {
            foreach (ComboBoxItem item in cmbUnidad.Items)
            {
                if (item.Content?.ToString() == unidad)
                {
                    cmbUnidad.SelectedItem = item;
                    break;
                }
            }
        }
        
        private void GenerarCodigoAutomatico()
        {
            // Generar código único basado en productos existentes
            var productos = Database.Instance.ObtenerProductos();
            int maxNum = 100;
            foreach (var p in productos)
            {
                if (p.Codigo != null && p.Codigo.StartsWith("P") && 
                    int.TryParse(p.Codigo.Substring(1), out int num))
                {
                    if (num >= maxNum) maxNum = num + 1;
                }
            }
            txtCodigo.Text = $"P{maxNum:D4}";
        }
        
        private void Unidad_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;
            
            var unidad = (cmbUnidad.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
            bool esBulto = unidad == "Bulto" || unidad == "Caja" || unidad == "Paquete";
            
            // Mostrar/ocultar campos de bulto
            pnlUnidadesPorBulto.Visibility = esBulto ? Visibility.Visible : Visibility.Collapsed;
            pnlCostoUnidad.Visibility = esBulto ? Visibility.Visible : Visibility.Collapsed;
            pnlInfoBulto.Visibility = esBulto ? Visibility.Visible : Visibility.Collapsed;
            
            // Actualizar etiquetas
            lblPrecioCosto.Text = esBulto ? $"Costo por {unidad} (USD) *" : "Precio de Costo (USD) *";
            lblPrecioVenta.Text = esBulto ? "Venta por Unidad" : "Precio Venta (USD)";
            lblStock.Text = esBulto ? $"Stock ({unidad}s)" : "Stock";
            
            CalcularPrecios();
        }
        
        private void UnidadesBulto_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isInitialized) CalcularPrecios();
        }
        
        private void PrecioCosto_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isInitialized) CalcularPrecios();
        }
        
        private void Ganancia_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isInitialized) CalcularPrecios();
        }
        
        private void Stock_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isInitialized) CalcularPrecios();
        }
        
        private void CalcularPrecios()
        {
            try
            {
                if (txtPrecioCosto == null || txtPorcentajeGanancia == null || 
                    txtPrecioVenta == null || txtGananciaUnidad == null)
                    return;
                
                var textoCosto = txtPrecioCosto.Text?.Replace(",", ".") ?? "0";
                var textoPorcentaje = txtPorcentajeGanancia.Text?.Replace(",", ".") ?? "0";
                var textoUnidades = txtUnidadesPorBulto?.Text ?? "1";
                var textoStock = txtStock?.Text ?? "0";
                
                decimal.TryParse(textoCosto, System.Globalization.NumberStyles.Any, 
                    System.Globalization.CultureInfo.InvariantCulture, out decimal precioCosto);
                decimal.TryParse(textoPorcentaje, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out decimal porcentaje);
                int.TryParse(textoUnidades, out int unidadesPorBulto);
                int.TryParse(textoStock, out int stockIngresado);
                if (unidadesPorBulto < 1) unidadesPorBulto = 1;
                
                var unidad = (cmbUnidad?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
                bool esBulto = unidad == "Bulto" || unidad == "Caja" || unidad == "Paquete";
                
                decimal costoBase = precioCosto;
                if (esBulto && unidadesPorBulto > 1)
                {
                    costoBase = precioCosto / unidadesPorBulto;
                    if (txtCostoUnidad != null)
                        txtCostoUnidad.Text = $"$ {costoBase:N2}";
                    
                    int stockTotal = stockIngresado * unidadesPorBulto;
                    if (txtInfoBulto != null)
                        txtInfoBulto.Text = $"{stockIngresado} {unidad}(s) × {unidadesPorBulto} und = {stockTotal} unidades en inventario";
                }
                
                if (costoBase >= 0 && porcentaje >= 0)
                {
                    decimal ganancia = costoBase * (porcentaje / 100);
                    decimal precioVenta = costoBase + ganancia;
                    
                    txtPrecioVenta.Text = $"{precioVenta:N2}";
                    txtGananciaUnidad.Text = $"$ {ganancia:N2}";
                }
            }
            catch { }
        }
        
        private void Guardar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtNombre.Text))
            {
                MessageBox.Show("El nombre es requerido", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtNombre.Focus();
                return;
            }
            
            var textoCosto = txtPrecioCosto.Text?.Replace(",", ".") ?? "0";
            if (!decimal.TryParse(textoCosto, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out decimal precioCosto) || precioCosto < 0)
            {
                MessageBox.Show("El precio de costo es inválido", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtPrecioCosto.Focus();
                return;
            }
            
            var textoVenta = txtPrecioVenta.Text?.Replace(",", ".").Replace("$", "").Trim() ?? "0";
            decimal.TryParse(textoVenta, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out decimal precioVenta);
            
            int.TryParse(txtStock.Text, out int stockIngresado);
            int.TryParse(txtUnidadesPorBulto?.Text ?? "1", out int unidadesPorBulto);
            if (unidadesPorBulto < 1) unidadesPorBulto = 1;
            
            string categoria = (cmbCategoria.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Otros";
            string unidad = (cmbUnidad.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Unidad";
            
            bool esBulto = unidad == "Bulto" || unidad == "Caja" || unidad == "Paquete";
            
            // Si es bulto, el stock se guarda en UNIDADES (ej: 5 cajas × 24 = 120 unidades)
            int stockFinal = esBulto ? stockIngresado * unidadesPorBulto : stockIngresado;
            
            ProductoCreado = new ProductoModel
            {
                Id = _productoId,
                Codigo = txtCodigo.Text.Trim(),
                Nombre = txtNombre.Text.Trim(),
                Categoria = categoria,
                UnidadMedida = unidad,
                UnidadesPorBulto = unidadesPorBulto,
                PrecioCosto = precioCosto,
                PrecioVenta = precioVenta,
                Stock = stockFinal, // Stock siempre en unidades
                Descripcion = txtDescripcion.Text?.Trim() ?? ""
            };
            
            DialogResult = true;
            Close();
        }
        
        private void Cancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
