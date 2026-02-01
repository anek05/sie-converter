using OfficeOpenXml;
using OfficeOpenXml.Style;
using LicenseContext = OfficeOpenXml.LicenseContext;
using SieConverterApi.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

namespace SieConverterApi.Services
{
    /// <summary>
    /// Secure Excel export service with custom mapping support
    /// </summary>
    public class ExcelExportService : IExcelExportService
    {
        static ExcelExportService()
        {
            // Set EPPlus license context (NonCommercial for this project)
            // Note: In production, set this via environment variable EPPLUS_LICENSE or configuration
            ExcelPackage.License.SetNonCommercialPersonal("SIE Converter");
        }

        public async Task<byte[]> ExportToExcelAsync(SieFile sieFile, ExcelExportOptions? options = null)
        {
            options = options ?? new ExcelExportOptions();
            
            using var package = new ExcelPackage();
            
            // Create Company Info sheet
            CreateCompanyInfoSheet(package, sieFile, options);
            
            // Create Accounts sheet
            if (options.IncludeAccounts && sieFile.Accounts?.Count > 0)
            {
                CreateAccountsSheet(package, sieFile, options);
            }
            
            // Create Verifications/Transactions sheet
            if (options.IncludeVerifications && sieFile.Verifications?.Count > 0)
            {
                if (options.FlattenTransactions)
                {
                    CreateFlattenedTransactionsSheet(package, sieFile, options);
                }
                else
                {
                    CreateVerificationsSheet(package, sieFile, options);
                }
            }
            
            // Create Opening Balances sheet
            if (options.IncludeOpeningBalances && sieFile.OpeningBalances?.Count > 0)
            {
                CreateOpeningBalancesSheet(package, sieFile, options);
            }
            
            // Create Closing Balances sheet
            if (options.IncludeClosingBalances && sieFile.ClosingBalances?.Count > 0)
            {
                CreateClosingBalancesSheet(package, sieFile, options);
            }
            
            // Create Results sheet
            if (options.IncludeResults && sieFile.Results?.Count > 0)
            {
                CreateResultsSheet(package, sieFile, options);
            }
            
            // Create Dimensions sheet
            if (options.IncludeDimensions && sieFile.Dimensions?.Count > 0)
            {
                CreateDimensionsSheet(package, sieFile, options);
            }
            
            // Create Objects sheet
            if (options.IncludeObjects && sieFile.Objects?.Count > 0)
            {
                CreateObjectsSheet(package, sieFile, options);
            }
            
            return package.GetAsByteArray();
        }
        
        private void CreateCompanyInfoSheet(ExcelPackage package, SieFile sieFile, ExcelExportOptions options)
        {
            var sheet = package.Workbook.Worksheets.Add("Företagsinfo");
            int row = 1;
            
            // Header
            sheet.Cells[row, 1].Value = "Företagsinformation";
            sheet.Cells[row, 1].Style.Font.Bold = true;
            sheet.Cells[row, 1].Style.Font.Size = 14;
            row += 2;
            
            // Company details
            AddInfoRow(sheet, ref row, "Företagsnamn:", sieFile.CompanyName);
            AddInfoRow(sheet, ref row, "Organisationsnummer:", sieFile.CompanyNumber);
            AddInfoRow(sheet, ref row, "Filnamn:", sieFile.FileName);
            AddInfoRow(sheet, ref row, "Valuta:", sieFile.Currency);
            AddInfoRow(sheet, ref row, "Skatteår:", sieFile.TaxYear);
            AddInfoRow(sheet, ref row, "SIE-version:", sieFile.Version);
            AddInfoRow(sheet, ref row, "Format:", sieFile.Format);
            AddInfoRow(sheet, ref row, "Program:", $"{sieFile.ProgramName} {sieFile.ProgramVersion}");
            AddInfoRow(sheet, ref row, "Genererad:", sieFile.GeneratedDate.ToString("yyyy-MM-dd"));
            
            if (sieFile.FinancialYears?.Count > 0)
            {
                row++;
                var year = sieFile.FinancialYears.First();
                AddInfoRow(sheet, ref row, "Räkenskapsår:", $"{year.StartDate:yyyy-MM-dd} - {year.EndDate:yyyy-MM-dd}");
            }
            
            if (!string.IsNullOrEmpty(sieFile.AddressContact))
            {
                row++;
                AddInfoRow(sheet, ref row, "Kontaktperson:", sieFile.AddressContact);
                AddInfoRow(sheet, ref row, "Adress:", sieFile.AddressStreet);
                AddInfoRow(sheet, ref row, "Postadress:", sieFile.AddressPostal);
                AddInfoRow(sheet, ref row, "Telefon:", sieFile.AddressPhone);
            }
            
            if (options.AutoFitColumns)
            {
                sheet.Cells.AutoFitColumns();
            }
        }
        
        private void AddInfoRow(ExcelWorksheet sheet, ref int row, string label, string value)
        {
            sheet.Cells[row, 1].Value = label;
            sheet.Cells[row, 1].Style.Font.Bold = true;
            sheet.Cells[row, 2].Value = value;
            row++;
        }
        
        private void CreateAccountsSheet(ExcelPackage package, SieFile sieFile, ExcelExportOptions options)
        {
            var sheet = package.Workbook.Worksheets.Add("Konton");
            int row = 1;
            int col = 1;
            
            // Headers
            if (options.IncludeHeaders)
            {
                if (options.ShowAccountNumber)
                    sheet.Cells[row, col++].Value = options.AccountNumberColumnName;
                if (options.ShowAccountName)
                    sheet.Cells[row, col++].Value = options.AccountNameColumnName;
                if (options.ShowAccountType)
                    sheet.Cells[row, col++].Value = options.AccountTypeColumnName;
                if (options.ShowSRUCode)
                    sheet.Cells[row, col++].Value = options.SRUCodeColumnName;
                
                FormatHeaderRow(sheet, row, col - 1);
                row++;
            }
            
            // Data
            foreach (var account in sieFile.Accounts.OrderBy(a => a.AccountNumber))
            {
                col = 1;
                if (options.ShowAccountNumber)
                    sheet.Cells[row, col++].Value = account.AccountNumber;
                if (options.ShowAccountName)
                    sheet.Cells[row, col++].Value = account.AccountName;
                if (options.ShowAccountType)
                    sheet.Cells[row, col++].Value = GetAccountTypeDescription(account.AccountType);
                if (options.ShowSRUCode)
                    sheet.Cells[row, col++].Value = account.SRUCode;
                row++;
            }
            
            if (options.AutoFitColumns)
            {
                sheet.Cells.AutoFitColumns();
            }
        }
        
        private void CreateFlattenedTransactionsSheet(ExcelPackage package, SieFile sieFile, ExcelExportOptions options)
        {
            var sheet = package.Workbook.Worksheets.Add("Transaktioner");
            int row = 1;
            int col = 1;
            
            // Headers
            if (options.IncludeHeaders)
            {
                if (options.ShowVerificationSeries)
                    sheet.Cells[row, col++].Value = options.VerificationSeriesColumnName;
                if (options.ShowVerificationNumber)
                    sheet.Cells[row, col++].Value = options.VerificationNumberColumnName;
                if (options.ShowVerificationDate)
                    sheet.Cells[row, col++].Value = options.VerificationDateColumnName;
                if (options.ShowVerificationDescription)
                    sheet.Cells[row, col++].Value = options.VerificationDescriptionColumnName;
                if (options.ShowTransactionAccount)
                    sheet.Cells[row, col++].Value = options.TransactionAccountColumnName;
                if (options.ShowTransactionAmount)
                    sheet.Cells[row, col++].Value = options.TransactionAmountColumnName;
                if (options.ShowTransactionDate)
                    sheet.Cells[row, col++].Value = options.TransactionDateColumnName;
                if (options.ShowTransactionDescription)
                    sheet.Cells[row, col++].Value = options.TransactionDescriptionColumnName;
                if (options.ShowTransactionQuantity)
                    sheet.Cells[row, col++].Value = options.TransactionQuantityColumnName;
                if (options.ShowTransactionDimensions)
                    sheet.Cells[row, col++].Value = options.TransactionDimensionsColumnName;
                
                FormatHeaderRow(sheet, row, col - 1);
                row++;
            }
            
            // Data - flattened (one row per transaction)
            foreach (var verification in sieFile.Verifications.OrderBy(v => v.Date))
            {
                foreach (var transaction in verification.Transactions)
                {
                    col = 1;
                    
                    if (options.ShowVerificationSeries)
                        sheet.Cells[row, col++].Value = verification.Series;
                    if (options.ShowVerificationNumber)
                        sheet.Cells[row, col++].Value = verification.VerificationNumber;
                    if (options.ShowVerificationDate)
                        sheet.Cells[row, col++].Value = verification.Date.ToString("yyyy-MM-dd");
                    if (options.ShowVerificationDescription)
                        sheet.Cells[row, col++].Value = verification.Description;
                    if (options.ShowTransactionAccount)
                        sheet.Cells[row, col++].Value = transaction.AccountNumber;
                    if (options.ShowTransactionAmount)
                    {
                        sheet.Cells[row, col].Value = transaction.Amount;
                        if (options.FormatCurrency)
                            sheet.Cells[row, col].Style.Numberformat.Format = options.CurrencyFormat;
                        col++;
                    }
                    if (options.ShowTransactionDate)
                        sheet.Cells[row, col++].Value = transaction.TransactionDate?.ToString("yyyy-MM-dd");
                    if (options.ShowTransactionDescription)
                        sheet.Cells[row, col++].Value = transaction.Description;
                    if (options.ShowTransactionQuantity)
                    {
                        sheet.Cells[row, col].Value = transaction.Quantity;
                        col++;
                    }
                    if (options.ShowTransactionDimensions)
                    {
                        var dimText = string.Join(", ", transaction.Dimensions.Select(d => $"{d.DimensionNumber}:{d.ObjectNumber}"));
                        sheet.Cells[row, col++].Value = dimText;
                    }
                    
                    row++;
                }
            }
            
            if (options.AutoFitColumns)
            {
                sheet.Cells.AutoFitColumns();
            }
        }
        
        private void CreateVerificationsSheet(ExcelPackage package, SieFile sieFile, ExcelExportOptions options)
        {
            var sheet = package.Workbook.Worksheets.Add("Verifikationer");
            int row = 1;
            
            foreach (var verification in sieFile.Verifications.OrderBy(v => v.Date))
            {
                // Verification header
                sheet.Cells[row, 1].Value = $"{verification.Series} {verification.VerificationNumber}";
                sheet.Cells[row, 1].Style.Font.Bold = true;
                sheet.Cells[row, 2].Value = verification.Date.ToString("yyyy-MM-dd");
                sheet.Cells[row, 3].Value = verification.Description;
                row++;
                
                // Transaction headers
                int col = 1;
                sheet.Cells[row, col++].Value = "Konto";
                sheet.Cells[row, col++].Value = "Belopp";
                sheet.Cells[row, col++].Value = "Beskrivning";
                sheet.Cells[row, col++].Value = "Dimensioner";
                
                using (var range = sheet.Cells[row, 1, row, col - 1])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                }
                row++;
                
                // Transactions
                foreach (var transaction in verification.Transactions)
                {
                    col = 1;
                    sheet.Cells[row, col++].Value = transaction.AccountNumber;
                    
                    sheet.Cells[row, col].Value = transaction.Amount;
                    if (options.FormatCurrency)
                        sheet.Cells[row, col].Style.Numberformat.Format = options.CurrencyFormat;
                    col++;
                    
                    sheet.Cells[row, col++].Value = transaction.Description;
                    
                    var dimText = string.Join(", ", transaction.Dimensions.Select(d => $"{d.DimensionNumber}:{d.ObjectNumber}"));
                    sheet.Cells[row, col++].Value = dimText;
                    
                    row++;
                }
                
                row++; // Empty row between verifications
            }
            
            if (options.AutoFitColumns)
            {
                sheet.Cells.AutoFitColumns();
            }
        }
        
        private void CreateOpeningBalancesSheet(ExcelPackage package, SieFile sieFile, ExcelExportOptions options)
        {
            var sheet = package.Workbook.Worksheets.Add("Ingående saldon");
            int row = 1;
            int col = 1;
            
            // Headers
            if (options.IncludeHeaders)
            {
                if (options.ShowBalanceYear)
                    sheet.Cells[row, col++].Value = options.BalanceYearColumnName;
                if (options.ShowBalanceAccount)
                    sheet.Cells[row, col++].Value = options.BalanceAccountColumnName;
                if (options.ShowBalanceAmount)
                    sheet.Cells[row, col++].Value = options.BalanceAmountColumnName;
                if (options.ShowBalanceQuantity)
                    sheet.Cells[row, col++].Value = options.BalanceQuantityColumnName;
                
                FormatHeaderRow(sheet, row, col - 1);
                row++;
            }
            
            // Data
            foreach (var balance in sieFile.OpeningBalances.OrderBy(b => b.YearIndex).ThenBy(b => b.AccountNumber))
            {
                col = 1;
                if (options.ShowBalanceYear)
                    sheet.Cells[row, col++].Value = GetYearLabel(balance.YearIndex, sieFile);
                if (options.ShowBalanceAccount)
                    sheet.Cells[row, col++].Value = balance.AccountNumber;
                if (options.ShowBalanceAmount)
                {
                    sheet.Cells[row, col].Value = balance.Amount;
                    if (options.FormatCurrency)
                        sheet.Cells[row, col].Style.Numberformat.Format = options.CurrencyFormat;
                    col++;
                }
                if (options.ShowBalanceQuantity)
                {
                    sheet.Cells[row, col].Value = balance.Quantity;
                    col++;
                }
                row++;
            }
            
            if (options.AutoFitColumns)
            {
                sheet.Cells.AutoFitColumns();
            }
        }
        
        private void CreateClosingBalancesSheet(ExcelPackage package, SieFile sieFile, ExcelExportOptions options)
        {
            var sheet = package.Workbook.Worksheets.Add("Utgående saldon");
            int row = 1;
            int col = 1;
            
            // Headers
            if (options.IncludeHeaders)
            {
                if (options.ShowBalanceYear)
                    sheet.Cells[row, col++].Value = options.BalanceYearColumnName;
                if (options.ShowBalanceAccount)
                    sheet.Cells[row, col++].Value = options.BalanceAccountColumnName;
                if (options.ShowBalanceAmount)
                    sheet.Cells[row, col++].Value = options.BalanceAmountColumnName;
                if (options.ShowBalanceQuantity)
                    sheet.Cells[row, col++].Value = options.BalanceQuantityColumnName;
                
                FormatHeaderRow(sheet, row, col - 1);
                row++;
            }
            
            // Data
            foreach (var balance in sieFile.ClosingBalances.OrderBy(b => b.YearIndex).ThenBy(b => b.AccountNumber))
            {
                col = 1;
                if (options.ShowBalanceYear)
                    sheet.Cells[row, col++].Value = GetYearLabel(balance.YearIndex, sieFile);
                if (options.ShowBalanceAccount)
                    sheet.Cells[row, col++].Value = balance.AccountNumber;
                if (options.ShowBalanceAmount)
                {
                    sheet.Cells[row, col].Value = balance.Amount;
                    if (options.FormatCurrency)
                        sheet.Cells[row, col].Style.Numberformat.Format = options.CurrencyFormat;
                    col++;
                }
                if (options.ShowBalanceQuantity)
                {
                    sheet.Cells[row, col].Value = balance.Quantity;
                    col++;
                }
                row++;
            }
            
            if (options.AutoFitColumns)
            {
                sheet.Cells.AutoFitColumns();
            }
        }
        
        private void CreateResultsSheet(ExcelPackage package, SieFile sieFile, ExcelExportOptions options)
        {
            var sheet = package.Workbook.Worksheets.Add("Resultat");
            int row = 1;
            int col = 1;
            
            // Headers
            if (options.IncludeHeaders)
            {
                if (options.ShowBalanceYear)
                    sheet.Cells[row, col++].Value = options.BalanceYearColumnName;
                if (options.ShowBalanceAccount)
                    sheet.Cells[row, col++].Value = options.BalanceAccountColumnName;
                if (options.ShowBalanceAmount)
                    sheet.Cells[row, col++].Value = options.BalanceAmountColumnName;
                if (options.ShowBalanceQuantity)
                    sheet.Cells[row, col++].Value = options.BalanceQuantityColumnName;
                
                FormatHeaderRow(sheet, row, col - 1);
                row++;
            }
            
            // Data
            foreach (var result in sieFile.Results.OrderBy(r => r.YearIndex).ThenBy(r => r.AccountNumber))
            {
                col = 1;
                if (options.ShowBalanceYear)
                    sheet.Cells[row, col++].Value = GetYearLabel(result.YearIndex, sieFile);
                if (options.ShowBalanceAccount)
                    sheet.Cells[row, col++].Value = result.AccountNumber;
                if (options.ShowBalanceAmount)
                {
                    sheet.Cells[row, col].Value = result.Amount;
                    if (options.FormatCurrency)
                        sheet.Cells[row, col].Style.Numberformat.Format = options.CurrencyFormat;
                    col++;
                }
                if (options.ShowBalanceQuantity)
                {
                    sheet.Cells[row, col].Value = result.Quantity;
                    col++;
                }
                row++;
            }
            
            if (options.AutoFitColumns)
            {
                sheet.Cells.AutoFitColumns();
            }
        }
        
        private void CreateDimensionsSheet(ExcelPackage package, SieFile sieFile, ExcelExportOptions options)
        {
            var sheet = package.Workbook.Worksheets.Add("Dimensioner");
            int row = 1;
            int col = 1;
            
            // Headers
            if (options.IncludeHeaders)
            {
                sheet.Cells[row, col++].Value = options.DimensionNumberColumnName;
                sheet.Cells[row, col++].Value = options.DimensionNameColumnName;
                FormatHeaderRow(sheet, row, col - 1);
                row++;
            }
            
            // Data
            foreach (var dimension in sieFile.Dimensions.OrderBy(d => d.DimensionNumber))
            {
                col = 1;
                sheet.Cells[row, col++].Value = dimension.DimensionNumber;
                sheet.Cells[row, col++].Value = dimension.DimensionName;
                row++;
            }
            
            if (options.AutoFitColumns)
            {
                sheet.Cells.AutoFitColumns();
            }
        }
        
        private void CreateObjectsSheet(ExcelPackage package, SieFile sieFile, ExcelExportOptions options)
        {
            var sheet = package.Workbook.Worksheets.Add("Objekt");
            int row = 1;
            int col = 1;
            
            // Headers
            if (options.IncludeHeaders)
            {
                sheet.Cells[row, col++].Value = options.DimensionNumberColumnName;
                sheet.Cells[row, col++].Value = options.ObjectNumberColumnName;
                sheet.Cells[row, col++].Value = options.ObjectNameColumnName;
                FormatHeaderRow(sheet, row, col - 1);
                row++;
            }
            
            // Data
            foreach (var obj in sieFile.Objects.OrderBy(o => o.DimensionNumber).ThenBy(o => o.ObjectNumber))
            {
                col = 1;
                sheet.Cells[row, col++].Value = obj.DimensionNumber;
                sheet.Cells[row, col++].Value = obj.ObjectNumber;
                sheet.Cells[row, col++].Value = obj.ObjectName;
                row++;
            }
            
            if (options.AutoFitColumns)
            {
                sheet.Cells.AutoFitColumns();
            }
        }
        
        private void FormatHeaderRow(ExcelWorksheet sheet, int row, int lastCol)
        {
            using (var range = sheet.Cells[row, 1, row, lastCol])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(68, 114, 196));
                range.Style.Font.Color.SetColor(Color.White);
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            }
        }
        
        private string GetAccountTypeDescription(string type)
        {
            return type?.ToUpper() switch
            {
                "T" => "Tillgång (T)",
                "S" => "Skuld/Eget kapital (S)",
                "I" => "Intäkt (I)",
                "K" => "Kostnad (K)",
                _ => type
            };
        }
        
        private string GetYearLabel(int yearIndex, SieFile sieFile)
        {
            if (yearIndex == 0)
                return "Aktuellt år";
            if (yearIndex == -1)
                return "Föregående år";
            return $"År {yearIndex}";
        }
    }
}
