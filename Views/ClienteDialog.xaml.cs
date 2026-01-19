using System.Windows;
using PuntoDeVenta.Models;

namespace PuntoDeVenta.Views
{
    public partial class ClienteDialog : Window
    {
        public Contacto? ContactoCreado { get; private set; }
        
        public ClienteDialog()
        {
            InitializeComponent();
            txtNombre.Focus();
        }
        
        // Constructor para editar un contacto existente
        public ClienteDialog(Contacto contacto) : this()
        {
            ContactoCreado = contacto;
            Title = "Editar Contacto";
            
            // Cargar datos
            cmbTipo.SelectedIndex = contacto.Tipo == "Proveedor" ? 1 : 0;
            txtNombre.Text = contacto.Nombre;
            txtCedula.Text = contacto.Cedula;
            txtTelefono.Text = contacto.Telefono;
            txtEmail.Text = contacto.Email;
            txtDireccion.Text = contacto.Direccion;
        }
        
        private void Guardar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtNombre.Text))
            {
                MessageBox.Show("El nombre es requerido", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtNombre.Focus();
                return;
            }
            
            string tipo = cmbTipo.SelectedIndex == 0 ? "Cliente" : "Proveedor";
            
            if (ContactoCreado == null)
            {
                ContactoCreado = new Contacto();
            }
            
            ContactoCreado.Tipo = tipo;
            ContactoCreado.Nombre = txtNombre.Text.Trim();
            ContactoCreado.Cedula = txtCedula.Text?.Trim() ?? "";
            ContactoCreado.Telefono = txtTelefono.Text?.Trim() ?? "";
            ContactoCreado.Email = txtEmail.Text?.Trim() ?? "";
            ContactoCreado.Direccion = txtDireccion.Text?.Trim() ?? "";
            
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
