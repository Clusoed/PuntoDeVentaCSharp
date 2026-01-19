using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Reflection;
using System.Diagnostics;

namespace PuntoDeVenta.Services
{
    public class UpdateInfo
    {
        public string Version { get; set; } = "";
        public string DownloadUrl { get; set; } = "";
        public string ReleaseNotes { get; set; } = "";
        public DateTime ReleaseDate { get; set; }
        public bool IsNewVersionAvailable { get; set; }
    }
    
    public class UpdateService
    {
        private static UpdateService? _instance;
        public static UpdateService Instance => _instance ??= new UpdateService();
        
        // URL del archivo version.json en GitHub (rama main, raw)
        private const string VERSION_CHECK_URL = "https://raw.githubusercontent.com/Clusoed/PuntoDeVentaCSharp/main/version.json";
        
        private readonly HttpClient _httpClient;
        
        public UpdateService()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
        }
        
        /// <summary>
        /// Obtiene la versión actual de la aplicación
        /// </summary>
        public string GetCurrentVersion()
        {
            try
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version;
                return version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "1.0.0";
            }
            catch
            {
                return "1.0.0";
            }
        }
        
        /// <summary>
        /// Verifica si hay una nueva versión disponible
        /// </summary>
        public async Task<UpdateInfo> CheckForUpdatesAsync()
        {
            var result = new UpdateInfo
            {
                Version = GetCurrentVersion(),
                IsNewVersionAvailable = false
            };
            
            try
            {
                var response = await _httpClient.GetStringAsync(VERSION_CHECK_URL);
                var remoteInfo = JsonSerializer.Deserialize<RemoteVersionInfo>(response, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
                if (remoteInfo != null)
                {
                    result.Version = remoteInfo.Version;
                    result.DownloadUrl = remoteInfo.DownloadUrl;
                    result.ReleaseNotes = remoteInfo.ReleaseNotes;
                    result.ReleaseDate = remoteInfo.ReleaseDate;
                    result.IsNewVersionAvailable = IsNewerVersion(remoteInfo.Version, GetCurrentVersion());
                }
            }
            catch
            {
                // Sin conexión o error, no hay actualizaciones disponibles
            }
            
            return result;
        }
        
        /// <summary>
        /// Abre la URL de descarga en el navegador
        /// </summary>
        public void OpenDownloadPage(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch { }
        }
        
        /// <summary>
        /// Compara versiones para determinar si hay una más nueva
        /// </summary>
        private bool IsNewerVersion(string remote, string current)
        {
            try
            {
                var remoteParts = remote.Split('.').Select(int.Parse).ToArray();
                var currentParts = current.Split('.').Select(int.Parse).ToArray();
                
                for (int i = 0; i < Math.Min(remoteParts.Length, currentParts.Length); i++)
                {
                    if (remoteParts[i] > currentParts[i]) return true;
                    if (remoteParts[i] < currentParts[i]) return false;
                }
                
                return remoteParts.Length > currentParts.Length;
            }
            catch
            {
                return false;
            }
        }
        
        private class RemoteVersionInfo
        {
            public string Version { get; set; } = "";
            public string DownloadUrl { get; set; } = "";
            public string ReleaseNotes { get; set; } = "";
            public DateTime ReleaseDate { get; set; }
        }
    }
}
