using System.Windows;
using System.Windows.Controls;
using System.Linq;
using System.Windows.Threading;
using PuntoDeVenta.Data;

namespace PuntoDeVenta.Views
{
    public partial class DashboardView : UserControl
    {
        private readonly DispatcherTimer _timer;
        
        public DashboardView()
        {
            InitializeComponent();
            LoadDashboardData();
            
            // Configurar timer para actualizar hora cada segundo
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += (s, e) => ActualizarFechaHora();
            _timer.Start();
            ActualizarFechaHora();
        }
        
        private void ActualizarFechaHora()
        {
            txtFecha.Text = DateTime.Now.ToString("dddd, dd MMMM yyyy");
            txtHora.Text = DateTime.Now.ToString("HH:mm:ss");
        }
        
        private void LoadDashboardData()
        {
            try
            {
                // Obtener tasa de cambio y mostrarla
                decimal tasaCambio = Database.Instance.ObtenerTasaCambio();
                txtTasaCambio.Text = tasaCambio.ToString("N2");
                
                // Obtener ventas del día
                var (ventasUsd, transacciones) = Database.Instance.ObtenerVentasDelDia();
                
                txtVentasUsd.Text = $"$ {ventasUsd:N2}";
                txtVentasBs.Text = $"Bs {ventasUsd * tasaCambio:N2}";
                txtTransacciones.Text = transacciones.ToString();
                
                // Obtener estadísticas de productos
                var (totalProductos, stockBajo) = Database.Instance.ObtenerEstadisticasProductos();
                txtProductos.Text = totalProductos.ToString();
                txtStockBajo.Text = stockBajo.ToString();
                
                // Cargar últimas ventas
                var ultimasVentas = Database.Instance.ObtenerUltimasVentas(10);
                lstUltimasVentas.ItemsSource = ultimasVentas.Select(v => new {
                    Factura = v.NumeroFactura,
                    Cliente = v.ClienteNombre,
                    Total = $"$ {v.Total:N2}",
                    Hora = v.Fecha.ToString("HH:mm")
                });
            }
            catch
            {
                // Valores por defecto si hay error
                txtVentasUsd.Text = "$ 0.00";
                txtVentasBs.Text = "Bs 0.00";
                txtTransacciones.Text = "0";
                txtProductos.Text = "0";
                txtStockBajo.Text = "0";
                txtTasaCambio.Text = "0.00";
            }
        }
        
        private void GuardarTasa_Click(object sender, RoutedEventArgs e)
        {
            if (decimal.TryParse(txtTasaCambio.Text, out var nuevaTasa) && nuevaTasa > 0)
            {
                Database.Instance.ActualizarTasaCambio(nuevaTasa);
                
                // Actualizar los valores en Bs con la nueva tasa
                LoadDashboardData();
                
                MessageBox.Show($"Tasa de cambio actualizada a Bs {nuevaTasa:N2}", 
                    "✅ Tasa Actualizada", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Por favor ingrese una tasa válida mayor a 0", 
                    "⚠️ Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        
        private void NuevaVenta_Click(object sender, RoutedEventArgs e)
        {
            // Navegar a VentasView a través del MainWindow
            if (Window.GetWindow(this) is MainWindow mainWindow)
            {
                mainWindow.NavigateTo("Ventas");
            }
        }
        
        private void AgregarProducto_Click(object sender, RoutedEventArgs e)
        {
            // Navegar a ProductosView
            if (Window.GetWindow(this) is MainWindow mainWindow)
            {
                mainWindow.NavigateTo("Productos");
            }
        }
        
        private void NuevoCliente_Click(object sender, RoutedEventArgs e)
        {
            // Navegar a ClientesView
            if (Window.GetWindow(this) is MainWindow mainWindow)
            {
                mainWindow.NavigateTo("Clientes");
            }
        }
    }
}
