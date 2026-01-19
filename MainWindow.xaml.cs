using System.Windows;
using System.Windows.Controls;
using PuntoDeVenta.Views;
using PuntoDeVenta.Services;

namespace PuntoDeVenta
{
    public partial class MainWindow : Window
    {
        private Button? _activeButton;
        
        public MainWindow()
        {
            InitializeComponent();
            
            // Verificar licencia antes de continuar
            if (!CheckLicense())
            {
                Application.Current.Shutdown();
                return;
            }
            
            // Cargar Dashboard por defecto
            NavigateTo("Dashboard");
            SetActiveButton(btnDashboard);
            
            // Verificar actualizaciones en segundo plano
            CheckForUpdatesAsync();
        }
        
        private bool CheckLicense()
        {
            if (!LicenseService.Instance.IsLicenseValid())
            {
                var dialog = new LicenseDialog();
                var result = dialog.ShowDialog();
                
                // Si no activó licencia, cerrar aplicación
                if (result != true || !dialog.LicenseActivated)
                {
                    return false;
                }
            }
            return true;
        }
        
        private async void CheckForUpdatesAsync()
        {
            try
            {
                await System.Threading.Tasks.Task.Delay(3000); // Esperar 3 segundos antes de verificar
                
                var updateInfo = await UpdateService.Instance.CheckForUpdatesAsync();
                
                if (updateInfo.IsNewVersionAvailable)
                {
                    var result = MessageBox.Show(
                        $"¡Nueva versión disponible!\n\n" +
                        $"Versión actual: {UpdateService.Instance.GetCurrentVersion()}\n" +
                        $"Nueva versión: {updateInfo.Version}\n\n" +
                        $"{updateInfo.ReleaseNotes}\n\n" +
                        $"¿Desea abrir la página de descarga?",
                        "🔄 Actualización Disponible",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);
                    
                    if (result == MessageBoxResult.Yes && !string.IsNullOrEmpty(updateInfo.DownloadUrl))
                    {
                        UpdateService.Instance.OpenDownloadPage(updateInfo.DownloadUrl);
                    }
                }
            }
            catch
            {
                // Silenciar errores de verificación de actualizaciones
            }
        }
        
        private void NavButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string page)
            {
                NavigateTo(page);
                SetActiveButton(button);
            }
        }
        
        public void NavigateTo(string page)
        {
            UserControl? view = page switch
            {
                "Dashboard" => new DashboardView(),
                "Ventas" => new VentasView(),
                "Productos" => new ProductosView(),
                "Clientes" => new ClientesView(),
                "Compras" => new ComprasView(),
                "Reportes" => new ReportesView(),
                "Config" => new ConfigView(),
                _ => new DashboardView()
            };
            
            MainContent.Content = view;
        }
        
        private void SetActiveButton(Button button)
        {
            // Resetear botón anterior
            if (_activeButton != null)
            {
                _activeButton.Background = System.Windows.Media.Brushes.Transparent;
                _activeButton.Foreground = (System.Windows.Media.Brush)FindResource("TextSecondaryBrush");
            }
            
            // Marcar nuevo botón activo
            _activeButton = button;
            _activeButton.Background = (System.Windows.Media.Brush)FindResource("BackgroundTertiaryBrush");
            _activeButton.Foreground = (System.Windows.Media.Brush)FindResource("TextPrimaryBrush");
        }
    }
}