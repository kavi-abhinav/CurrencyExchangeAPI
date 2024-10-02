using CurrencyExchangeAPI.Models;

namespace CurrencyExchangeAPI.Services
{
    public interface ICurrencyService
    {
        Task<ExchangeResponse> GetExchangeRateAsync(string baseCurrencyCode);
        Task<ConvertCurrencyResponse> ConvertAmountAsync(decimal amount, string fromCurrencyCode, string? toCurrencyCode);
        Task<HistoricalRateResponse> GetHistoricalRatesAsync(string currencyCode, string fromDate, string toDate, int pageSize, int page);
   
    }
}
