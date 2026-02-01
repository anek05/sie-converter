using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace SieConverterApi.Services
{
    /// <summary>
    /// Secure temporary file service for handling sensitive financial data
    /// - Files are stored in system temp directory
    /// - Files are tracked and automatically cleaned up
    /// - Secure deletion option overwrites file content before deletion
    /// </summary>
    public class TempFileService : ITempFileService
    {
        private readonly HashSet<string> _tempFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly string _tempDirectory;
        private bool _disposed = false;
        private readonly object _lock = new object();

        public TempFileService()
        {
            // Use system temp directory
            _tempDirectory = Path.GetTempPath();
        }

        public string CreateTempFile(string prefix = "", string suffix = "")
        {
            // Generate cryptographically secure random filename
            var randomBytes = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }
            var randomName = BitConverter.ToString(randomBytes).Replace("-", "").ToLowerInvariant();
            
            // Build filename
            var fileName = $"sie_{randomName}";
            if (!string.IsNullOrEmpty(prefix))
                fileName = $"{prefix}_{fileName}";
            if (!string.IsNullOrEmpty(suffix))
                fileName = $"{fileName}{suffix}";
            
            var tempPath = Path.Combine(_tempDirectory, fileName);
            
            lock (_lock)
            {
                _tempFiles.Add(tempPath);
            }
            
            // Create empty file to reserve the name
            using (File.Create(tempPath)) { }
            
            return tempPath;
        }

        public async Task<string> SaveToTempFileAsync(Stream stream, string prefix = "", string suffix = "")
        {
            var tempPath = CreateTempFile(prefix, suffix);
            
            try
            {
                using (var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true))
                {
                    await stream.CopyToAsync(fileStream);
                }
                
                return tempPath;
            }
            catch
            {
                // Clean up on failure
                DeleteTempFile(tempPath);
                throw;
            }
        }

        public void DeleteTempFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return;
                
            lock (_lock)
            {
                if (!_tempFiles.Contains(filePath))
                    return; // Only delete files we created
            }
            
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch
            {
                // Ignore deletion errors - file may be locked or already deleted
            }
            finally
            {
                lock (_lock)
                {
                    _tempFiles.Remove(filePath);
                }
            }
        }

        public void SecureDelete(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return;
                
            lock (_lock)
            {
                if (!_tempFiles.Contains(filePath))
                    return; // Only delete files we created
            }
            
            try
            {
                if (File.Exists(filePath))
                {
                    // Overwrite file content before deletion for security
                    var fileInfo = new FileInfo(filePath);
                    if (fileInfo.Length > 0)
                    {
                        try
                        {
                            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Write))
                            {
                                var buffer = new byte[8192];
                                var remaining = fileInfo.Length;
                                
                                // Overwrite with zeros
                                while (remaining > 0)
                                {
                                    var toWrite = (int)Math.Min(buffer.Length, remaining);
                                    fs.Write(buffer, 0, toWrite);
                                    remaining -= toWrite;
                                }
                                fs.Flush();
                            }
                        }
                        catch
                        {
                            // If overwrite fails, still attempt delete
                        }
                    }
                    
                    File.Delete(filePath);
                }
            }
            catch
            {
                // Ignore deletion errors
            }
            finally
            {
                lock (_lock)
                {
                    _tempFiles.Remove(filePath);
                }
            }
        }

        public bool TempFileExists(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return false;
                
            lock (_lock)
            {
                return _tempFiles.Contains(filePath) && File.Exists(filePath);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Clean up all tracked temp files
                    lock (_lock)
                    {
                        foreach (var tempFile in _tempFiles)
                        {
                            try
                            {
                                if (File.Exists(tempFile))
                                    File.Delete(tempFile);
                            }
                            catch
                            {
                                // Ignore errors during cleanup
                            }
                        }
                        _tempFiles.Clear();
                    }
                }
                
                _disposed = true;
            }
        }
        
        ~TempFileService()
        {
            Dispose(false);
        }
    }
}
