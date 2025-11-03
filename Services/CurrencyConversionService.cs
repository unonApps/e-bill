using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TAB.Web.Data;

namespace TAB.Web.Services
{
    public class CurrencyConversionService : ICurrencyConversionService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CurrencyConversionService> _logger;

        public CurrencyConversionService(
            ApplicationDbContext context,
            ILogger<CurrencyConversionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<decimal> ConvertCurrencyAsync(decimal amount, string fromCurrency, string toCurrency)
        {
            // Normalize currency codes to uppercase
            fromCurrency = fromCurrency?.ToUpper() ?? throw new ArgumentNullException(nameof(fromCurrency));
            toCurrency = toCurrency?.ToUpper() ?? throw new ArgumentNullException(nameof(toCurrency));

            // If same currency, no conversion needed
            if (fromCurrency == toCurrency)
            {
                return amount;
            }

            // Handle zero amount
            if (amount == 0)
            {
                return 0;
            }

            // Only support KSH <-> USD conversion
            if ((fromCurrency != "KSH" && fromCurrency != "USD") ||
                (toCurrency != "KSH" && toCurrency != "USD"))
            {
                throw new InvalidOperationException($"Only KSH <-> USD conversion is supported. Got {fromCurrency} to {toCurrency}");
            }

            try
            {
                // Get the latest exchange rate (1 USD = X KES)
                var exchangeRate = await _context.ExchangeRates
                    .OrderByDescending(er => er.Year)
                    .ThenByDescending(er => er.Month)
                    .FirstOrDefaultAsync();

                if (exchangeRate == null)
                {
                    _logger.LogWarning("No exchange rate found in the system");
                    throw new InvalidOperationException("No exchange rate found in the system. Please configure exchange rates.");
                }

                if (exchangeRate.Rate == 0)
                {
                    throw new InvalidOperationException("Exchange rate cannot be zero");
                }

                decimal result;

                if (fromCurrency == "USD" && toCurrency == "KSH")
                {
                    // Convert USD to KES: multiply by rate
                    result = amount * exchangeRate.Rate;
                    _logger.LogDebug("Converted {Amount} USD to {Result} KSH using rate {Rate}",
                        amount, result, exchangeRate.Rate);
                }
                else // fromCurrency == "KSH" && toCurrency == "USD"
                {
                    // Convert KES to USD: divide by rate
                    result = amount / exchangeRate.Rate;
                    _logger.LogDebug("Converted {Amount} KSH to {Result} USD using rate {Rate}",
                        amount, result, exchangeRate.Rate);
                }

                return Math.Round(result, 2);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting currency from {FromCurrency} to {ToCurrency}", fromCurrency, toCurrency);
                throw;
            }
        }

        public async Task<decimal?> GetExchangeRateAsync(string fromCurrency, string toCurrency)
        {
            fromCurrency = fromCurrency?.ToUpper() ?? throw new ArgumentNullException(nameof(fromCurrency));
            toCurrency = toCurrency?.ToUpper() ?? throw new ArgumentNullException(nameof(toCurrency));

            if (fromCurrency == toCurrency)
            {
                return 1.0m;
            }

            // Only support KSH <-> USD conversion
            if ((fromCurrency != "KSH" && fromCurrency != "USD") ||
                (toCurrency != "KSH" && toCurrency != "USD"))
            {
                return null;
            }

            try
            {
                // Get the latest exchange rate (1 USD = X KES)
                var exchangeRate = await _context.ExchangeRates
                    .OrderByDescending(er => er.Year)
                    .ThenByDescending(er => er.Month)
                    .FirstOrDefaultAsync();

                if (exchangeRate == null)
                {
                    return null;
                }

                // If converting USD to KSH, return the rate directly
                if (fromCurrency == "USD" && toCurrency == "KSH")
                {
                    return exchangeRate.Rate;
                }
                // If converting KSH to USD, return inverse rate
                else if (fromCurrency == "KSH" && toCurrency == "USD")
                {
                    return exchangeRate.Rate != 0 ? 1 / exchangeRate.Rate : null;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting exchange rate from {FromCurrency} to {ToCurrency}", fromCurrency, toCurrency);
                return null;
            }
        }

        public async Task<string> ConvertAndFormatAsync(decimal amount, string fromCurrency, string toCurrency)
        {
            fromCurrency = fromCurrency?.ToUpper() ?? throw new ArgumentNullException(nameof(fromCurrency));
            toCurrency = toCurrency?.ToUpper() ?? throw new ArgumentNullException(nameof(toCurrency));

            var convertedAmount = await ConvertCurrencyAsync(amount, fromCurrency, toCurrency);

            // Format based on target currency
            return FormatCurrency(convertedAmount, toCurrency);
        }

        private string FormatCurrency(decimal amount, string currencyCode)
        {
            var culture = currencyCode switch
            {
                "KSH" => new CultureInfo("sw-KE"), // Swahili (Kenya)
                "USD" => new CultureInfo("en-US"),
                "EUR" => new CultureInfo("en-GB"),
                _ => CultureInfo.InvariantCulture
            };

            var symbol = currencyCode switch
            {
                "KSH" => "KSH",
                "USD" => "$",
                "EUR" => "€",
                _ => currencyCode
            };

            // Format with thousand separators
            var formattedNumber = amount.ToString("N2", culture);

            // For KSH, prefix with "KSH "; for USD, prefix with "$"
            return currencyCode == "KSH"
                ? $"KSH {formattedNumber}"
                : $"{symbol}{formattedNumber}";
        }
    }
}
