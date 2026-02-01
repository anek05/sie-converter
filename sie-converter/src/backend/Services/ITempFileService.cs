using System;
using System.IO;
using System.Threading.Tasks;

namespace SieConverterApi.Services
{
    /// <summary>
    /// Service for secure temporary file handling
    /// </summary>
    public interface ITempFileService : IDisposable
    {
        /// <summary>
        /// Creates a secure temporary file and returns the path
        /// </summary>
        string CreateTempFile(string prefix = "", string suffix = "");
        
        /// <summary>
        /// Saves stream content to a secure temporary file
        /// </summary>
        Task<string> SaveToTempFileAsync(Stream stream, string prefix = "", string suffix = "");
        
        /// <summary>
        /// Immediately deletes a temporary file
        /// </summary>
        void DeleteTempFile(string filePath);
        
        /// <summary>
        /// Checks if a temp file exists
        /// </summary>
        bool TempFileExists(string filePath);
        
        /// <summary>
        /// Securely wipes and deletes a file (overwrites before delete)
        /// </summary>
        void SecureDelete(string filePath);
    }
}
