using System;
using System.IO;
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
        
        // URLs de GitHub
        private const string VERSION_CHECK_URL = "https://raw.githubusercontent.com/Clusoed/PuntoDeVentaCSharp/main/version.json";
        private const string DOWNLOAD_URL = "https://github.com/Clusoed/PuntoDeVentaCSharp/releases/latest/download/PuntoDeVenta.exe";
        
        private readonly HttpClient _httpClient;
        
        // Evento para reportar progreso de descarga
        public event Action<int>? DownloadProgressChanged;
        public event Action<string>? StatusChanged;
        
        public UpdateService()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromMinutes(10); // Timeout largo para descargas
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
                StatusChanged?.Invoke("Verificando actualizaciones...");
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
        /// Descarga e instala la actualización automáticamente
        /// </summary>
        public async Task<bool> DownloadAndInstallUpdateAsync()
        {
            try
            {
                StatusChanged?.Invoke("Preparando descarga...");
                
                // Obtener la ruta del ejecutable actual
                string currentExePath = Process.GetCurrentProcess().MainModule?.FileName ?? "";
                if (string.IsNullOrEmpty(currentExePath))
                    return false;
                
                string directory = Path.GetDirectoryName(currentExePath) ?? "";
                string newExePath = Path.Combine(directory, "PuntoDeVenta_new.exe");
                string oldExePath = Path.Combine(directory, "PuntoDeVenta_old.exe");
                string updaterBatPath = Path.Combine(directory, "updater.bat");
                
                // Descargar el nuevo ejecutable
                StatusChanged?.Invoke("Descargando actualización...");
                
                using (var response = await _httpClient.GetAsync(DOWNLOAD_URL, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();
                    
                    var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                    var downloadedBytes = 0L;
                    
                    using (var contentStream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = new FileStream(newExePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                    {
                        var buffer = new byte[8192];
                        int bytesRead;
                        
                        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, bytesRead);
                            downloadedBytes += bytesRead;
                            
                            if (totalBytes > 0)
                            {
                                var progress = (int)((downloadedBytes * 100) / totalBytes);
                                DownloadProgressChanged?.Invoke(progress);
                                StatusChanged?.Invoke($"Descargando... {progress}%");
                            }
                        }
                    }
                }
                
                StatusChanged?.Invoke("Preparando instalación...");
                
                // Crear script batch para reemplazar el ejecutable
                string batchContent = $@"@echo off
timeout /t 2 /nobreak > nul
if exist ""{oldExePath}"" del /f /q ""{oldExePath}""
move /y ""{currentExePath}"" ""{oldExePath}""
move /y ""{newExePath}"" ""{currentExePath}""
start """" ""{currentExePath}""
del /f /q ""{oldExePath}""
del ""%~f0""
";
                
                await File.WriteAllTextAsync(updaterBatPath, batchContent);
                
                StatusChanged?.Invoke("Reiniciando aplicación...");
                
                // Ejecutar el script de actualización y cerrar la app
                Process.Start(new ProcessStartInfo
                {
                    FileName = updaterBatPath,
                    UseShellExecute = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                });
                
                // Cerrar la aplicación actual
                Environment.Exit(0);
                
                return true;
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke($"Error: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Abre la URL de descarga en el navegador (método alternativo)
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
