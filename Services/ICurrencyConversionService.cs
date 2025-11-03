using System.Threading.Tasks;

namespace TAB.Web.Services
{
    /// <summary>
    /// Service for handling currency conversions using exchange rates from the database
    /// </summary>
    public interface ICurrencyConversionService
    {
        /// <summary>
        /// Convert an amount from one currency to another using the latest exchange rate
        /// </summary>
        /// <param name="amount">Amount to convert</param>
        /// <param name="fromCurrency">Source currency code (e.g., "KSH", "USD")</param>
        /// <param name="toCurrency">Target currency code (e.g., "KSH", "USD")</param>
        /// <returns>Converted amount</returns>
        Task<decimal> ConvertCurrencyAsync(decimal amount, string fromCurrency, string toCurrency);

        /// <summary>
        /// Get the latest exchange rate between two currencies
        /// </summary>
        /// <param name="fromCurrency">Source currency code</param>
        /// <param name="toCurrency">Target currency code</param>
        /// <returns>Exchange rate, or null if not found</returns>
        Task<decimal?> GetExchangeRateAsync(string fromCurrency, string toCurrency);

        /// <summary>
        /// Convert amount and return formatted string with currency symbol
        /// </summary>
        /// <param name="amount">Amount to convert</param>
        /// <param name="fromCurrency">Source currency code</param>
        /// <param name="toCurrency">Target currency code</param>
        /// <returns>Formatted string (e.g., "KSH 150,000" or "$1,250")</returns>
        Task<string> ConvertAndFormatAsync(decimal amount, string fromCurrency, string toCurrency);
    }
}
