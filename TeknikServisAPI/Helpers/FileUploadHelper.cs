using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace TeknikServisAPI.Helpers
{
    /// <summary>
    /// Secure file upload helper with comprehensive validation
    /// Prevents: malicious file uploads, path traversal, MIME spoofing
    /// </summary>
    public static class FileUploadHelper
    {
        // Allowed MIME types (whitelist approach)
        private static readonly Dictionary<string, string[]> AllowedMimeTypes = new()
        {
            { ".pdf", new[] { "application/pdf" } },
            { ".jpg", new[] { "image/jpeg" } },
            { ".jpeg", new[] { "image/jpeg" } },
            { ".png", new[] { "image/png" } },
            { ".doc", new[] { "application/msword" } },
            { ".docx", new[] { "application/vnd.openxmlformats-officedocument.wordprocessingml.document" } },
            { ".xls", new[] { "application/vnd.ms-excel" } },
            { ".xlsx", new[] { "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" } }
        };

        // Maximum file size: 10 MB
        private const long MaxFileSize = 10 * 1024 * 1024;

        /// <summary>
        /// Validates uploaded file for security
        /// </summary>
        public static (bool isValid, string error) ValidateFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return (false, "Dosya boş veya geçersiz");
            }

            // Check file size
            if (file.Length > MaxFileSize)
            {
                return (false, $"Dosya boyutu çok büyük. Maksimum: {MaxFileSize / (1024 * 1024)} MB");
            }

            // Get file extension
            var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
            if (string.IsNullOrEmpty(extension) || !AllowedMimeTypes.ContainsKey(extension))
            {
                return (false, $"Dosya tipi desteklenmiyor: {extension}. İzin verilenler: {string.Join(", ", AllowedMimeTypes.Keys)}");
            }

            // Validate MIME type matches extension
            var allowedMimes = AllowedMimeTypes[extension];
            if (!allowedMimes.Contains(file.ContentType))
            {
                return (false, $"Dosya içeriği uyumsuz. Beklenen: {string.Join(", ", allowedMimes)}, Gelen: {file.ContentType}");
            }

            // Check file signature (magic bytes) - basic check
            using var reader = new BinaryReader(file.OpenReadStream());
            var signature = reader.ReadBytes(8);
            
            if (!IsValidFileSignature(signature, extension))
            {
                return (false, "Dosya içeriği geçersiz (magic bytes check failed)");
            }

            return (true, string.Empty);
        }

        /// <summary>
        /// Generates secure random filename
        /// </summary>
        public static string GenerateSecureFileName(string originalFileName)
        {
            var extension = Path.GetExtension(originalFileName);
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var randomPart = Guid.NewGuid().ToString("N").Substring(0, 8);
            
            return $"{timestamp}_{randomPart}{extension}";
        }

        /// <summary>
        /// Validates file signature (magic bytes)
        /// </summary>
        private static bool IsValidFileSignature(byte[] signature, string extension)
        {
            if (signature == null || signature.Length < 4)
                return false;

            return extension switch
            {
                ".pdf" => signature[0] == 0x25 && signature[1] == 0x50 && signature[2] == 0x44 && signature[3] == 0x46, // %PDF
                ".jpg" or ".jpeg" => signature[0] == 0xFF && signature[1] == 0xD8 && signature[2] == 0xFF,
                ".png" => signature[0] == 0x89 && signature[1] == 0x50 && signature[2] == 0x4E && signature[3] == 0x47,
                ".doc" => signature[0] == 0xD0 && signature[1] == 0xCF && signature[2] == 0x11 && signature[3] == 0xE0,
                ".docx" or ".xlsx" => signature[0] == 0x50 && signature[1] == 0x4B && signature[2] == 0x03 && signature[3] == 0x04, // ZIP
                _ => true // For other types, skip signature check
            };
        }

        /// <summary>
        /// Gets safe storage path (outside wwwroot for security)
        /// </summary>
        public static string GetSecureStoragePath(string basePath, string subfolder)
        {
            // Store outside wwwroot
            var uploadPath = Path.Combine(basePath, "SecureUploads", subfolder);
            
            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }

            return uploadPath;
        }
    }
}
