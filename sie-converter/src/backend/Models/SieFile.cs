using System;
using System.Collections.Generic;

namespace SieConverterApi.Models
{
    /// <summary>
    /// Represents a complete SIE file with all its data
    /// </summary>
    public class SieFile
    {
        // Header Information
        public string Version { get; set; } = "4";
        public string Format { get; set; } = "PC8";
        public string ProgramName { get; set; }
        public string ProgramVersion { get; set; }
        public DateTime GeneratedDate { get; set; }
        
        // Company Information
        public string CompanyName { get; set; }
        public string CompanyNumber { get; set; }
        public string FileName { get; set; }
        public string AddressContact { get; set; }
        public string AddressStreet { get; set; }
        public string AddressPostal { get; set; }
        public string AddressPhone { get; set; }
        public string Currency { get; set; } = "SEK";
        public string TaxYear { get; set; }
        public string ChartOfAccountsType { get; set; }
        
        // Financial Years
        public List<FinancialYear> FinancialYears { get; set; } = new List<FinancialYear>();
        
        // Data
        public List<SieAccount> Accounts { get; set; } = new List<SieAccount>();
        public List<SieDimension> Dimensions { get; set; } = new List<SieDimension>();
        public List<SieObject> Objects { get; set; } = new List<SieObject>();
        public List<SieVerification> Verifications { get; set; } = new List<SieVerification>();
        public List<SieBalance> OpeningBalances { get; set; } = new List<SieBalance>();
        public List<SieBalance> ClosingBalances { get; set; } = new List<SieBalance>();
        public List<SieResult> Results { get; set; } = new List<SieResult>();
    }

    public class FinancialYear
    {
        public int YearIndex { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class SieAccount
    {
        public string AccountNumber { get; set; }
        public string AccountName { get; set; }
        public string AccountType { get; set; } // T = Asset, S = Liability, I = Income, K = Cost
        public string SRUCode { get; set; }
    }

    public class SieDimension
    {
        public string DimensionNumber { get; set; }
        public string DimensionName { get; set; }
    }

    public class SieObject
    {
        public string DimensionNumber { get; set; }
        public string ObjectNumber { get; set; }
        public string ObjectName { get; set; }
    }

    public class SieVerification
    {
        public string Series { get; set; }
        public string VerificationNumber { get; set; }
        public DateTime Date { get; set; }
        public string Description { get; set; }
        public DateTime? RegistrationDate { get; set; }
        public string RegistrationBy { get; set; }
        public List<SieTransaction> Transactions { get; set; } = new List<SieTransaction>();
    }

    public class SieTransaction
    {
        public string AccountNumber { get; set; }
        public decimal Amount { get; set; }
        public DateTime? TransactionDate { get; set; }
        public string Description { get; set; }
        public decimal? Quantity { get; set; }
        public string CreatedBy { get; set; }
        public List<SieDimensionReference> Dimensions { get; set; } = new List<SieDimensionReference>();
    }

    public class SieDimensionReference
    {
        public string DimensionNumber { get; set; }
        public string ObjectNumber { get; set; }
    }

    public class SieBalance
    {
        public int YearIndex { get; set; }
        public string AccountNumber { get; set; }
        public decimal Amount { get; set; }
        public decimal? Quantity { get; set; }
    }

    public class SieResult
    {
        public int YearIndex { get; set; }
        public string AccountNumber { get; set; }
        public decimal Amount { get; set; }
        public decimal? Quantity { get; set; }
    }
}
