using System.IO;
using Microsoft.Data.Sqlite;
using PuntoDeVenta.Models;

namespace PuntoDeVenta.Data
{
    public class Database
    {
        private static Database? _instance;
        private static readonly object _lock = new();
        private readonly string _connectionString;
        
        public static Database Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new Database();
                    }
                }
                return _instance;
            }
        }
        
        private Database()
        {
            string dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "PuntoDeVenta", "puntoventa.db");
            
            // Crear directorio si no existe
            Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
            
            _connectionString = $"Data Source={dbPath}";
            InitializeDatabase();
        }
        
        private SqliteConnection GetConnection()
        {
            var connection = new SqliteConnection(_connectionString);
            connection.Open();
            return connection;
        }
        
        private void InitializeDatabase()
        {
            using var connection = GetConnection();
            
            // Tabla Contactos (Clientes y Proveedores unificados)
            ExecuteNonQuery(connection, @"
                CREATE TABLE IF NOT EXISTS Contactos (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Tipo TEXT NOT NULL DEFAULT 'Cliente',
                    Nombre TEXT NOT NULL,
                    Cedula TEXT,
                    Telefono TEXT,
                    Email TEXT,
                    Direccion TEXT,
                    TotalOperaciones INTEGER DEFAULT 0,
                    FechaCreacion TEXT DEFAULT CURRENT_TIMESTAMP
                )");
            
            // Tabla Productos
            ExecuteNonQuery(connection, @"
                CREATE TABLE IF NOT EXISTS Productos (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Codigo TEXT UNIQUE,
                    Nombre TEXT NOT NULL,
                    Categoria TEXT,
                    UnidadMedida TEXT DEFAULT 'Unidad',
                    UnidadesPorBulto INTEGER DEFAULT 1,
                    PrecioCosto REAL DEFAULT 0,
                    PrecioVenta REAL DEFAULT 0,
                    Stock INTEGER DEFAULT 0,
                    StockMinimo INTEGER DEFAULT 5,
                    Descripcion TEXT,
                    FechaCreacion TEXT DEFAULT CURRENT_TIMESTAMP
                )");
            
            // Tabla Ventas
            ExecuteNonQuery(connection, @"
                CREATE TABLE IF NOT EXISTS Ventas (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    NumeroFactura TEXT,
                    ClienteId INTEGER,
                    ClienteNombre TEXT DEFAULT 'Cliente General',
                    Fecha TEXT DEFAULT CURRENT_TIMESTAMP,
                    Subtotal REAL DEFAULT 0,
                    Descuento REAL DEFAULT 0,
                    Total REAL DEFAULT 0,
                    MetodoPago TEXT DEFAULT 'Efectivo',
                    Estado TEXT DEFAULT 'Completada',
                    FOREIGN KEY (ClienteId) REFERENCES Contactos(Id)
                )");
            
            // Tabla VentaDetalles
            ExecuteNonQuery(connection, @"
                CREATE TABLE IF NOT EXISTS VentaDetalles (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    VentaId INTEGER NOT NULL,
                    ProductoId INTEGER,
                    ProductoNombre TEXT,
                    Cantidad INTEGER DEFAULT 1,
                    PrecioUnitario REAL DEFAULT 0,
                    FOREIGN KEY (VentaId) REFERENCES Ventas(Id),
                    FOREIGN KEY (ProductoId) REFERENCES Productos(Id)
                )");
            
            // Tabla Compras
            ExecuteNonQuery(connection, @"
                CREATE TABLE IF NOT EXISTS Compras (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    NumeroFactura TEXT,
                    ProveedorId INTEGER,
                    ProveedorNombre TEXT,
                    Fecha TEXT DEFAULT CURRENT_TIMESTAMP,
                    TotalUsd REAL DEFAULT 0,
                    TotalBs REAL DEFAULT 0,
                    Estado TEXT DEFAULT 'Completada',
                    FOREIGN KEY (ProveedorId) REFERENCES Contactos(Id)
                )");
            
            // Tabla CompraDetalles
            ExecuteNonQuery(connection, @"
                CREATE TABLE IF NOT EXISTS CompraDetalles (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    CompraId INTEGER NOT NULL,
                    ProductoId INTEGER,
                    ProductoNombre TEXT,
                    Cantidad INTEGER DEFAULT 1,
                    PrecioUnitario REAL DEFAULT 0,
                    FOREIGN KEY (CompraId) REFERENCES Compras(Id),
                    FOREIGN KEY (ProductoId) REFERENCES Productos(Id)
                )");
            
            // Tabla Configuracion
            ExecuteNonQuery(connection, @"
                CREATE TABLE IF NOT EXISTS Configuracion (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Clave TEXT UNIQUE NOT NULL,
                    Valor TEXT
                )");
            
            // Insertar configuración por defecto si no existe
            InsertDefaultConfig(connection);
            
            // Agregar columnas nuevas si no existen (migración)
            AddColumnIfNotExists(connection, "Productos", "CostoBulto", "REAL DEFAULT 0");
            AddColumnIfNotExists(connection, "Productos", "CostoUnitario", "REAL DEFAULT 0");
            AddColumnIfNotExists(connection, "CompraDetalles", "TipoCompra", "TEXT DEFAULT 'Unidad'");
            
            // Insertar datos de ejemplo si las tablas están vacías
            InsertSampleData(connection);
        }
        
        private void AddColumnIfNotExists(SqliteConnection connection, string table, string column, string definition)
        {
            try
            {
                ExecuteNonQuery(connection, $"ALTER TABLE {table} ADD COLUMN {column} {definition}");
            }
            catch (SqliteException)
            {
                // La columna ya existe, ignorar
            }
        }
        
        private void InsertDefaultConfig(SqliteConnection connection)
        {
            var configs = new Dictionary<string, string>
            {
                { "TasaCambio", "45.50" },
                { "NombreNegocio", "Mi Punto de Venta" },
                { "Direccion", "" },
                { "Telefono", "" },
                { "RIF", "" },
                { "IVA", "16" },
                { "MargenGanancia", "30" }  // 30% por defecto
            };
            
            foreach (var config in configs)
            {
                ExecuteNonQuery(connection, 
                    "INSERT OR IGNORE INTO Configuracion (Clave, Valor) VALUES (@clave, @valor)",
                    ("@clave", config.Key), ("@valor", config.Value));
            }
        }
        
        private void InsertSampleData(SqliteConnection connection)
        {
            // Verificar si ya hay datos
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM Productos";
            var count = Convert.ToInt32(cmd.ExecuteScalar());
            
            if (count > 0) return; // Ya hay datos
            
            // Productos de ejemplo
            var productos = new[]
            {
                ("001", "Harina Pan 1kg", "Alimentos", 2.00m, 2.50m, 50),
                ("002", "Aceite de Maíz 1L", "Alimentos", 3.50m, 4.25m, 30),
                ("003", "Azúcar 1kg", "Alimentos", 1.40m, 1.80m, 45),
                ("004", "Arroz 1kg", "Alimentos", 1.60m, 2.00m, 60),
                ("005", "Leche en Polvo 400g", "Lácteos", 4.50m, 5.50m, 25),
                ("006", "Pasta 500g", "Alimentos", 1.20m, 1.50m, 80),
                ("007", "Café molido 250g", "Bebidas", 3.00m, 3.75m, 40),
                ("008", "Margarina 500g", "Lácteos", 1.80m, 2.25m, 35),
                ("009", "Detergente 1L", "Limpieza", 2.40m, 3.00m, 20),
                ("010", "Jabón en barra x3", "Limpieza", 2.20m, 2.75m, 15),
            };
            
            foreach (var p in productos)
            {
                ExecuteNonQuery(connection,
                    @"INSERT INTO Productos (Codigo, Nombre, Categoria, PrecioCosto, PrecioVenta, Stock) 
                      VALUES (@codigo, @nombre, @cat, @costo, @venta, @stock)",
                    ("@codigo", p.Item1), ("@nombre", p.Item2), ("@cat", p.Item3),
                    ("@costo", p.Item4), ("@venta", p.Item5), ("@stock", p.Item6));
            }
            
            // Contactos de ejemplo (Clientes)
            var clientes = new[]
            {
                ("Cliente", "Juan Pérez", "V-12345678", "0412-1234567"),
                ("Cliente", "María García", "V-87654321", "0414-7654321"),
                ("Cliente", "Carlos Rodríguez", "V-11223344", "0416-1122334"),
            };
            
            foreach (var c in clientes)
            {
                ExecuteNonQuery(connection,
                    "INSERT INTO Contactos (Tipo, Nombre, Cedula, Telefono) VALUES (@tipo, @nombre, @cedula, @tel)",
                    ("@tipo", c.Item1), ("@nombre", c.Item2), ("@cedula", c.Item3), ("@tel", c.Item4));
            }
            
            // Contactos de ejemplo (Proveedores)
            var proveedores = new[]
            {
                ("Proveedor", "Distribuidora Central", "J-12345678-0", "0212-1234567"),
                ("Proveedor", "Mayorista del Este", "J-87654321-0", "0212-7654321"),
                ("Proveedor", "Alimentos del Valle", "J-11223344-0", "0212-1122334"),
            };
            
            foreach (var p in proveedores)
            {
                ExecuteNonQuery(connection,
                    "INSERT INTO Contactos (Tipo, Nombre, Cedula, Telefono) VALUES (@tipo, @nombre, @cedula, @tel)",
                    ("@tipo", p.Item1), ("@nombre", p.Item2), ("@cedula", p.Item3), ("@tel", p.Item4));
            }
        }
        
        private void ExecuteNonQuery(SqliteConnection connection, string sql, params (string, object)[] parameters)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            foreach (var (name, value) in parameters)
            {
                cmd.Parameters.AddWithValue(name, value);
            }
            cmd.ExecuteNonQuery();
        }
        
        #region Contactos CRUD
        
        public List<Contacto> ObtenerContactos(string? tipo = null)
        {
            var contactos = new List<Contacto>();
            using var connection = GetConnection();
            using var cmd = connection.CreateCommand();
            
            cmd.CommandText = tipo == null 
                ? "SELECT * FROM Contactos ORDER BY Nombre"
                : "SELECT * FROM Contactos WHERE Tipo = @tipo ORDER BY Nombre";
            
            if (tipo != null)
                cmd.Parameters.AddWithValue("@tipo", tipo);
            
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                contactos.Add(new Contacto
                {
                    Id = reader.GetInt32(0),
                    Tipo = reader.GetString(1),
                    Nombre = reader.GetString(2),
                    Cedula = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    Telefono = reader.IsDBNull(4) ? "" : reader.GetString(4),
                    Email = reader.IsDBNull(5) ? "" : reader.GetString(5),
                    Direccion = reader.IsDBNull(6) ? "" : reader.GetString(6),
                    TotalOperaciones = reader.IsDBNull(7) ? 0 : reader.GetInt32(7),
                });
            }
            return contactos;
        }
        
        public void GuardarContacto(Contacto contacto)
        {
            using var connection = GetConnection();
            using var cmd = connection.CreateCommand();
            
            if (contacto.Id == 0)
            {
                cmd.CommandText = @"INSERT INTO Contactos (Tipo, Nombre, Cedula, Telefono, Email, Direccion) 
                                    VALUES (@tipo, @nombre, @cedula, @tel, @email, @dir)";
            }
            else
            {
                cmd.CommandText = @"UPDATE Contactos SET Tipo=@tipo, Nombre=@nombre, Cedula=@cedula, 
                                    Telefono=@tel, Email=@email, Direccion=@dir WHERE Id=@id";
                cmd.Parameters.AddWithValue("@id", contacto.Id);
            }
            
            cmd.Parameters.AddWithValue("@tipo", contacto.Tipo);
            cmd.Parameters.AddWithValue("@nombre", contacto.Nombre);
            cmd.Parameters.AddWithValue("@cedula", contacto.Cedula ?? "");
            cmd.Parameters.AddWithValue("@tel", contacto.Telefono ?? "");
            cmd.Parameters.AddWithValue("@email", contacto.Email ?? "");
            cmd.Parameters.AddWithValue("@dir", contacto.Direccion ?? "");
            cmd.ExecuteNonQuery();
        }
        
        public void EliminarContacto(int id)
        {
            using var connection = GetConnection();
            ExecuteNonQuery(connection, "DELETE FROM Contactos WHERE Id = @id", ("@id", id));
        }
        
        public void IncrementarOperacionesContacto(int? contactoId)
        {
            if (!contactoId.HasValue) return;
            using var connection = GetConnection();
            ExecuteNonQuery(connection, 
                "UPDATE Contactos SET TotalOperaciones = TotalOperaciones + 1 WHERE Id = @id", 
                ("@id", contactoId.Value));
        }
        
        #endregion
        
        #region Productos CRUD
        
        public List<ProductoModel> ObtenerProductos()
        {
            var productos = new List<ProductoModel>();
            using var connection = GetConnection();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT * FROM Productos ORDER BY Nombre";
            
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                productos.Add(new ProductoModel
                {
                    Id = reader.GetInt32(0),
                    Codigo = reader.IsDBNull(1) ? "" : reader.GetString(1),
                    Nombre = reader.GetString(2),
                    Categoria = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    UnidadMedida = reader.IsDBNull(4) ? "Unidad" : reader.GetString(4),
                    UnidadesPorBulto = reader.IsDBNull(5) ? 1 : reader.GetInt32(5),
                    PrecioCosto = reader.IsDBNull(6) ? 0 : reader.GetDecimal(6),
                    PrecioVenta = reader.IsDBNull(7) ? 0 : reader.GetDecimal(7),
                    Stock = reader.IsDBNull(8) ? 0 : reader.GetInt32(8),
                    StockMinimo = reader.IsDBNull(9) ? 5 : reader.GetInt32(9),
                    Descripcion = reader.IsDBNull(10) ? "" : reader.GetString(10),
                });
            }
            return productos;
        }
        
        public void GuardarProducto(ProductoModel producto)
        {
            using var connection = GetConnection();
            using var cmd = connection.CreateCommand();
            
            if (producto.Id == 0)
            {
                cmd.CommandText = @"INSERT INTO Productos (Codigo, Nombre, Categoria, UnidadMedida, UnidadesPorBulto, PrecioCosto, PrecioVenta, Stock, StockMinimo, Descripcion) 
                                    VALUES (@codigo, @nombre, @cat, @unidad, @bulto, @costo, @venta, @stock, @stockMin, @desc)";
            }
            else
            {
                cmd.CommandText = @"UPDATE Productos SET Codigo=@codigo, Nombre=@nombre, Categoria=@cat, UnidadMedida=@unidad, 
                                    UnidadesPorBulto=@bulto, PrecioCosto=@costo, PrecioVenta=@venta, Stock=@stock, StockMinimo=@stockMin, Descripcion=@desc WHERE Id=@id";
                cmd.Parameters.AddWithValue("@id", producto.Id);
            }
            
            cmd.Parameters.AddWithValue("@codigo", producto.Codigo ?? "");
            cmd.Parameters.AddWithValue("@nombre", producto.Nombre);
            cmd.Parameters.AddWithValue("@cat", producto.Categoria ?? "");
            cmd.Parameters.AddWithValue("@unidad", producto.UnidadMedida ?? "Unidad");
            cmd.Parameters.AddWithValue("@bulto", producto.UnidadesPorBulto);
            cmd.Parameters.AddWithValue("@costo", producto.PrecioCosto);
            cmd.Parameters.AddWithValue("@venta", producto.PrecioVenta);
            cmd.Parameters.AddWithValue("@stock", producto.Stock);
            cmd.Parameters.AddWithValue("@stockMin", producto.StockMinimo);
            cmd.Parameters.AddWithValue("@desc", producto.Descripcion ?? "");
            cmd.ExecuteNonQuery();
        }
        
        public void EliminarProducto(int id)
        {
            using var connection = GetConnection();
            ExecuteNonQuery(connection, "DELETE FROM Productos WHERE Id = @id", ("@id", id));
        }
        
        public void InsertarProducto(ProductoModel producto)
        {
            producto.Id = 0; // Asegurar que es nuevo
            GuardarProducto(producto);
        }
        
        public void ActualizarProducto(ProductoModel producto)
        {
            GuardarProducto(producto);
        }
        
        public void ActualizarStock(int productoId, int cantidad)
        {
            using var connection = GetConnection();
            ExecuteNonQuery(connection, 
                "UPDATE Productos SET Stock = Stock + @cantidad WHERE Id = @id",
                ("@cantidad", cantidad), ("@id", productoId));
        }
        
        #endregion
        
        #region Configuración
        
        public string ObtenerConfiguracion(string clave, string valorDefecto = "")
        {
            using var connection = GetConnection();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT Valor FROM Configuracion WHERE Clave = @clave";
            cmd.Parameters.AddWithValue("@clave", clave);
            var result = cmd.ExecuteScalar();
            return result?.ToString() ?? valorDefecto;
        }
        
        public void GuardarConfiguracion(string clave, string valor)
        {
            using var connection = GetConnection();
            ExecuteNonQuery(connection,
                "INSERT OR REPLACE INTO Configuracion (Clave, Valor) VALUES (@clave, @valor)",
                ("@clave", clave), ("@valor", valor));
        }
        
        public decimal ObtenerTasaCambio()
        {
            var tasa = ObtenerConfiguracion("TasaCambio", "45.50");
            return decimal.TryParse(tasa, out var result) ? result : 45.50m;
        }
        
        public void ActualizarTasaCambio(decimal nuevaTasa)
        {
            GuardarConfiguracion("TasaCambio", nuevaTasa.ToString("F2"));
        }
        
        #endregion
        
        #region Ventas
        
        public void InsertarVenta(VentaModel venta, List<VentaDetalle> detalles)
        {
            using var connection = GetConnection();
            using var transaction = connection.BeginTransaction();
            
            try
            {
                // Generar número de factura
                using var cmdNum = connection.CreateCommand();
                cmdNum.CommandText = "SELECT COALESCE(MAX(Id), 0) + 1 FROM Ventas";
                var nextId = Convert.ToInt32(cmdNum.ExecuteScalar());
                venta.NumeroFactura = $"V-{nextId:D6}";
                
                // Insertar venta
                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"INSERT INTO Ventas (NumeroFactura, ClienteId, ClienteNombre, Fecha, Subtotal, Descuento, Total, MetodoPago, Estado)
                                    VALUES (@numero, @clienteId, @clienteNombre, @fecha, @subtotal, @descuento, @total, @metodo, @estado);
                                    SELECT last_insert_rowid();";
                cmd.Parameters.AddWithValue("@numero", venta.NumeroFactura);
                cmd.Parameters.AddWithValue("@clienteId", venta.ClienteId ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@clienteNombre", venta.ClienteNombre);
                cmd.Parameters.AddWithValue("@fecha", venta.Fecha.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("@subtotal", venta.Subtotal);
                cmd.Parameters.AddWithValue("@descuento", venta.Descuento);
                cmd.Parameters.AddWithValue("@total", venta.Total);
                cmd.Parameters.AddWithValue("@metodo", venta.MetodoPago);
                cmd.Parameters.AddWithValue("@estado", venta.Estado);
                
                var ventaId = Convert.ToInt32(cmd.ExecuteScalar());
                
                // Insertar detalles
                foreach (var detalle in detalles)
                {
                    using var cmdDet = connection.CreateCommand();
                    cmdDet.CommandText = @"INSERT INTO VentaDetalles (VentaId, ProductoId, ProductoNombre, Cantidad, PrecioUnitario)
                                           VALUES (@ventaId, @productoId, @productoNombre, @cantidad, @precio)";
                    cmdDet.Parameters.AddWithValue("@ventaId", ventaId);
                    cmdDet.Parameters.AddWithValue("@productoId", detalle.ProductoId);
                    cmdDet.Parameters.AddWithValue("@productoNombre", detalle.ProductoNombre);
                    cmdDet.Parameters.AddWithValue("@cantidad", detalle.Cantidad);
                    cmdDet.Parameters.AddWithValue("@precio", detalle.PrecioUnitario);
                    cmdDet.ExecuteNonQuery();
                }
                
                transaction.Commit();
                
                // Incrementar operaciones del cliente
                if (venta.ClienteId.HasValue)
                {
                    IncrementarOperacionesContacto(venta.ClienteId);
                }
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
        
        public List<VentaModel> ObtenerVentas(int limite = 50)
        {
            var ventas = new List<VentaModel>();
            using var connection = GetConnection();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"SELECT Id, NumeroFactura, ClienteId, ClienteNombre, Fecha, Subtotal, Descuento, Total, MetodoPago, Estado 
                               FROM Ventas ORDER BY Fecha DESC LIMIT @limite";
            cmd.Parameters.AddWithValue("@limite", limite);
            
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                ventas.Add(new VentaModel
                {
                    Id = reader.GetInt32(0),
                    NumeroFactura = reader.IsDBNull(1) ? "" : reader.GetString(1),
                    ClienteId = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                    ClienteNombre = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    Fecha = DateTime.TryParse(reader.GetString(4), out var fecha) ? fecha : DateTime.Now,
                    Subtotal = reader.IsDBNull(5) ? 0 : reader.GetDecimal(5),
                    Descuento = reader.IsDBNull(6) ? 0 : reader.GetDecimal(6),
                    Total = reader.IsDBNull(7) ? 0 : reader.GetDecimal(7),
                    MetodoPago = reader.IsDBNull(8) ? "" : reader.GetString(8),
                    Estado = reader.IsDBNull(9) ? "Completada" : reader.GetString(9)
                });
            }
            return ventas;
        }
        
        public (decimal ventasUsd, int transacciones) ObtenerVentasDelDia()
        {
            using var connection = GetConnection();
            using var cmd = connection.CreateCommand();
            var hoy = DateTime.Now.ToString("yyyy-MM-dd");
            cmd.CommandText = @"SELECT COALESCE(SUM(Total), 0), COUNT(*) 
                               FROM Ventas 
                               WHERE date(Fecha) = @hoy AND Estado = 'Completada'";
            cmd.Parameters.AddWithValue("@hoy", hoy);
            
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return (reader.GetDecimal(0), reader.GetInt32(1));
            }
            return (0, 0);
        }
        
        public int ObtenerCantidadProductosVenta(int ventaId)
        {
            using var connection = GetConnection();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT COALESCE(SUM(Cantidad), 0) FROM VentaDetalles WHERE VentaId = @ventaId";
            cmd.Parameters.AddWithValue("@ventaId", ventaId);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }
        
        public (int totalProductos, int stockBajo) ObtenerEstadisticasProductos()
        {
            using var connection = GetConnection();
            
            using var cmd1 = connection.CreateCommand();
            cmd1.CommandText = "SELECT COUNT(*) FROM Productos";
            int total = Convert.ToInt32(cmd1.ExecuteScalar());
            
            using var cmd2 = connection.CreateCommand();
            cmd2.CommandText = "SELECT COUNT(*) FROM Productos WHERE Stock <= StockMinimo";
            int bajo = Convert.ToInt32(cmd2.ExecuteScalar());
            
            return (total, bajo);
        }
        
        public List<VentaModel> ObtenerUltimasVentas(int cantidad = 10)
        {
            var ventas = new List<VentaModel>();
            using var connection = GetConnection();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"SELECT Id, NumeroFactura, ClienteNombre, Fecha, Total 
                               FROM Ventas 
                               ORDER BY Fecha DESC 
                               LIMIT @cantidad";
            cmd.Parameters.AddWithValue("@cantidad", cantidad);
            
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                ventas.Add(new VentaModel
                {
                    Id = reader.GetInt32(0),
                    NumeroFactura = reader.IsDBNull(1) ? "" : reader.GetString(1),
                    ClienteNombre = reader.IsDBNull(2) ? "Cliente General" : reader.GetString(2),
                    Fecha = DateTime.TryParse(reader.GetString(3), out var fecha) ? fecha : DateTime.Now,
                    Total = reader.IsDBNull(4) ? 0 : reader.GetDecimal(4)
                });
            }
            return ventas;
        }
        
        #endregion
        
        #region Compras
        
        public decimal ObtenerMargenGanancia()
        {
            var margenStr = ObtenerConfiguracion("MargenGanancia", "30");
            return decimal.TryParse(margenStr, out var margen) ? margen / 100m : 0.30m;
        }
        
        public void InsertarCompra(CompraModel compra, List<CompraDetalle> detalles)
        {
            using var connection = GetConnection();
            using var transaction = connection.BeginTransaction();
            
            try
            {
                decimal tasaCambio = ObtenerTasaCambio();
                decimal margen = ObtenerMargenGanancia();
                
                // Insertar compra
                using var cmdCompra = connection.CreateCommand();
                cmdCompra.Transaction = transaction;
                cmdCompra.CommandText = @"
                    INSERT INTO Compras (NumeroFactura, ProveedorId, ProveedorNombre, Fecha, TotalUsd, TotalBs, Estado)
                    VALUES (@factura, @proveedorId, @proveedorNombre, @fecha, @totalUsd, @totalBs, @estado);
                    SELECT last_insert_rowid();";
                
                cmdCompra.Parameters.AddWithValue("@factura", compra.NumeroFactura);
                cmdCompra.Parameters.AddWithValue("@proveedorId", compra.ProveedorId ?? (object)DBNull.Value);
                cmdCompra.Parameters.AddWithValue("@proveedorNombre", compra.ProveedorNombre);
                cmdCompra.Parameters.AddWithValue("@fecha", compra.Fecha.ToString("yyyy-MM-dd HH:mm:ss"));
                cmdCompra.Parameters.AddWithValue("@totalUsd", compra.TotalUsd);
                cmdCompra.Parameters.AddWithValue("@totalBs", compra.TotalUsd * tasaCambio);
                cmdCompra.Parameters.AddWithValue("@estado", compra.Estado);
                
                int compraId = Convert.ToInt32(cmdCompra.ExecuteScalar());
                
                // Insertar detalles y actualizar productos
                foreach (var detalle in detalles)
                {
                    // Insertar detalle
                    using var cmdDetalle = connection.CreateCommand();
                    cmdDetalle.Transaction = transaction;
                    cmdDetalle.CommandText = @"
                        INSERT INTO CompraDetalles (CompraId, ProductoId, ProductoNombre, TipoCompra, Cantidad, PrecioUnitario)
                        VALUES (@compraId, @productoId, @nombre, @tipoCompra, @cantidad, @precio)";
                    
                    cmdDetalle.Parameters.AddWithValue("@compraId", compraId);
                    cmdDetalle.Parameters.AddWithValue("@productoId", detalle.ProductoId ?? (object)DBNull.Value);
                    cmdDetalle.Parameters.AddWithValue("@nombre", detalle.ProductoNombre);
                    cmdDetalle.Parameters.AddWithValue("@tipoCompra", detalle.TipoCompra);
                    cmdDetalle.Parameters.AddWithValue("@cantidad", detalle.Cantidad);
                    cmdDetalle.Parameters.AddWithValue("@precio", detalle.PrecioUnitario);
                    cmdDetalle.ExecuteNonQuery();
                    
                    // Si hay ProductoId, actualizar stock y precios
                    if (detalle.ProductoId.HasValue)
                    {
                        ActualizarProductoDesdeCompra(
                            connection, transaction,
                            detalle.ProductoId.Value,
                            detalle.TipoCompra,
                            detalle.Cantidad,
                            detalle.PrecioUnitario,
                            margen
                        );
                    }
                }
                
                transaction.Commit();
                
                // Incrementar operaciones del proveedor
                if (compra.ProveedorId.HasValue)
                {
                    IncrementarOperacionesContacto(compra.ProveedorId);
                }
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
        
        private void ActualizarProductoDesdeCompra(
            SqliteConnection connection,
            SqliteTransaction transaction,
            int productoId,
            string tipoCompra,
            int cantidad,
            decimal precioCompra,
            decimal margen)
        {
            // Primero obtener el producto actual
            using var cmdGet = connection.CreateCommand();
            cmdGet.Transaction = transaction;
            cmdGet.CommandText = "SELECT UnidadesPorBulto, Stock FROM Productos WHERE Id = @id";
            cmdGet.Parameters.AddWithValue("@id", productoId);
            
            using var reader = cmdGet.ExecuteReader();
            if (!reader.Read()) return;
            
            int unidadesPorBulto = reader.GetInt32(0);
            int stockActual = reader.GetInt32(1);
            reader.Close();
            
            decimal costoUnitario;
            decimal costoBulto;
            int unidadesAAgregar;
            
            if (tipoCompra == "Bulto")
            {
                // Compra por bulto
                costoBulto = precioCompra;
                costoUnitario = unidadesPorBulto > 0 ? precioCompra / unidadesPorBulto : precioCompra;
                unidadesAAgregar = cantidad * unidadesPorBulto;
            }
            else
            {
                // Compra por unidad
                costoUnitario = precioCompra;
                costoBulto = precioCompra * unidadesPorBulto;
                unidadesAAgregar = cantidad;
            }
            
            decimal precioVenta = costoUnitario * (1 + margen);
            
            // Actualizar producto
            using var cmdUpdate = connection.CreateCommand();
            cmdUpdate.Transaction = transaction;
            cmdUpdate.CommandText = @"
                UPDATE Productos 
                SET CostoBulto = @costoBulto,
                    CostoUnitario = @costoUnitario,
                    PrecioCosto = @costoUnitario,
                    PrecioVenta = @precioVenta,
                    Stock = @nuevoStock
                WHERE Id = @id";
            
            cmdUpdate.Parameters.AddWithValue("@costoBulto", costoBulto);
            cmdUpdate.Parameters.AddWithValue("@costoUnitario", costoUnitario);
            cmdUpdate.Parameters.AddWithValue("@precioVenta", precioVenta);
            cmdUpdate.Parameters.AddWithValue("@nuevoStock", stockActual + unidadesAAgregar);
            cmdUpdate.Parameters.AddWithValue("@id", productoId);
            cmdUpdate.ExecuteNonQuery();
        }
        
        public List<CompraModel> ObtenerCompras(int limite = 50)
        {
            var compras = new List<CompraModel>();
            using var connection = GetConnection();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"SELECT Id, NumeroFactura, ProveedorId, ProveedorNombre, Fecha, TotalUsd, TotalBs, Estado 
                               FROM Compras ORDER BY Fecha DESC LIMIT @limite";
            cmd.Parameters.AddWithValue("@limite", limite);
            
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                compras.Add(new CompraModel
                {
                    Id = reader.GetInt32(0),
                    NumeroFactura = reader.IsDBNull(1) ? "" : reader.GetString(1),
                    ProveedorId = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                    ProveedorNombre = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    Fecha = DateTime.TryParse(reader.GetString(4), out var fecha) ? fecha : DateTime.Now,
                    TotalUsd = reader.IsDBNull(5) ? 0 : reader.GetDecimal(5),
                    TotalBs = reader.IsDBNull(6) ? 0 : reader.GetDecimal(6),
                    Estado = reader.IsDBNull(7) ? "Completada" : reader.GetString(7)
                });
            }
            return compras;
        }
        
        public List<CompraDetalle> ObtenerDetallesCompra(int compraId)
        {
            var detalles = new List<CompraDetalle>();
            using var connection = GetConnection();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"SELECT Id, CompraId, ProductoId, ProductoNombre, TipoCompra, Cantidad, PrecioUnitario 
                               FROM CompraDetalles WHERE CompraId = @compraId";
            cmd.Parameters.AddWithValue("@compraId", compraId);
            
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                detalles.Add(new CompraDetalle
                {
                    Id = reader.GetInt32(0),
                    CompraId = reader.GetInt32(1),
                    ProductoId = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                    ProductoNombre = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    TipoCompra = reader.IsDBNull(4) ? "Unidad" : reader.GetString(4),
                    Cantidad = reader.GetInt32(5),
                    PrecioUnitario = reader.GetDecimal(6)
                });
            }
            return detalles;
        }
        
        #endregion
    }
}
