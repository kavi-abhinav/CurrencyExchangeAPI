using CurrencyExchangeAPI.Exceptions;
using CurrencyExchangeAPI.Models;
using Microsoft.Extensions.Caching.Memory;

namespace CurrencyExchangeAPI.Services
{
    public class FrankfurterCurrencyService : ICurrencyService
    {

        private readonly ILogger<FrankfurterCurrencyService> _logger;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _cache;
        private readonly List<string> _unsupportedCurrenciesForConverion = new(){
            "TRY", "PLN", "THB", "MXN"
        };

        public FrankfurterCurrencyService(ILogger<FrankfurterCurrencyService> logger, 
            HttpClient httpClient, 
            IConfiguration configuration,
            IMemoryCache cache)
        {
            _logger = logger;
            _httpClient = httpClient;
            _configuration = configuration;
            _cache = cache;
        }

        public async Task<ConvertCurrencyResponse> ConvertAmountAsync(decimal amount, string fromCurrencyCode, string? toCurrencyCode)
        {      
            var conversionRates = await GetConversionRatesAsync(fromCurrencyCode, toCurrencyCode);

            //Filter unsupported currencies
            _unsupportedCurrenciesForConverion.ForEach(c => conversionRates?.Remove(c));

            var convertedAmount =  conversionRates?.Select(kvp => new KeyValuePair<string, decimal>(kvp.Key, kvp.Value * amount))
              .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);


            return new ConvertCurrencyResponse
            {
                InputAmount = amount,
                FromCurrency = fromCurrencyCode,
                ConvertedAmounts = convertedAmount,
                ExchangeRatesApplied = conversionRates
            };
        }

        private async Task<Dictionary<string,decimal>?> GetConversionRatesAsync(string fromCurrencyCode, string? toCurrencyCode)
        {
            //Note: Getting all exchange rate is and calculating conversion at client end is better than using Frankfurter convert api. Also mentioned in the docs (https://www.frankfurter.app/docs/)
            //We will be caching the exchange rates thereby further reducing network calls
            var response = await GetExchangeRateAsync(fromCurrencyCode);
            var rates = new Dictionary<string,decimal>();

            if (response.Rates == null)
                throw new Exception("Error occurred while converting amount");

            if (toCurrencyCode != null)
            {
                decimal exchangeRate;

                if (response.Rates.TryGetValue(toCurrencyCode.ToUpper(), out exchangeRate))
                {
                    rates.Add(toCurrencyCode.ToUpper(),exchangeRate);
                    return rates;
                }
                else
                    throw new InvalidCurrencyException($"Currency Code {toCurrencyCode} is not supported");
            }

            return response.Rates;
        }

        public async Task<ExchangeResponse> GetExchangeRateAsync(string baseCurrencyCode)
        {
            var apiResponse = await _cache.GetOrCreateAsync<ExchangeResponse>(baseCurrencyCode, async entry =>
            {
                entry.SetSlidingExpiration(TimeSpan.FromMinutes(30));
                entry.SetAbsoluteExpiration(TimeSpan.FromHours(1));

                var response =  await _httpClient.GetAsync($"latest?from={baseCurrencyCode}");

                if (response?.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var exchangeResponse = await response.Content.ReadFromJsonAsync<ExchangeResponse>();
                    if (exchangeResponse == null)
                        throw new Exception($"Error occurred while fetching rates for {baseCurrencyCode} : response body not serializable - {response}");
                    return exchangeResponse;
                }

                else if (response?.StatusCode == System.Net.HttpStatusCode.NotFound)
                    throw new InvalidCurrencyException($"Currency Code {baseCurrencyCode} is not supported");

                else
                    throw new Exception($"Error occurred while fetching rates for {baseCurrencyCode} : status({response?.StatusCode}) - {response}");
            });

           
            if (apiResponse == null)
                throw new Exception($"Error occurred while fetching rates for {baseCurrencyCode}");

            return apiResponse;

        }


        public async Task<HistoricalRateResponse> GetHistoricalRatesAsync(string currencyCode, string fromDate, string toDate, int pageSize, int page)
        {
            //Note: We have to fetch all data first and cache it and then serve pages on subsequent calls
            var response = await _cache.GetOrCreateAsync($"{currencyCode}_{fromDate}_{toDate}", async entry =>
            {
                entry.SetSlidingExpiration(TimeSpan.FromMinutes(30));
                entry.SetAbsoluteExpiration(TimeSpan.FromHours(1));

                var fromDateAsDate = DateOnly.Parse(fromDate);
                var toDateAsDate = DateOnly.Parse(toDate);

                if (fromDateAsDate >= toDateAsDate)
                    throw new InvalidDataException("fromdate should be less than toDate");

                var days = toDateAsDate.DayNumber - fromDateAsDate.DayNumber + 1;

                //Note: Frankfurter returns all data points for up to 90 days. 
                //Above that, it starts sampling by week or month. Therefore to get all data we need to make multiple requests and cache it to get all data. (https://www.frankfurter.app/docs/)
                if (days > 90)
                    return await CreateCombinedResponseAsync(currencyCode, fromDateAsDate, toDateAsDate);

                return await _httpClient.GetFromJsonAsync<HistoricalRatesServiceResponse>($"{fromDate}..{toDate}?from={currencyCode}");
            });

            if (response == null)
                throw new Exception($"Error occurred while fetching historical rates for {currencyCode}");

            var totalPages = GetTotalPages(response.Rates, pageSize);
            var filteredRates = GetFilteredRates(response.Rates, pageSize, page);
            var nextPageUrl = GetNextPageUrl(currencyCode, fromDate, toDate, pageSize, totalPages, page);

            return new HistoricalRateResponse
            {
            Amount = response.Amount,
            Base = response.Base,
            StartDate = response.StartDate,
            EndDate = response.EndDate,
            TotalPages = totalPages,
            PageSize = pageSize,
            CurrentPage = page,
            NextPageUrl = nextPageUrl,
            Rates = filteredRates
            };
        }

        private async Task<HistoricalRatesServiceResponse> CreateCombinedResponseAsync(string currencyCode, DateOnly fromDateAsDate, DateOnly toDateAsDate)
        {
            var days = toDateAsDate.DayNumber - fromDateAsDate.DayNumber + 1;
            var requests = days % 90 > 0 ? (int) (days / 90) + 1 : days/90;
            List<HistoricalRatesServiceResponse?> responses = new();

            for (int i = 0; i < requests; i++)
            {
                var fromDate = fromDateAsDate.AddDays(i*90);
                var toDate = fromDate.AddDays(89) > toDateAsDate ? toDateAsDate : fromDate.AddDays(89);
                responses.Add(await _httpClient.GetFromJsonAsync<HistoricalRatesServiceResponse>($"{fromDate.ToString("yyyy-MM-dd")}..{toDate.ToString("yyyy-MM-dd")}?from={currencyCode}"));
            }

            responses = responses.Where(x => x != null).ToList();
           
            if(responses.Count == 0)
                throw new Exception($"Error occurred while fetching historical rates for {currencyCode}");

            HistoricalRatesServiceResponse combinedResponse = new()
            {
                Amount = responses[0]?.Amount,
                Base = responses[0]?.Base,
                StartDate = fromDateAsDate.ToString("yyyy-MM-dd"),
                EndDate = toDateAsDate.ToString("yyyy-MM-dd"),
                Rates = new()
            };

            foreach(var response in responses)
            {
                if(response?.Rates != null)
                {
                    foreach (var key in response.Rates.Keys)
                    {
                        if(!combinedResponse.Rates.ContainsKey(key))
                            combinedResponse.Rates.Add(key, response.Rates[key]);
                    }
                }
                  
            }

            return combinedResponse;

        }

        private string GetNextPageUrl(string currencyCode, string fromDate, string toDate, int pageSize, int totalPages, int page)
        {
            var baseUrl = _configuration.GetValue<string>("API:BaseUrl");
            return page < totalPages ? $"{baseUrl}historicalRates?currencyCode={currencyCode}&fromDate={fromDate}&toDate={toDate}&pageSize={pageSize}&page={page+1}" : "";
        }

        private Dictionary<string, Dictionary<string, decimal>> GetFilteredRates(Dictionary<string, Dictionary<string, decimal>>? rates, int pageSize, int page)
        {
            if (rates == null || rates.Count() == 0)
                return new Dictionary<string, Dictionary<string, decimal>>();

            var firstElementIndex = pageSize * (page - 1);
            var lastElementIndex = firstElementIndex + (pageSize - 1);
            lastElementIndex = lastElementIndex > rates.Count()-1 ? rates.Count()-1 : lastElementIndex;

            var filteredKeys = rates.Keys.ToList<string>().Where((x,i)=> i >= firstElementIndex && i <= lastElementIndex);

            return rates.Where(x => filteredKeys.Contains(x.Key)).ToDictionary(); 
            
        }

        private int GetTotalPages(Dictionary<string, Dictionary<string, decimal>>? rates, int pageSize)
        {
            var totalData = rates?.Count() ?? 0;
            return totalData % pageSize > 0 ? (int)(totalData / pageSize) + 1 : totalData / pageSize;
        }


    }
}
