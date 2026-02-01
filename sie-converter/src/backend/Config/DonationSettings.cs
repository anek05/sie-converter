namespace SieConverterApi.Config
{
    public class DonationSettings
    {
        public bool IsDonationEnabled { get; set; } = true;
        public decimal TargetWeeklyAmount { get; set; } = 10000; // 10,000 SEK per week goal
        public string SwishNumber { get; set; } = "1234567890";
        public string PayPalEmail { get; set; } = "donations@example.com";
        
        // Free tier settings
        public int FreeConversionsPerDay { get; set; } = 5;
        public int MaxFileSizeMB { get; set; } = 10;
    }
}