using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using PuntoDeVenta.Data;
using PuntoDeVenta.Models;
using ClosedXML.Excel;

namespace PuntoDeVenta.Views
{
    public class VentaReporte
    {
        public int Id { get; set; }
        public string NumeroFactura { get; set; } = "";
        public string Fecha { get; set; } = "";
        public string Cliente { get; set; } = "";
        public int CantidadProductos { get; set; }
        public decimal TotalUsd { get; set; }
        public decimal TotalBs { get; set; }
        public string TotalUsdFormateado => $"$ {TotalUsd:N2}";
        public string TotalBsFormateado => $"Bs {TotalBs:N2}";
    }
    
    public partial class ReportesView : UserControl
    {
        private ObservableCollection<VentaReporte> _ventas = new();
        private decimal _tasaCambio;
        
        public ReportesView()
        {
            InitializeComponent();
            _tasaCambio = Database.Instance.ObtenerTasaCambio();
            GenerarReporte();
        }
        
        private void GenerarReporte()
        {
            var ventas = Database.Instance.ObtenerVentas(100);
            
            _ventas = new ObservableCollection<VentaReporte>(
                ventas.Select(v => new VentaReporte
                {
                    Id = v.Id,
                    NumeroFactura = v.NumeroFactura ?? $"V-{v.Id:D4}",
                    Fecha = v.Fecha.ToString("dd/MM/yyyy"),
                    Cliente = v.ClienteNombre ?? "Cliente General",
                    CantidadProductos = Database.Instance.ObtenerCantidadProductosVenta(v.Id),
                    TotalUsd = v.Total,
                    TotalBs = v.Total * _tasaCambio
                }));
            
            dgVentas.ItemsSource = _ventas;
            
            decimal totalUsd = _ventas.Sum(v => v.TotalUsd);
            decimal totalBs = _ventas.Sum(v => v.TotalBs);
            int totalProductos = _ventas.Sum(v => v.CantidadProductos);
            decimal promedio = _ventas.Count > 0 ? totalUsd / _ventas.Count : 0;
            
            txtVentasUsd.Text = $"$ {totalUsd:N2}";
            txtVentasBs.Text = $"Bs {totalBs:N2}";
            txtTransacciones.Text = _ventas.Count.ToString();
            txtPromedio.Text = $"$ {promedio:N2}";
            txtPromedioBs.Text = $"Bs {promedio * _tasaCambio:N2}";
            txtProductosVendidos.Text = totalProductos.ToString();
        }
        
        private void GenerarReporte_Click(object sender, RoutedEventArgs e)
        {
            GenerarReporte();
            MessageBox.Show("Reporte actualizado con datos recientes", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        
        private void ExportarVentas_Click(object sender, RoutedEventArgs e)
        {
            var saveDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Archivo Excel (*.xlsx)|*.xlsx",
                DefaultExt = ".xlsx",
                FileName = $"ventas_{DateTime.Now:yyyyMMdd}"
            };
            
            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    using var workbook = new XLWorkbook();
                    var ws = workbook.Worksheets.Add("Ventas");
                    
                    // Encabezados
                    var headers = new[] { "Nº Factura", "Fecha", "Cliente", "Productos", "Total USD", "Total Bs" };
                    for (int i = 0; i < headers.Length; i++)
                    {
                        ws.Cell(1, i + 1).Value = headers[i];
                        ws.Cell(1, i + 1).Style.Font.Bold = true;
                        ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#10b981");
                        ws.Cell(1, i + 1).Style.Font.FontColor = XLColor.White;
                    }
                    
                    int row = 2;
                    foreach (var v in _ventas)
                    {
                        ws.Cell(row, 1).Value = v.NumeroFactura;
                        ws.Cell(row, 2).Value = v.Fecha;
                        ws.Cell(row, 3).Value = v.Cliente;
                        ws.Cell(row, 4).Value = v.CantidadProductos;
                        ws.Cell(row, 5).Value = v.TotalUsd;
                        ws.Cell(row, 6).Value = v.TotalBs;
                        row++;
                    }
                    
                    ws.Columns().AdjustToContents();
                    workbook.SaveAs(saveDialog.FileName);
                    MessageBox.Show($"Se exportaron {_ventas.Count} ventas exitosamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al exportar: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        
        private void ExportarCompras_Click(object sender, RoutedEventArgs e)
        {
            var compras = Database.Instance.ObtenerCompras(100);
            
            var saveDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Archivo Excel (*.xlsx)|*.xlsx",
                DefaultExt = ".xlsx",
                FileName = $"compras_{DateTime.Now:yyyyMMdd}"
            };
            
            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    using var workbook = new XLWorkbook();
                    var ws = workbook.Worksheets.Add("Compras");
                    
                    var headers = new[] { "Nº Factura", "Fecha", "Proveedor", "Total USD", "Total Bs", "Estado" };
                    for (int i = 0; i < headers.Length; i++)
                    {
                        ws.Cell(1, i + 1).Value = headers[i];
                        ws.Cell(1, i + 1).Style.Font.Bold = true;
                        ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#0ea5e9");
                        ws.Cell(1, i + 1).Style.Font.FontColor = XLColor.White;
                    }
                    
                    int row = 2;
                    foreach (var c in compras)
                    {
                        ws.Cell(row, 1).Value = c.NumeroFactura;
                        ws.Cell(row, 2).Value = c.Fecha.ToString("dd/MM/yyyy");
                        ws.Cell(row, 3).Value = c.ProveedorNombre;
                        ws.Cell(row, 4).Value = c.TotalUsd;
                        ws.Cell(row, 5).Value = c.TotalBs;
                        ws.Cell(row, 6).Value = c.Estado;
                        row++;
                    }
                    
                    ws.Columns().AdjustToContents();
                    workbook.SaveAs(saveDialog.FileName);
                    MessageBox.Show($"Se exportaron {compras.Count} compras exitosamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al exportar: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        
        private void ExportarInventario_Click(object sender, RoutedEventArgs e)
        {
            var productos = Database.Instance.ObtenerProductos();
            
            var saveDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Archivo Excel (*.xlsx)|*.xlsx",
                DefaultExt = ".xlsx",
                FileName = $"inventario_{DateTime.Now:yyyyMMdd}"
            };
            
            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    using var workbook = new XLWorkbook();
                    var ws = workbook.Worksheets.Add("Inventario");
                    
                    var headers = new[] { "Código", "Nombre", "Categoría", "Stock", "Precio Venta", "Precio Costo", "Valorizado" };
                    for (int i = 0; i < headers.Length; i++)
                    {
                        ws.Cell(1, i + 1).Value = headers[i];
                        ws.Cell(1, i + 1).Style.Font.Bold = true;
                        ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#f59e0b");
                        ws.Cell(1, i + 1).Style.Font.FontColor = XLColor.White;
                    }
                    
                    int row = 2;
                    foreach (var p in productos)
                    {
                        ws.Cell(row, 1).Value = p.Codigo;
                        ws.Cell(row, 2).Value = p.Nombre;
                        ws.Cell(row, 3).Value = p.Categoria;
                        ws.Cell(row, 4).Value = p.Stock;
                        ws.Cell(row, 5).Value = p.PrecioVenta;
                        ws.Cell(row, 6).Value = p.PrecioCosto;
                        ws.Cell(row, 7).Value = p.Stock * p.PrecioCosto;
                        row++;
                    }
                    
                    ws.Columns().AdjustToContents();
                    workbook.SaveAs(saveDialog.FileName);
                    MessageBox.Show($"Se exportaron {productos.Count} productos exitosamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al exportar: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}

