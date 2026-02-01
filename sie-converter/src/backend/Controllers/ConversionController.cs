using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SieConverterApi.Models;
using SieConverterApi.Services;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SieConverterApi.Controllers
{
    /// <summary>
    /// Secure SIE to Excel conversion controller
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ConversionController : ControllerBase
    {
        private readonly ISieParserService _sieParserService;
        private readonly IExcelExportService _excelExportService;
        private readonly ITempFileService _tempFileService;
        private readonly ILogger<ConversionController> _logger;
        
        // Security limits
        private const long MaxFileSize = 50 * 1024 * 1024; // 50MB
        private readonly string[] AllowedExtensions = { ".sie", ".se", ".si", ".txt" };

        public ConversionController(
            ISieParserService sieParserService,
            IExcelExportService excelExportService,
            ITempFileService tempFileService,
            ILogger<ConversionController> logger)
        {
            _sieParserService = sieParserService;
            _excelExportService = excelExportService;
            _tempFileService = tempFileService;
            _logger = logger;
        }

        /// <summary>
        /// Converts a SIE file to Excel format
        /// </summary>
        /// <param name="file">The SIE file to convert</param>
        /// <returns>Excel file download</returns>
        [HttpPost("convert")]
        [RequestSizeLimit(MaxFileSize)]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ConvertSieToExcel(IFormFile file)
        {
            string tempPath = null;
            
            try
            {
                // Validate input
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { error = "Ingen fil har laddats upp" });
                }
                
                if (file.Length > MaxFileSize)
                {
                    return BadRequest(new { error = $"Filstorleken överskrider maxgränsen på {MaxFileSize / 1024 / 1024} MB" });
                }
                
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!AllowedExtensions.Contains(extension))
                {
                    return BadRequest(new { error = $"Ogiltigt filformat. Tillåtna format: {string.Join(", ", AllowedExtensions)}" });
                }
                
                // Read and validate SIE content
                string content;
                using (var stream = file.OpenReadStream())
                {
                    using (var reader = new StreamReader(stream, System.Text.Encoding.GetEncoding(437), true))
                    {
                        content = await reader.ReadToEndAsync();
                    }
                }
                
                if (!_sieParserService.IsValidSieContent(content))
                {
                    return BadRequest(new { error = "Filen verkar inte vara en giltig SIE-fil" });
                }
                
                // Parse SIE file (in memory, no temp file needed for parsing from string)
                var sieFile = await _sieParserService.ParseFromStringAsync(content);
                
                // Build export options from request
                var options = BuildExportOptions(Request.Form);
                
                // Generate Excel
                var excelData = await _excelExportService.ExportToExcelAsync(sieFile, options);
                
                // Generate secure filename
                var outputFileName = GenerateSecureFileName(file.FileName);
                
                // Return file with security headers (middleware already sets most, just add Content-Disposition)
                Response.Headers["Content-Disposition"] = $"attachment; filename=\"{outputFileName}\"";
                
                return File(excelData, 
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    outputFileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting SIE file");
                
                // Don't expose internal error details to client for security
                return StatusCode(500, new { error = "Ett fel uppstod vid konvertering av filen" });
            }
            finally
            {
                // Ensure temp file cleanup
                if (!string.IsNullOrEmpty(tempPath))
                {
                    _tempFileService.SecureDelete(tempPath);
                }
            }
        }
        
        /// <summary>
        /// Validates a SIE file without converting it
        /// </summary>
        [HttpPost("validate")]
        [RequestSizeLimit(MaxFileSize)]
        public async Task<IActionResult> ValidateSieFile(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { valid = false, error = "Ingen fil har laddats upp" });
                }
                
                if (file.Length > MaxFileSize)
                {
                    return BadRequest(new { valid = false, error = "Filstorleken överskrider maxgränsen" });
                }
                
                using (var stream = file.OpenReadStream())
                {
                    using (var reader = new StreamReader(stream, System.Text.Encoding.GetEncoding(437), true))
                    {
                        var content = await reader.ReadToEndAsync();
                        var isValid = _sieParserService.IsValidSieContent(content);
                        
                        if (isValid)
                        {
                            var sieFile = await _sieParserService.ParseFromStringAsync(content);
                            return Ok(new 
                            { 
                                valid = true, 
                                company = sieFile.CompanyName,
                                accounts = sieFile.Accounts?.Count ?? 0,
                                verifications = sieFile.Verifications?.Count ?? 0,
                                version = sieFile.Version
                            });
                        }
                        else
                        {
                            return Ok(new { valid = false, error = "Filen är inte en giltig SIE-fil" });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating SIE file");
                return Ok(new { valid = false, error = "Ett fel uppstod vid validering" });
            }
        }
        
        /// <summary>
        /// Gets default export options
        /// </summary>
        [HttpGet("options")]
        public IActionResult GetDefaultOptions()
        {
            var options = new ExcelExportOptions();
            return Ok(new
            {
                sheets = new
                {
                    includeAccounts = options.IncludeAccounts,
                    includeVerifications = options.IncludeVerifications,
                    includeOpeningBalances = options.IncludeOpeningBalances,
                    includeClosingBalances = options.IncludeClosingBalances,
                    includeResults = options.IncludeResults,
                    includeDimensions = options.IncludeDimensions,
                    includeObjects = options.IncludeObjects
                },
                columns = new
                {
                    accounts = new
                    {
                        accountNumber = options.AccountNumberColumnName,
                        accountName = options.AccountNameColumnName,
                        accountType = options.AccountTypeColumnName,
                        sruCode = options.SRUCodeColumnName
                    },
                    transactions = new
                    {
                        verificationSeries = options.VerificationSeriesColumnName,
                        verificationNumber = options.VerificationNumberColumnName,
                        verificationDate = options.VerificationDateColumnName,
                        verificationDescription = options.VerificationDescriptionColumnName,
                        transactionAccount = options.TransactionAccountColumnName,
                        transactionAmount = options.TransactionAmountColumnName,
                        transactionDate = options.TransactionDateColumnName,
                        transactionDescription = options.TransactionDescriptionColumnName,
                        transactionQuantity = options.TransactionQuantityColumnName,
                        transactionDimensions = options.TransactionDimensionsColumnName
                    }
                },
                formatting = new
                {
                    includeHeaders = options.IncludeHeaders,
                    autoFitColumns = options.AutoFitColumns,
                    formatCurrency = options.FormatCurrency,
                    currencyFormat = options.CurrencyFormat,
                    flattenTransactions = options.FlattenTransactions
                }
            });
        }
        
        private ExcelExportOptions BuildExportOptions(IFormCollection form)
        {
            var options = new ExcelExportOptions();
            
            // Sheet selection
            if (form.ContainsKey("includeAccounts") && bool.TryParse(form["includeAccounts"], out var includeAccounts))
                options.IncludeAccounts = includeAccounts;
            if (form.ContainsKey("includeVerifications") && bool.TryParse(form["includeVerifications"], out var includeVerifications))
                options.IncludeVerifications = includeVerifications;
            if (form.ContainsKey("includeOpeningBalances") && bool.TryParse(form["includeOpeningBalances"], out var includeOpeningBalances))
                options.IncludeOpeningBalances = includeOpeningBalances;
            if (form.ContainsKey("includeClosingBalances") && bool.TryParse(form["includeClosingBalances"], out var includeClosingBalances))
                options.IncludeClosingBalances = includeClosingBalances;
            if (form.ContainsKey("includeResults") && bool.TryParse(form["includeResults"], out var includeResults))
                options.IncludeResults = includeResults;
            if (form.ContainsKey("includeDimensions") && bool.TryParse(form["includeDimensions"], out var includeDimensions))
                options.IncludeDimensions = includeDimensions;
            if (form.ContainsKey("includeObjects") && bool.TryParse(form["includeObjects"], out var includeObjects))
                options.IncludeObjects = includeObjects;
            
            // Column custom names
            if (form.ContainsKey("accountNumberColumnName"))
                options.AccountNumberColumnName = form["accountNumberColumnName"];
            if (form.ContainsKey("accountNameColumnName"))
                options.AccountNameColumnName = form["accountNameColumnName"];
            if (form.ContainsKey("accountTypeColumnName"))
                options.AccountTypeColumnName = form["accountTypeColumnName"];
            if (form.ContainsKey("sruCodeColumnName"))
                options.SRUCodeColumnName = form["sruCodeColumnName"];
            
            if (form.ContainsKey("verificationSeriesColumnName"))
                options.VerificationSeriesColumnName = form["verificationSeriesColumnName"];
            if (form.ContainsKey("verificationNumberColumnName"))
                options.VerificationNumberColumnName = form["verificationNumberColumnName"];
            if (form.ContainsKey("verificationDateColumnName"))
                options.VerificationDateColumnName = form["verificationDateColumnName"];
            if (form.ContainsKey("verificationDescriptionColumnName"))
                options.VerificationDescriptionColumnName = form["verificationDescriptionColumnName"];
            
            if (form.ContainsKey("transactionAccountColumnName"))
                options.TransactionAccountColumnName = form["transactionAccountColumnName"];
            if (form.ContainsKey("transactionAmountColumnName"))
                options.TransactionAmountColumnName = form["transactionAmountColumnName"];
            if (form.ContainsKey("transactionDateColumnName"))
                options.TransactionDateColumnName = form["transactionDateColumnName"];
            if (form.ContainsKey("transactionDescriptionColumnName"))
                options.TransactionDescriptionColumnName = form["transactionDescriptionColumnName"];
            if (form.ContainsKey("transactionQuantityColumnName"))
                options.TransactionQuantityColumnName = form["transactionQuantityColumnName"];
            if (form.ContainsKey("transactionDimensionsColumnName"))
                options.TransactionDimensionsColumnName = form["transactionDimensionsColumnName"];
            
            // Formatting options
            if (form.ContainsKey("includeHeaders") && bool.TryParse(form["includeHeaders"], out var includeHeaders))
                options.IncludeHeaders = includeHeaders;
            if (form.ContainsKey("autoFitColumns") && bool.TryParse(form["autoFitColumns"], out var autoFitColumns))
                options.AutoFitColumns = autoFitColumns;
            if (form.ContainsKey("formatCurrency") && bool.TryParse(form["formatCurrency"], out var formatCurrency))
                options.FormatCurrency = formatCurrency;
            if (form.ContainsKey("flattenTransactions") && bool.TryParse(form["flattenTransactions"], out var flattenTransactions))
                options.FlattenTransactions = flattenTransactions;
            if (form.ContainsKey("currencyFormat"))
                options.CurrencyFormat = form["currencyFormat"];
            
            return options;
        }
        
        private string GenerateSecureFileName(string originalFileName)
        {
            // Remove extension and any path components
            var baseName = Path.GetFileNameWithoutExtension(originalFileName);
            
            // Sanitize filename - only allow alphanumeric and some safe characters
            var sanitized = string.Concat(baseName
                .Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == ' ')
                .Take(50)); // Limit length
            
            // Add timestamp for uniqueness
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            
            return $"{sanitized}_{timestamp}.xlsx";
        }
    }
}
