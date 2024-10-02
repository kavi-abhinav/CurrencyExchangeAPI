namespace CurrencyExchangeAPI.Models
{
    public class HistoricalRateResponse
    {
        public decimal? Amount { get; set; }
        public string? Base { get; set; }

        public string? StartDate { get; set; }

        public string? EndDate { get; set; }

        public int TotalPages { get; set; }

        public int PageSize { get; set; }

        public int CurrentPage { get; set; }

        public string? NextPageUrl { get; set; }
        public Dictionary<string, Dictionary<string, decimal>>? Rates { get; set; }

    }
}
