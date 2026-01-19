using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using PuntoDeVenta.Data;
using PuntoDeVenta.Models;
using ClosedXML.Excel;

namespace PuntoDeVenta.Views
{
    public partial class ProductosView : UserControl
    {
        private ObservableCollection<ProductoModel> _productos = new();
        
        public ProductosView()
        {
            InitializeComponent();
            LoadProductos();
        }
        
        private void LoadProductos()
        {
            // Cargar desde base de datos SQLite
            var productos = Database.Instance.ObtenerProductos();
            _productos = new ObservableCollection<ProductoModel>(productos);
            dgProductos.ItemsSource = _productos;
        }
        
        private void TxtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Mostrar/ocultar placeholder
            txtBuscarPlaceholder.Visibility = string.IsNullOrEmpty(txtBuscar.Text) 
                ? Visibility.Visible : Visibility.Collapsed;
            
            var filtro = txtBuscar.Text.ToLower();
            if (string.IsNullOrWhiteSpace(filtro))
            {
                dgProductos.ItemsSource = _productos;
                return;
            }
            
            var filtrados = _productos.Where(p => 
                (p.Nombre ?? "").ToLower().Contains(filtro) || 
                (p.Codigo ?? "").ToLower().Contains(filtro) ||
                (p.Categoria ?? "").ToLower().Contains(filtro));
                
            dgProductos.ItemsSource = filtrados;
        }
        
        private void NuevoProducto_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ProductoDialog();
            dialog.Owner = Window.GetWindow(this);
            
            if (dialog.ShowDialog() == true && dialog.ProductoCreado != null)
            {
                // Guardar en base de datos
                Database.Instance.InsertarProducto(dialog.ProductoCreado);
                
                // Recargar lista
                LoadProductos();
                MessageBox.Show($"Producto '{dialog.ProductoCreado.Nombre}' agregado exitosamente", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        
        private void EditarProducto_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is ProductoModel producto)
            {
                var dialog = new ProductoDialog(producto);
                dialog.Owner = Window.GetWindow(this);
                
                if (dialog.ShowDialog() == true && dialog.ProductoCreado != null)
                {
                    // Actualizar en base de datos
                    Database.Instance.ActualizarProducto(dialog.ProductoCreado);
                    LoadProductos();
                    MessageBox.Show($"Producto '{dialog.ProductoCreado.Nombre}' actualizado", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }
        
        private void EliminarProducto_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is ProductoModel producto)
            {
                var result = MessageBox.Show($"¿Eliminar el producto '{producto.Nombre}'?", 
                    "Confirmar eliminación", MessageBoxButton.YesNo, MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    Database.Instance.EliminarProducto(producto.Id);
                    LoadProductos();
                    MessageBox.Show("Producto eliminado", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }
        
        private void ExportarProductos_Click(object sender, RoutedEventArgs e)
        {
            var saveDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Archivo Excel (*.xlsx)|*.xlsx|Archivo CSV (*.csv)|*.csv",
                DefaultExt = ".xlsx",
                FileName = $"productos_{DateTime.Now:yyyyMMdd}"
            };
            
            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    if (saveDialog.FileName.EndsWith(".xlsx"))
                    {
                        ExportarExcel(saveDialog.FileName);
                    }
                    else
                    {
                        ExportarCsv(saveDialog.FileName);
                    }
                    MessageBox.Show($"Se exportaron {_productos.Count} productos exitosamente.", "Exportación Completa", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al exportar: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        
        private void ExportarExcel(string rutaArchivo)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Productos");
            
            // Encabezados con estilo
            var headers = new[] { "Código", "Nombre", "Categoría", "Precio Venta", "Precio Costo", "Stock", "Und/Bulto" };
            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(1, i + 1).Value = headers[i];
                ws.Cell(1, i + 1).Style.Font.Bold = true;
                ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#0ea5e9");
                ws.Cell(1, i + 1).Style.Font.FontColor = XLColor.White;
            }
            
            // Datos
            int row = 2;
            foreach (var p in _productos)
            {
                ws.Cell(row, 1).Value = p.Codigo;
                ws.Cell(row, 2).Value = p.Nombre;
                ws.Cell(row, 3).Value = p.Categoria;
                ws.Cell(row, 4).Value = p.PrecioVenta;
                ws.Cell(row, 5).Value = p.PrecioCosto;
                ws.Cell(row, 6).Value = p.Stock;
                ws.Cell(row, 7).Value = p.UnidadesPorBulto;
                row++;
            }
            
            // Ajustar columnas
            ws.Columns().AdjustToContents();
            
            workbook.SaveAs(rutaArchivo);
        }
        
        private void ExportarCsv(string rutaArchivo)
        {
            var lineas = new List<string>
            {
                "Codigo,Nombre,Categoria,PrecioVenta,PrecioCosto,Stock,UnidadesPorBulto"
            };
            
            foreach (var p in _productos)
            {
                lineas.Add($"\"{p.Codigo}\",\"{p.Nombre}\",\"{p.Categoria}\",{p.PrecioVenta},{p.PrecioCosto},{p.Stock},{p.UnidadesPorBulto}");
            }
            
            System.IO.File.WriteAllLines(rutaArchivo, lineas, System.Text.Encoding.UTF8);
        }
        
        private void GenerarPlantilla_Click(object sender, RoutedEventArgs e)
        {
            var saveDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Archivo Excel (*.xlsx)|*.xlsx",
                DefaultExt = ".xlsx",
                FileName = "plantilla_productos"
            };
            
            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    using var workbook = new XLWorkbook();
                    var ws = workbook.Worksheets.Add("Productos");
                    
                    // Encabezados con instrucciones
                    var headers = new[] { "Código*", "Nombre*", "Categoría", "PrecioCosto*", "MargenGanancia%", "PrecioVenta", "Stock", "UnidadesPorBulto" };
                    for (int i = 0; i < headers.Length; i++)
                    {
                        ws.Cell(1, i + 1).Value = headers[i];
                        ws.Cell(1, i + 1).Style.Font.Bold = true;
                        ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#10b981");
                        ws.Cell(1, i + 1).Style.Font.FontColor = XLColor.White;
                    }
                    
                    // Ejemplo de datos
                    ws.Cell(2, 1).Value = "PROD-001";
                    ws.Cell(2, 2).Value = "Producto de ejemplo";
                    ws.Cell(2, 3).Value = "Alimentos";
                    ws.Cell(2, 4).Value = 4.00;  // Precio Costo
                    ws.Cell(2, 5).Value = 30;    // Margen 30%
                    ws.Cell(2, 6).Value = "";    // PrecioVenta (se calcula auto)
                    ws.Cell(2, 7).Value = 100;
                    ws.Cell(2, 8).Value = 1;
                    
                    // Fila de instrucciones
                    ws.Cell(2, 1).Style.Font.FontColor = XLColor.Gray;
                    ws.Cell(2, 2).Style.Font.FontColor = XLColor.Gray;
                    
                    // Hoja de instrucciones
                    var wsInstr = workbook.Worksheets.Add("Instrucciones");
                    wsInstr.Cell(1, 1).Value = "📋 INSTRUCCIONES DE IMPORTACIÓN";
                    wsInstr.Cell(1, 1).Style.Font.Bold = true;
                    wsInstr.Cell(1, 1).Style.Font.FontSize = 14;
                    
                    wsInstr.Cell(3, 1).Value = "1. Los campos marcados con (*) son obligatorios";
                    wsInstr.Cell(4, 1).Value = "2. Código: Identificador único del producto";
                    wsInstr.Cell(5, 1).Value = "3. Nombre: Nombre descriptivo del producto";
                    wsInstr.Cell(6, 1).Value = "4. Categoría: Alimentos, Bebidas, Lácteos, Limpieza, Otros";
                    wsInstr.Cell(7, 1).Value = "5. PrecioCosto: Precio de costo en USD (obligatorio)";
                    wsInstr.Cell(8, 1).Value = "6. MargenGanancia%: Porcentaje de ganancia (ej: 30 para 30%). Si se omite usa el margen del sistema";
                    wsInstr.Cell(9, 1).Value = "7. PrecioVenta: Precio de venta en USD. Si se deja vacío, se calcula: Costo × (1 + Margen/100)";
                    wsInstr.Cell(10, 1).Value = "8. Stock: Cantidad inicial en inventario";
                    wsInstr.Cell(11, 1).Value = "9. UnidadesPorBulto: Si aplica, cuántas unidades tiene cada bulto";
                    wsInstr.Cell(13, 1).Value = "💡 CÁLCULO AUTOMÁTICO: Si no especifica PrecioVenta, se calcula con el MargenGanancia";
                    wsInstr.Cell(13, 1).Style.Font.FontColor = XLColor.Blue;
                    wsInstr.Cell(14, 1).Value = "⚠️ La fila 2 de la hoja Productos es un ejemplo, elimínela antes de importar";
                    wsInstr.Cell(14, 1).Style.Font.FontColor = XLColor.Red;
                    
                    ws.Columns().AdjustToContents();
                    wsInstr.Column(1).Width = 60;
                    
                    workbook.SaveAs(saveDialog.FileName);
                    MessageBox.Show("Plantilla generada exitosamente.\n\nAbra el archivo y complete los datos de sus productos.", 
                        "Plantilla Creada", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        
        private void ImportarProductos_Click(object sender, RoutedEventArgs e)
        {
            var openDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Archivos Excel/CSV (*.xlsx;*.csv)|*.xlsx;*.csv|Archivo Excel (*.xlsx)|*.xlsx|Archivo CSV (*.csv)|*.csv",
                DefaultExt = ".xlsx"
            };
            
            if (openDialog.ShowDialog() == true)
            {
                try
                {
                    int importados = 0;
                    int errores = 0;
                    
                    if (openDialog.FileName.EndsWith(".xlsx"))
                    {
                        (importados, errores) = ImportarDesdeExcel(openDialog.FileName);
                    }
                    else
                    {
                        (importados, errores) = ImportarDesdeCsv(openDialog.FileName);
                    }
                    
                    LoadProductos();
                    MessageBox.Show($"Importación completa:\n✅ {importados} productos importados\n❌ {errores} errores", 
                        "Resultado", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al importar: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        
        private (int importados, int errores) ImportarDesdeExcel(string rutaArchivo)
        {
            int importados = 0, errores = 0;
            
            using var workbook = new XLWorkbook(rutaArchivo);
            var ws = workbook.Worksheet(1);
            var rows = ws.RowsUsed().Skip(1); // Saltar encabezado
            
            foreach (var row in rows)
            {
                try
                {
                    var codigo = row.Cell(1).GetString();
                    var nombre = row.Cell(2).GetString();
                    
                    if (string.IsNullOrWhiteSpace(codigo) || string.IsNullOrWhiteSpace(nombre))
                    {
                        errores++;
                        continue;
                    }
                    
                    // Leer valores
                    var categoria = row.Cell(3).GetString();
                    var costoBulto = row.Cell(4).TryGetValue<decimal>(out var pc) ? pc : 0;
                    var margen = row.Cell(5).TryGetValue<decimal>(out var mg) ? mg : Database.Instance.ObtenerMargenGanancia();
                    var precioVentaExplicito = row.Cell(6).TryGetValue<decimal>(out var pv) ? pv : 0;
                    var stock = row.Cell(7).TryGetValue<int>(out var s) ? s : 0;
                    var unidadesBulto = row.Cell(8).TryGetValue<int>(out var u) ? u : 1;
                    
                    // Calcular costo unitario (dividir si es bulto)
                    var costoUnitario = unidadesBulto > 1 ? costoBulto / unidadesBulto : costoBulto;
                    
                    // Calcular precio de venta si no se especificó
                    var precioVentaFinal = precioVentaExplicito > 0 
                        ? precioVentaExplicito 
                        : costoUnitario * (1 + margen / 100m);
                    
                    var producto = new ProductoModel
                    {
                        Codigo = codigo,
                        Nombre = nombre,
                        Categoria = categoria,
                        CostoBulto = costoBulto,
                        CostoUnitario = costoUnitario,
                        PrecioCosto = costoUnitario,
                        PrecioVenta = precioVentaFinal,
                        Stock = stock,
                        UnidadesPorBulto = unidadesBulto
                    };
                    
                    Database.Instance.InsertarProducto(producto);
                    importados++;
                }
                catch
                {
                    errores++;
                }
            }
            
            return (importados, errores);
        }
        
        private (int importados, int errores) ImportarDesdeCsv(string rutaArchivo)
        {
            int importados = 0, errores = 0;
            var lineas = System.IO.File.ReadAllLines(rutaArchivo, System.Text.Encoding.UTF8);
            
            // Saltar encabezado
            for (int i = 1; i < lineas.Length; i++)
            {
                try
                {
                    var valores = ParseCsvLine(lineas[i]);
                    if (valores.Length >= 4)
                    {
                        var codigo = valores[0].Trim('"');
                        var nombre = valores[1].Trim('"');
                        var categoria = valores.Length > 2 ? valores[2].Trim('"') : "";
                        var costoBulto = valores.Length > 3 && decimal.TryParse(valores[3], out var pc) ? pc : 0;
                        var margen = valores.Length > 4 && decimal.TryParse(valores[4], out var mg) ? mg : Database.Instance.ObtenerMargenGanancia();
                        var precioVentaExplicito = valores.Length > 5 && decimal.TryParse(valores[5], out var pv) ? pv : 0;
                        var stock = valores.Length > 6 && int.TryParse(valores[6], out var s) ? s : 0;
                        var unidadesBulto = valores.Length > 7 && int.TryParse(valores[7], out var u) ? u : 1;
                        
                        // Calcular costo unitario (dividir si es bulto)
                        var costoUnitario = unidadesBulto > 1 ? costoBulto / unidadesBulto : costoBulto;
                        
                        // Calcular precio de venta si no se especificó
                        var precioVentaFinal = precioVentaExplicito > 0 
                            ? precioVentaExplicito 
                            : costoUnitario * (1 + margen / 100m);
                        
                        var producto = new ProductoModel
                        {
                            Codigo = codigo,
                            Nombre = nombre,
                            Categoria = categoria,
                            CostoBulto = costoBulto,
                            CostoUnitario = costoUnitario,
                            PrecioCosto = costoUnitario,
                            PrecioVenta = precioVentaFinal,
                            Stock = stock,
                            UnidadesPorBulto = unidadesBulto
                        };
                        
                        Database.Instance.InsertarProducto(producto);
                        importados++;
                    }
                }
                catch
                {
                    errores++;
                }
            }
            
            return (importados, errores);
        }
        
        private string[] ParseCsvLine(string line)
        {
            var resultado = new List<string>();
            bool enComillas = false;
            string valor = "";
            
            foreach (char c in line)
            {
                if (c == '"')
                    enComillas = !enComillas;
                else if (c == ',' && !enComillas)
                {
                    resultado.Add(valor);
                    valor = "";
                }
                else
                    valor += c;
            }
            resultado.Add(valor);
            return resultado.ToArray();
        }
    }
}

