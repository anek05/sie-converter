using SieConverterApi.Models;
using System.IO;
using System.Threading.Tasks;

namespace SieConverterApi.Services
{
    /// <summary>
    /// Service for parsing SIE files
    /// </summary>
    public interface ISieParserService
    {
        /// <summary>
        /// Parse a SIE file from a file path
        /// </summary>
        Task<SieFile> ParseAsync(string filePath);
        
        /// <summary>
        /// Parse a SIE file from a stream
        /// </summary>
        Task<SieFile> ParseFromStreamAsync(Stream stream);
        
        /// <summary>
        /// Parse a SIE file from string content
        /// </summary>
        Task<SieFile> ParseFromStringAsync(string content);
        
        /// <summary>
        /// Validates if the content appears to be a valid SIE file
        /// </summary>
        bool IsValidSieContent(string content);
    }
}
