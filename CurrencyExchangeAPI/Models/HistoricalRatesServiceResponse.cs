using System.Text.Json.Serialization;

namespace CurrencyExchangeAPI.Models
{
    public class HistoricalRatesServiceResponse
    {
        public decimal? Amount { get; set; }
        public string? Base { get; set; }

        [JsonPropertyName("start_date")]
        public string? StartDate { get; set; }

        [JsonPropertyName("end_date")]
        public string? EndDate { get; set; }

        public Dictionary<string, Dictionary<string, decimal>>? Rates { get; set; }
    }
}
