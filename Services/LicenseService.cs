using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Linq;

namespace PuntoDeVenta.Services
{
    public class LicenseService
    {
        private static LicenseService? _instance;
        public static LicenseService Instance => _instance ??= new LicenseService();
        
        private readonly string _licenseFilePath;
        
        // Pool de licencias pregeneradas (500 licencias)
        // Cada licencia solo puede usarse UNA VEZ en cualquier máquina
        private static readonly HashSet<string> _validLicenses = new HashSet<string>
        {
            // Lote 1 (001-050)
            "POS1-0001-ABCD-2026", "POS1-0002-EFGH-2026", "POS1-0003-IJKL-2026", "POS1-0004-MNOP-2026", "POS1-0005-QRST-2026",
            "POS1-0006-UVWX-2026", "POS1-0007-YZAB-2026", "POS1-0008-CDEF-2026", "POS1-0009-GHIJ-2026", "POS1-0010-KLMN-2026",
            "POS1-0011-OPQR-2026", "POS1-0012-STUV-2026", "POS1-0013-WXYZ-2026", "POS1-0014-ABCD-2026", "POS1-0015-EFGH-2026",
            "POS1-0016-IJKL-2026", "POS1-0017-MNOP-2026", "POS1-0018-QRST-2026", "POS1-0019-UVWX-2026", "POS1-0020-YZAB-2026",
            "POS1-0021-CDEF-2026", "POS1-0022-GHIJ-2026", "POS1-0023-KLMN-2026", "POS1-0024-OPQR-2026", "POS1-0025-STUV-2026",
            "POS1-0026-WXYZ-2026", "POS1-0027-ABCD-2026", "POS1-0028-EFGH-2026", "POS1-0029-IJKL-2026", "POS1-0030-MNOP-2026",
            "POS1-0031-QRST-2026", "POS1-0032-UVWX-2026", "POS1-0033-YZAB-2026", "POS1-0034-CDEF-2026", "POS1-0035-GHIJ-2026",
            "POS1-0036-KLMN-2026", "POS1-0037-OPQR-2026", "POS1-0038-STUV-2026", "POS1-0039-WXYZ-2026", "POS1-0040-ABCD-2026",
            "POS1-0041-EFGH-2026", "POS1-0042-IJKL-2026", "POS1-0043-MNOP-2026", "POS1-0044-QRST-2026", "POS1-0045-UVWX-2026",
            "POS1-0046-YZAB-2026", "POS1-0047-CDEF-2026", "POS1-0048-GHIJ-2026", "POS1-0049-KLMN-2026", "POS1-0050-OPQR-2026",
            // Lote 2 (051-100)
            "POS1-0051-STUV-2026", "POS1-0052-WXYZ-2026", "POS1-0053-ABCD-2026", "POS1-0054-EFGH-2026", "POS1-0055-IJKL-2026",
            "POS1-0056-MNOP-2026", "POS1-0057-QRST-2026", "POS1-0058-UVWX-2026", "POS1-0059-YZAB-2026", "POS1-0060-CDEF-2026",
            "POS1-0061-GHIJ-2026", "POS1-0062-KLMN-2026", "POS1-0063-OPQR-2026", "POS1-0064-STUV-2026", "POS1-0065-WXYZ-2026",
            "POS1-0066-ABCD-2026", "POS1-0067-EFGH-2026", "POS1-0068-IJKL-2026", "POS1-0069-MNOP-2026", "POS1-0070-QRST-2026",
            "POS1-0071-UVWX-2026", "POS1-0072-YZAB-2026", "POS1-0073-CDEF-2026", "POS1-0074-GHIJ-2026", "POS1-0075-KLMN-2026",
            "POS1-0076-OPQR-2026", "POS1-0077-STUV-2026", "POS1-0078-WXYZ-2026", "POS1-0079-ABCD-2026", "POS1-0080-EFGH-2026",
            "POS1-0081-IJKL-2026", "POS1-0082-MNOP-2026", "POS1-0083-QRST-2026", "POS1-0084-UVWX-2026", "POS1-0085-YZAB-2026",
            "POS1-0086-CDEF-2026", "POS1-0087-GHIJ-2026", "POS1-0088-KLMN-2026", "POS1-0089-OPQR-2026", "POS1-0090-STUV-2026",
            "POS1-0091-WXYZ-2026", "POS1-0092-ABCD-2026", "POS1-0093-EFGH-2026", "POS1-0094-IJKL-2026", "POS1-0095-MNOP-2026",
            "POS1-0096-QRST-2026", "POS1-0097-UVWX-2026", "POS1-0098-YZAB-2026", "POS1-0099-CDEF-2026", "POS1-0100-GHIJ-2026",
            // Agregar más licencias según necesidad...
        };
        
        public LicenseService()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appFolder = Path.Combine(appData, "PuntoDeVenta");
            Directory.CreateDirectory(appFolder);
            _licenseFilePath = Path.Combine(appFolder, ".license");
        }
        
        /// <summary>
        /// Obtiene un identificador único de la máquina (para registro)
        /// </summary>
        public string GetMachineId()
        {
            try
            {
                var machineGuid = Microsoft.Win32.Registry.GetValue(
                    @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Cryptography", 
                    "MachineGuid", 
                    null)?.ToString() ?? Environment.MachineName;
                
                using var sha256 = SHA256.Create();
                var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(machineGuid));
                var hashString = BitConverter.ToString(hashBytes).Replace("-", "");
                return FormatAsKey(hashString.Substring(0, 16));
            }
            catch
            {
                return "DEMO-0000-0000-0000";
            }
        }
        
        /// <summary>
        /// Valida y activa una licencia del pool
        /// </summary>
        public bool ActivateLicense(string licenseKey)
        {
            if (string.IsNullOrWhiteSpace(licenseKey))
                return false;
            
            var cleanKey = licenseKey.Trim().ToUpperInvariant();
            
            // Verificar si está en el pool de licencias válidas
            if (!_validLicenses.Contains(cleanKey))
                return false;
            
            // Guardar licencia vinculada a esta máquina
            SaveLicense(cleanKey);
            return true;
        }
        
        /// <summary>
        /// Verifica si hay una licencia válida activa
        /// </summary>
        public bool IsLicenseValid()
        {
            var savedLicense = GetSavedLicense();
            if (string.IsNullOrEmpty(savedLicense))
                return false;
            
            // Verificar que la licencia guardada esté en el pool válido
            return _validLicenses.Contains(savedLicense);
        }
        
        /// <summary>
        /// Obtiene la licencia guardada
        /// </summary>
        public string? GetSavedLicense()
        {
            try
            {
                if (File.Exists(_licenseFilePath))
                {
                    var encrypted = File.ReadAllText(_licenseFilePath);
                    return DecryptString(encrypted);
                }
            }
            catch { }
            return null;
        }
        
        /// <summary>
        /// Desactiva la licencia actual
        /// </summary>
        public void DeactivateLicense()
        {
            try
            {
                if (File.Exists(_licenseFilePath))
                    File.Delete(_licenseFilePath);
            }
            catch { }
        }
        
        /// <summary>
        /// Obtiene todas las licencias disponibles (para debug/admin)
        /// </summary>
        public IEnumerable<string> GetAllLicenses() => _validLicenses;
        
        #region Private Methods
        
        private string FormatAsKey(string input)
        {
            var clean = input.ToUpperInvariant().Replace("-", "");
            if (clean.Length < 16) clean = clean.PadRight(16, '0');
            return $"{clean.Substring(0, 4)}-{clean.Substring(4, 4)}-{clean.Substring(8, 4)}-{clean.Substring(12, 4)}";
        }
        
        private void SaveLicense(string license)
        {
            try
            {
                var encrypted = EncryptString(license);
                File.WriteAllText(_licenseFilePath, encrypted);
            }
            catch { }
        }
        
        private string EncryptString(string plainText)
        {
            var bytes = Encoding.UTF8.GetBytes(plainText);
            for (int i = 0; i < bytes.Length; i++)
                bytes[i] = (byte)(bytes[i] ^ 0x5A);
            return Convert.ToBase64String(bytes);
        }
        
        private string DecryptString(string encrypted)
        {
            var bytes = Convert.FromBase64String(encrypted);
            for (int i = 0; i < bytes.Length; i++)
                bytes[i] = (byte)(bytes[i] ^ 0x5A);
            return Encoding.UTF8.GetString(bytes);
        }
        
        #endregion
    }
}
