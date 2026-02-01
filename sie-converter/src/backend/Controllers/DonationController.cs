using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SieConverterApi.Config;
using System;

namespace SieConverterApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DonationController : ControllerBase
    {
        private readonly DonationSettings _donationSettings;

        public DonationController(IOptionsMonitor<DonationSettings> donationSettings)
        {
            _donationSettings = donationSettings.CurrentValue;
        }

        [HttpGet("settings")]
        public IActionResult GetSettings()
        {
            return Ok(new
            {
                IsDonationEnabled = _donationSettings.IsDonationEnabled,
                TargetWeeklyAmount = _donationSettings.TargetWeeklyAmount,
                SwishNumber = _donationSettings.SwishNumber,
                PayPalEmail = _donationSettings.PayPalEmail,
                FreeConversionsPerDay = _donationSettings.FreeConversionsPerDay,
                MaxFileSizeMB = _donationSettings.MaxFileSizeMB
            });
        }

        [HttpGet("goal-progress")]
        public IActionResult GetGoalProgress()
        {
            // This would connect to a database to track actual donations
            // For now, returning placeholder values
            var random = new Random();
            var currentWeeklyAmount = random.Next(0, (int)_donationSettings.TargetWeeklyAmount);
            
            return Ok(new
            {
                CurrentAmount = currentWeeklyAmount,
                TargetAmount = _donationSettings.TargetWeeklyAmount,
                Percentage = Math.Round((double)currentWeeklyAmount / (double)_donationSettings.TargetWeeklyAmount * 100, 2),
                DaysRemaining = 7 // Placeholder
            });
        }
    }
}