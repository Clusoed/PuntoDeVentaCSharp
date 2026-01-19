using System.Windows;
using PuntoDeVenta.Services;

namespace PuntoDeVenta.Views
{
    public partial class UpdateDialog : Window
    {
        private readonly UpdateInfo _updateInfo;
        
        public UpdateDialog(UpdateInfo updateInfo)
        {
            InitializeComponent();
            _updateInfo = updateInfo;
            
            // Mostrar información de versiones
            txtCurrentVersion.Text = UpdateService.Instance.GetCurrentVersion();
            txtNewVersion.Text = updateInfo.Version;
            txtReleaseNotes.Text = updateInfo.ReleaseNotes;
            
            // Suscribirse a eventos de progreso
            UpdateService.Instance.DownloadProgressChanged += OnProgressChanged;
            UpdateService.Instance.StatusChanged += OnStatusChanged;
        }
        
        private void OnProgressChanged(int progress)
        {
            Dispatcher.Invoke(() =>
            {
                progressBar.Value = progress;
            });
        }
        
        private void OnStatusChanged(string status)
        {
            Dispatcher.Invoke(() =>
            {
                txtStatus.Text = status;
            });
        }
        
        private async void Update_Click(object sender, RoutedEventArgs e)
        {
            // Mostrar barra de progreso y ocultar botones
            progressPanel.Visibility = Visibility.Visible;
            buttonsPanel.Visibility = Visibility.Collapsed;
            
            // Iniciar descarga e instalación
            await UpdateService.Instance.DownloadAndInstallUpdateAsync();
        }
        
        private void Later_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
        
        protected override void OnClosed(System.EventArgs e)
        {
            // Desuscribirse de eventos
            UpdateService.Instance.DownloadProgressChanged -= OnProgressChanged;
            UpdateService.Instance.StatusChanged -= OnStatusChanged;
            base.OnClosed(e);
        }
    }
}
