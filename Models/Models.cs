namespace PuntoDeVenta.Models
{
    /// <summary>
    /// Modelo unificado para Clientes y Proveedores
    /// </summary>
    public class Contacto
    {
        public int Id { get; set; }
        public string Tipo { get; set; } = "Cliente"; // "Cliente" o "Proveedor"
        public string Nombre { get; set; } = "";
        public string Cedula { get; set; } = "";
        public string Telefono { get; set; } = "";
        public string Email { get; set; } = "";
        public string Direccion { get; set; } = "";
        public int TotalOperaciones { get; set; } = 0;
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
    }
    
    /// <summary>
    /// Modelo de Producto
    /// </summary>
    public class ProductoModel
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = "";
        public string Nombre { get; set; } = "";
        public string Categoria { get; set; } = "";
        public string UnidadMedida { get; set; } = "Unidad";
        public int UnidadesPorBulto { get; set; } = 1;
        public decimal CostoBulto { get; set; }        // Costo de compra por bulto
        public decimal CostoUnitario { get; set; }     // Costo de compra por unidad
        public decimal PrecioCosto { get; set; }       // Alias de CostoUnitario (compatibilidad)
        public decimal PrecioVenta { get; set; }
        public int Stock { get; set; }
        public int StockMinimo { get; set; } = 5;
        public string Descripcion { get; set; } = "";
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        
        // Propiedades calculadas
        public decimal Precio => PrecioVenta;
        public string PrecioFormateado => $"$ {PrecioVenta:N2}";
        public string CostoFormateado => $"$ {CostoUnitario:N2}";
        public string InfoBulto => UnidadesPorBulto > 1 ? $"{UnidadesPorBulto} und/bulto" : "";
    }
    
    /// <summary>
    /// Modelo de Venta
    /// </summary>
    public class VentaModel
    {
        public int Id { get; set; }
        public string NumeroFactura { get; set; } = "";
        public int? ClienteId { get; set; }
        public string ClienteNombre { get; set; } = "Cliente General";
        public DateTime Fecha { get; set; } = DateTime.Now;
        public decimal Subtotal { get; set; }
        public decimal Descuento { get; set; }
        public decimal Total { get; set; }
        public string MetodoPago { get; set; } = "Efectivo";
        public string Estado { get; set; } = "Completada";
    }
    
    /// <summary>
    /// Detalle de venta (productos vendidos)
    /// </summary>
    public class VentaDetalle
    {
        public int Id { get; set; }
        public int VentaId { get; set; }
        public int ProductoId { get; set; }
        public string ProductoNombre { get; set; } = "";
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Subtotal => Cantidad * PrecioUnitario;
    }
    
    /// <summary>
    /// Modelo de Compra
    /// </summary>
    public class CompraModel
    {
        public int Id { get; set; }
        public string NumeroFactura { get; set; } = "";
        public int? ProveedorId { get; set; }
        public string ProveedorNombre { get; set; } = "";
        public DateTime Fecha { get; set; } = DateTime.Now;
        public decimal TotalUsd { get; set; }
        public decimal TotalBs { get; set; }
        public string Estado { get; set; } = "Completada";
        
        // Propiedades para UI bindings
        public string Proveedor => ProveedorNombre;
        public string FechaFormateada => Fecha.ToString("dd/MM/yyyy");
        public string TotalUsdFormateado => $"$ {TotalUsd:N2}";
        public string TotalBsFormateado => $"Bs {TotalBs:N2}";
    }
    
    /// <summary>
    /// Detalle de compra (productos comprados)
    /// </summary>
    public class CompraDetalle
    {
        public int Id { get; set; }
        public int CompraId { get; set; }
        public int? ProductoId { get; set; }
        public string ProductoNombre { get; set; } = "";
        public string TipoCompra { get; set; } = "Unidad"; // "Bulto" o "Unidad"
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Subtotal => Cantidad * PrecioUnitario;
    }
    
    /// <summary>
    /// Configuración del sistema
    /// </summary>
    public class Configuracion
    {
        public int Id { get; set; }
        public string Clave { get; set; } = "";
        public string Valor { get; set; } = "";
    }
}
