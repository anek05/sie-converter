using SieConverterApi.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SieConverterApi.Services
{
    /// <summary>
    /// Secure SIE file parser that properly handles the SIE4 format specification
    /// </summary>
    public class SieParserService : ISieParserService
    {
        // Maximum file size: 50MB for security
        private const int MaxFileSize = 50 * 1024 * 1024;
        
        public async Task<SieFile> ParseAsync(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("SIE file not found", filePath);
                
            var fileInfo = new FileInfo(filePath);
            if (fileInfo.Length > MaxFileSize)
                throw new InvalidOperationException($"File size exceeds maximum allowed size of {MaxFileSize / 1024 / 1024}MB");
            
            // Read with proper encoding detection (SIE files often use CP437/PC8)
            string content;
            using (var reader = new StreamReader(filePath, Encoding.GetEncoding(437), detectEncodingFromByteOrderMarks: true))
            {
                content = await reader.ReadToEndAsync();
            }
            
            return await ParseFromStringAsync(content);
        }

        public async Task<SieFile> ParseFromStreamAsync(Stream stream)
        {
            if (stream.Length > MaxFileSize)
                throw new InvalidOperationException($"File size exceeds maximum allowed size of {MaxFileSize / 1024 / 1024}MB");
            
            using (var reader = new StreamReader(stream, Encoding.GetEncoding(437), detectEncodingFromByteOrderMarks: true))
            {
                var content = await reader.ReadToEndAsync();
                return await ParseFromStringAsync(content);
            }
        }

        public Task<SieFile> ParseFromStringAsync(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("SIE content cannot be empty", nameof(content));
            
            var sieFile = new SieFile();
            var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                              .Select(l => l.Trim())
                              .Where(l => !string.IsNullOrWhiteSpace(l))
                              .ToList();
            
            SieVerification currentVerification = null;
            bool inVerificationBlock = false;
            
            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                
                // Skip comments
                if (line.StartsWith(";"))
                    continue;
                
                // Handle verification blocks
                if (line.StartsWith("#VER"))
                {
                    currentVerification = ParseVerification(line);
                    inVerificationBlock = true;
                    sieFile.Verifications.Add(currentVerification);
                    continue;
                }
                
                if (line.StartsWith("{"))
                {
                    inVerificationBlock = true;
                    continue;
                }
                
                if (line.StartsWith("}"))
                {
                    inVerificationBlock = false;
                    currentVerification = null;
                    continue;
                }
                
                // Parse transactions within verification blocks
                if (inVerificationBlock && line.StartsWith("#TRANS") && currentVerification != null)
                {
                    var transaction = ParseTransaction(line);
                    currentVerification.Transactions.Add(transaction);
                    continue;
                }
                
                // Parse header and metadata lines
                ParseHeaderLine(line, sieFile);
            }
            
            return Task.FromResult(sieFile);
        }
        
        public bool IsValidSieContent(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return false;
                
            // Check for required SIE markers
            return content.Contains("#FLAGGA") || 
                   content.Contains("#SIETYP") || 
                   content.Contains("#KONTO") ||
                   content.Contains("#VER");
        }
        
        private void ParseHeaderLine(string line, SieFile sieFile)
        {
            if (!line.StartsWith("#"))
                return;
                
            var parts = SplitSieLine(line);
            if (parts.Count == 0)
                return;
                
            var keyword = parts[0];
            
            switch (keyword)
            {
                case "#FLAGGA":
                    // Flag line - usually "#FLAGGA 1" or "#FLAGGA 0"
                    break;
                    
                case "#FORMAT":
                    if (parts.Count > 1)
                        sieFile.Format = parts[1];
                    break;
                    
                case "#SIETYP":
                    if (parts.Count > 1)
                        sieFile.Version = parts[1];
                    break;
                    
                case "#PROGRAM":
                    if (parts.Count > 1)
                        sieFile.ProgramName = Unquote(parts[1]);
                    if (parts.Count > 2)
                        sieFile.ProgramVersion = Unquote(parts[2]);
                    break;
                    
                case "#GEN":
                    if (parts.Count > 1 && DateTime.TryParseExact(parts[1], "yyyyMMdd", 
                        System.Globalization.CultureInfo.InvariantCulture, 
                        System.Globalization.DateTimeStyles.None, out var genDate))
                        sieFile.GeneratedDate = genDate;
                    break;
                    
                case "#FNAMN":
                    if (parts.Count > 1)
                        sieFile.CompanyName = Unquote(parts[1]);
                    break;
                    
                case "#FNR":
                    if (parts.Count > 1)
                        sieFile.FileName = Unquote(parts[1]);
                    break;
                    
                case "#ORGNR":
                    if (parts.Count > 1)
                        sieFile.CompanyNumber = parts[1];
                    break;
                    
                case "#ADRESS":
                    if (parts.Count > 1)
                        sieFile.AddressContact = Unquote(parts[1]);
                    if (parts.Count > 2)
                        sieFile.AddressStreet = Unquote(parts[2]);
                    if (parts.Count > 3)
                        sieFile.AddressPostal = Unquote(parts[3]);
                    if (parts.Count > 4)
                        sieFile.AddressPhone = Unquote(parts[4]);
                    break;
                    
                case "#RAR":
                    ParseFinancialYear(parts, sieFile);
                    break;
                    
                case "#TAXAR":
                    if (parts.Count > 1)
                        sieFile.TaxYear = parts[1];
                    break;
                    
                case "#VALUTA":
                    if (parts.Count > 1)
                        sieFile.Currency = parts[1];
                    break;
                    
                case "#KPTYP":
                    if (parts.Count > 1)
                        sieFile.ChartOfAccountsType = parts[1];
                    break;
                    
                case "#KONTO":
                    ParseAccount(parts, sieFile);
                    break;
                    
                case "#KTYP":
                    ParseAccountType(parts, sieFile);
                    break;
                    
                case "#SRU":
                    ParseSRUCode(parts, sieFile);
                    break;
                    
                case "#DIM":
                    ParseDimension(parts, sieFile);
                    break;
                    
                case "#OBJEKT":
                    ParseObject(parts, sieFile);
                    break;
                    
                case "#IB":
                    ParseOpeningBalance(parts, sieFile);
                    break;
                    
                case "#UB":
                    ParseClosingBalance(parts, sieFile);
                    break;
                    
                case "#RES":
                    ParseResult(parts, sieFile);
                    break;
            }
        }
        
        private SieVerification ParseVerification(string line)
        {
            // Format: #VER series number date description [registrationDate] [registrationBy]
            // Example: #VER A 1 20210105 "Kaffebröd" 20210310
            var parts = SplitSieLine(line);
            
            var verification = new SieVerification();
            
            if (parts.Count > 1)
                verification.Series = parts[1];
            if (parts.Count > 2)
                verification.VerificationNumber = parts[2];
            if (parts.Count > 3 && DateTime.TryParseExact(parts[3], "yyyyMMdd",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var date))
                verification.Date = date;
            if (parts.Count > 4)
                verification.Description = Unquote(parts[4]);
            if (parts.Count > 5 && DateTime.TryParseExact(parts[5], "yyyyMMdd",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var regDate))
                verification.RegistrationDate = regDate;
            if (parts.Count > 6)
                verification.RegistrationBy = Unquote(parts[6]);
                
            return verification;
        }
        
        private SieTransaction ParseTransaction(string line)
        {
            // Format: #TRANS account {dimensions} amount [date] [description] [quantity]
            // Example: #TRANS 1910 {} -195.00
            // Example: #TRANS 3041 {1 Nord} -3550.00
            // Example: #TRANS 7010 {1 Nord} 30962.80 20210123 "" 216
            
            var transaction = new SieTransaction();
            
            // Extract account number (first token after #TRANS)
            var accountMatch = Regex.Match(line, @"#TRANS\s+(\d+)");
            if (accountMatch.Success)
                transaction.AccountNumber = accountMatch.Groups[1].Value;
            
            // Extract dimensions {dim obj ...}
            var dimMatch = Regex.Match(line, @"\{([^}]*)\}");
            if (dimMatch.Success)
            {
                var dimContent = dimMatch.Groups[1].Value.Trim();
                if (!string.IsNullOrEmpty(dimContent))
                {
                    var dimParts = dimContent.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 0; i < dimParts.Length - 1; i += 2)
                    {
                        transaction.Dimensions.Add(new SieDimensionReference
                        {
                            DimensionNumber = dimParts[i],
                            ObjectNumber = dimParts[i + 1]
                        });
                    }
                }
            }
            
            // Extract amount, date, description, quantity
            // Remove #TRANS, account, and dimensions for easier parsing
            var remaining = Regex.Replace(line, @"#TRANS\s+\d+\s*\{[^}]*\}", "").Trim();
            
            var tokens = SplitSieLine(remaining);
            if (tokens.Count > 0 && decimal.TryParse(tokens[0], 
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var amount))
                transaction.Amount = amount;
            
            if (tokens.Count > 1 && DateTime.TryParseExact(tokens[1], "yyyyMMdd",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var transDate))
                transaction.TransactionDate = transDate;
            
            if (tokens.Count > 2)
                transaction.Description = Unquote(tokens[2]);
            
            if (tokens.Count > 3 && decimal.TryParse(tokens[3],
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var qty))
                transaction.Quantity = qty;
            
            return transaction;
        }
        
        private void ParseFinancialYear(List<string> parts, SieFile sieFile)
        {
            // Format: #RAR yearIndex startDate endDate
            // Example: #RAR 0 20210101 20211231
            if (parts.Count < 4)
                return;
                
            if (int.TryParse(parts[1], out var yearIndex) &&
                DateTime.TryParseExact(parts[2], "yyyyMMdd",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out var startDate) &&
                DateTime.TryParseExact(parts[3], "yyyyMMdd",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out var endDate))
            {
                sieFile.FinancialYears.Add(new FinancialYear
                {
                    YearIndex = yearIndex,
                    StartDate = startDate,
                    EndDate = endDate
                });
            }
        }
        
        private void ParseAccount(List<string> parts, SieFile sieFile)
        {
            // Format: #KONTO accountNumber "accountName"
            // Example: #KONTO 1060 Hyresrätt
            if (parts.Count < 2)
                return;
                
            var account = new SieAccount
            {
                AccountNumber = parts[1],
                AccountName = parts.Count > 2 ? Unquote(parts[2]) : ""
            };
            sieFile.Accounts.Add(account);
        }
        
        private void ParseAccountType(List<string> parts, SieFile sieFile)
        {
            // Format: #KTYP accountNumber type
            // Example: #KTYP 1060 T
            if (parts.Count < 3)
                return;
                
            var account = sieFile.Accounts.FirstOrDefault(a => a.AccountNumber == parts[1]);
            if (account != null)
                account.AccountType = parts[2];
        }
        
        private void ParseSRUCode(List<string> parts, SieFile sieFile)
        {
            // Format: #SRU accountNumber sruCode
            // Example: #SRU 1060 7201
            if (parts.Count < 3)
                return;
                
            var account = sieFile.Accounts.FirstOrDefault(a => a.AccountNumber == parts[1]);
            if (account != null)
                account.SRUCode = parts[2];
        }
        
        private void ParseDimension(List<string> parts, SieFile sieFile)
        {
            // Format: #DIM dimensionNumber "dimensionName"
            // Example: #DIM 1 Resultatenhet
            if (parts.Count < 2)
                return;
                
            sieFile.Dimensions.Add(new SieDimension
            {
                DimensionNumber = parts[1],
                DimensionName = parts.Count > 2 ? Unquote(parts[2]) : ""
            });
        }
        
        private void ParseObject(List<string> parts, SieFile sieFile)
        {
            // Format: #OBJEKT dimensionNumber objectNumber "objectName"
            // Example: #OBJEKT 1 Nord "Kontor Nord"
            if (parts.Count < 3)
                return;
                
            sieFile.Objects.Add(new SieObject
            {
                DimensionNumber = parts[1],
                ObjectNumber = parts[2],
                ObjectName = parts.Count > 3 ? Unquote(parts[3]) : ""
            });
        }
        
        private void ParseOpeningBalance(List<string> parts, SieFile sieFile)
        {
            // Format: #IB yearIndex accountNumber amount [quantity]
            // Example: #IB 0 1221 421457.53
            if (parts.Count < 4)
                return;
                
            if (int.TryParse(parts[1], out var yearIndex) &&
                decimal.TryParse(parts[3], System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var amount))
            {
                var balance = new SieBalance
                {
                    YearIndex = yearIndex,
                    AccountNumber = parts[2],
                    Amount = amount
                };
                
                if (parts.Count > 4 && decimal.TryParse(parts[4], System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var qty))
                    balance.Quantity = qty;
                    
                sieFile.OpeningBalances.Add(balance);
            }
        }
        
        private void ParseClosingBalance(List<string> parts, SieFile sieFile)
        {
            // Format: #UB yearIndex accountNumber amount [quantity]
            // Example: #UB 0 1221 518057.53
            if (parts.Count < 4)
                return;
                
            if (int.TryParse(parts[1], out var yearIndex) &&
                decimal.TryParse(parts[3], System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var amount))
            {
                var balance = new SieBalance
                {
                    YearIndex = yearIndex,
                    AccountNumber = parts[2],
                    Amount = amount
                };
                
                if (parts.Count > 4 && decimal.TryParse(parts[4], System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var qty))
                    balance.Quantity = qty;
                    
                sieFile.ClosingBalances.Add(balance);
            }
        }
        
        private void ParseResult(List<string> parts, SieFile sieFile)
        {
            // Format: #RES yearIndex accountNumber amount [quantity]
            // Example: #RES 0 3041 -1690380.20
            if (parts.Count < 4)
                return;
                
            if (int.TryParse(parts[1], out var yearIndex) &&
                decimal.TryParse(parts[3], System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var amount))
            {
                var result = new SieResult
                {
                    YearIndex = yearIndex,
                    AccountNumber = parts[2],
                    Amount = amount
                };
                
                if (parts.Count > 4 && decimal.TryParse(parts[4], System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var qty))
                    result.Quantity = qty;
                    
                sieFile.Results.Add(result);
            }
        }
        
        private List<string> SplitSieLine(string line)
        {
            var result = new List<string>();
            var current = new StringBuilder();
            bool inQuotes = false;
            
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                    current.Append(c);
                }
                else if (char.IsWhiteSpace(c) && !inQuotes)
                {
                    if (current.Length > 0)
                    {
                        result.Add(current.ToString());
                        current.Clear();
                    }
                }
                else
                {
                    current.Append(c);
                }
            }
            
            if (current.Length > 0)
                result.Add(current.ToString());
                
            return result;
        }
        
        private string Unquote(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;
                
            if (value.StartsWith("\"") && value.EndsWith("\""))
                return value.Substring(1, value.Length - 2);
                
            return value;
        }
    }
}
