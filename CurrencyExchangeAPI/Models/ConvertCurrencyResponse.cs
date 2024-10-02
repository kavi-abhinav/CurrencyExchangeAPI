namespace CurrencyExchangeAPI.Models
{
    public class ConvertCurrencyResponse
    {
        public decimal InputAmount { get; set; }

        public string? FromCurrency { get; set; }

        public Dictionary<string,decimal>? ConvertedAmounts { get; set; }
        public Dictionary<string,decimal>? ExchangeRatesApplied { get; set; }
    }

}
