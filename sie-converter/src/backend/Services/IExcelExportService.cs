using SieConverterApi.Models;
using System.Threading.Tasks;

namespace SieConverterApi.Services
{
    /// <summary>
    /// Service for exporting SIE data to Excel
    /// </summary>
    public interface IExcelExportService
    {
        /// <summary>
        /// Export SIE file data to Excel format
        /// </summary>
        Task<byte[]> ExportToExcelAsync(SieFile sieFile, ExcelExportOptions? options = null);
    }
    
    /// <summary>
    /// Options for customizing Excel export
    /// </summary>
    public class ExcelExportOptions
    {
        // Sheet selection
        public bool IncludeAccounts { get; set; } = true;
        public bool IncludeVerifications { get; set; } = true;
        public bool IncludeOpeningBalances { get; set; } = true;
        public bool IncludeClosingBalances { get; set; } = true;
        public bool IncludeResults { get; set; } = true;
        public bool IncludeDimensions { get; set; } = true;
        public bool IncludeObjects { get; set; } = true;
        
        // Column visibility for Accounts sheet
        public bool ShowAccountNumber { get; set; } = true;
        public bool ShowAccountName { get; set; } = true;
        public bool ShowAccountType { get; set; } = true;
        public bool ShowSRUCode { get; set; } = true;
        
        // Column visibility for Transactions sheet
        public bool ShowVerificationSeries { get; set; } = true;
        public bool ShowVerificationNumber { get; set; } = true;
        public bool ShowVerificationDate { get; set; } = true;
        public bool ShowVerificationDescription { get; set; } = true;
        public bool ShowTransactionAccount { get; set; } = true;
        public bool ShowTransactionAmount { get; set; } = true;
        public bool ShowTransactionDate { get; set; } = true;
        public bool ShowTransactionDescription { get; set; } = true;
        public bool ShowTransactionQuantity { get; set; } = true;
        public bool ShowTransactionDimensions { get; set; } = true;
        
        // Column visibility for Balances sheets
        public bool ShowBalanceYear { get; set; } = true;
        public bool ShowBalanceAccount { get; set; } = true;
        public bool ShowBalanceAmount { get; set; } = true;
        public bool ShowBalanceQuantity { get; set; } = true;
        
        // Custom column names (for company-specific mapping)
        public string AccountNumberColumnName { get; set; } = "Kontonummer";
        public string AccountNameColumnName { get; set; } = "Kontonamn";
        public string AccountTypeColumnName { get; set; } = "Kontotyp";
        public string SRUCodeColumnName { get; set; } = "SRU-kod";
        
        public string VerificationSeriesColumnName { get; set; } = "Serie";
        public string VerificationNumberColumnName { get; set; } = "Verifikationsnummer";
        public string VerificationDateColumnName { get; set; } = "Datum";
        public string VerificationDescriptionColumnName { get; set; } = "Beskrivning";
        
        public string TransactionAccountColumnName { get; set; } = "Konto";
        public string TransactionAmountColumnName { get; set; } = "Belopp";
        public string TransactionDateColumnName { get; set; } = "Transaktionsdatum";
        public string TransactionDescriptionColumnName { get; set; } = "Transaktionsbeskrivning";
        public string TransactionQuantityColumnName { get; set; } = "Kvantitet";
        public string TransactionDimensionsColumnName { get; set; } = "Dimensioner";
        
        public string BalanceYearColumnName { get; set; } = "År";
        public string BalanceAccountColumnName { get; set; } = "Konto";
        public string BalanceAmountColumnName { get; set; } = "Saldo";
        public string BalanceQuantityColumnName { get; set; } = "Kvantitet";
        
        public string DimensionNumberColumnName { get; set; } = "Dimensionsnummer";
        public string DimensionNameColumnName { get; set; } = "Dimensionsnamn";
        public string ObjectNumberColumnName { get; set; } = "Objektnummer";
        public string ObjectNameColumnName { get; set; } = "Objektnamn";
        
        // Formatting options
        public bool IncludeHeaders { get; set; } = true;
        public bool AutoFitColumns { get; set; } = true;
        public bool FormatCurrency { get; set; } = true;
        public string CurrencyFormat { get; set; } = "#,##0.00";
        
        // Flatten transactions (one row per transaction instead of grouped by verification)
        public bool FlattenTransactions { get; set; } = true;
    }
}
