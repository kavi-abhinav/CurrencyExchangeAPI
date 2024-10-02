using Castle.Core.Logging;
using CurrencyExchangeAPI.Controllers;
using CurrencyExchangeAPI.Exceptions;
using CurrencyExchangeAPI.Models;
using CurrencyExchangeAPI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CurrencyExchangeAPI.UnitTests.Controllers
{
    public class CurrencyExchangeControllerTests
    {
        private readonly CurrencyExchangeController _currencyExchangeController;
        private readonly TestHttpHandler _testMessageHandler;


        //Test Fixture Data
        private readonly ExchangeResponse _expectedExchangeResponse = new()
        {
            Amount = 1,
            Base = "USD",
            Date = "2024-09-07",
            Rates = new Dictionary<string, decimal>
            {
                ["AUD"] = 1.4864M,
                ["BGN"] = 1.7615M,
                ["BRL"] = 5.571M,
                ["THB"] = 33.53M,
                ["TRY"] = 33.99M,
                ["PLN"] = 3.8548M,
                ["MXN"] = 19.9505M,
            }
        };

        private readonly HistoricalRatesServiceResponse _historicalRatesServiceResponse = new()
        {
            Amount = 1,
            Base = "USD",
            StartDate = "2024-07-08",
            EndDate = "2024-07-10",
            Rates = new Dictionary<string, Dictionary<string, decimal>>
            {
                ["2024-07-08"] = new Dictionary<string, decimal>
                {
                    ["AUD"] = 1.48784M,
                    ["BGN"] = 1.7215M,
                    ["BRL"] = 5.571M
                },
                ["2024-07-09"] = new Dictionary<string, decimal>
                {
                    ["AUD"] = 1.4864M,
                    ["BGN"] = 1.7135M,
                    ["BRL"] = 5.5712M
                },
                ["2024-07-10"] = new Dictionary<string, decimal>
                {
                    ["AUD"] = 1.4864M,
                    ["BGN"] = 1.7615M,
                    ["BRL"] = 5.5714M
                },

            }
        };


        public CurrencyExchangeControllerTests()
        {
            //Setting up common objects and config
            var inMemorySettings = new Dictionary<string, string?> {
                {"API:BaseUrl", "https://localhost:7037/CurrencyExchange/"}
            };
            IConfiguration _mockConfiguration = new ConfigurationBuilder()
                                                .AddInMemoryCollection(inMemorySettings)
                                                .Build();
            Mock<ILogger<FrankfurterCurrencyService>> _mocklogger = new Mock<ILogger<FrankfurterCurrencyService>>();

            var _cache = new MemoryCache(new MemoryCacheOptions());
            _testMessageHandler = new TestHttpHandler();
            HttpClient _httpClient = new HttpClient(_testMessageHandler);
            _httpClient.BaseAddress = new Uri("https://localhost:7037/");

            var _currencyService = new FrankfurterCurrencyService(_mocklogger.Object, _httpClient, _mockConfiguration, _cache);
            _currencyExchangeController = new CurrencyExchangeController(_currencyService);

        }

        [Fact]
        public async Task GetExchangeRatesReturnsOK()
        {
            //setup mock http response
            HttpResponseMessage successResponse = new()
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(_expectedExchangeResponse)),

            };

            _testMessageHandler.SetResponse(successResponse);

            //Execute
            var result = await _currencyExchangeController.GetExchangeRates("USD") as OkObjectResult;

            //Assert
            Assert.NotNull(result);
            Assert.Equal(StatusCodes.Status200OK, result?.StatusCode);
            Assert.IsType<ExchangeResponse>(result?.Value);

            var resultBody = result?.Value as ExchangeResponse;
            Assert.Equal(_expectedExchangeResponse.Amount, resultBody?.Amount);
            Assert.Equal(_expectedExchangeResponse.Base, resultBody?.Base);
            Assert.Equal(_expectedExchangeResponse.Date, resultBody?.Date);
            if(_expectedExchangeResponse.Rates != null)
                Assert.All(_expectedExchangeResponse.Rates.Keys, (key)=> resultBody?.Rates?.ContainsKey(key));
        }

        [Fact]
        public async Task GetExchangeRatesThrowsExceptionOnHttpFailure()
        {
            //setup mock http response
            HttpResponseMessage failureResponse = new()
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content = null,

            };

            _testMessageHandler.SetResponse(failureResponse);

            //Execute and Assert
            await Assert.ThrowsAsync<Exception>(async ()=> await _currencyExchangeController.GetExchangeRates("USD"));

        }

        [Fact]
        public async Task GetExchangeRatesThrowsExceptionOnInvalidHttpResponse()
        {
            //setup mock http response
            HttpResponseMessage failureResponse = new()
            {
                StatusCode = HttpStatusCode.OK,
                Content = null,
            };

            _testMessageHandler.SetResponse(failureResponse);

            //Execute and Assert
            await Assert.ThrowsAnyAsync<Exception>(async () => await _currencyExchangeController.GetExchangeRates("USD"));

        }

        [Fact]
        public async Task GetExchangeRatesThrowsInvalidCurrencyExceptionOnHttpNotFound()
        {
            //setup mock http response
            HttpResponseMessage failureResponse = new()
            {
                StatusCode = HttpStatusCode.NotFound,
                Content = null,

            };

            _testMessageHandler.SetResponse(failureResponse);

            //Execute and Assert
            await Assert.ThrowsAsync<InvalidCurrencyException>(async () => await _currencyExchangeController.GetExchangeRates("KEDSF"));

        }


        [Theory]
        [InlineData("USD", "AUD", 2, 2.9728)]
        [InlineData("USD", "BRL", 29.5, 164.3445)]
        public async Task GetConvertedAmountReturnsCorrectResults(string from, string to, decimal amount, decimal expectedResult)
        {
            //setup mock http response
            HttpResponseMessage successResponse = new()
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(_expectedExchangeResponse)),

            };

            _testMessageHandler.SetResponse(successResponse);

            var result = await _currencyExchangeController.GetConvertedAmount(amount, from, to) as OkObjectResult;
            Assert.NotNull(result);
            Assert.Equal(StatusCodes.Status200OK, result?.StatusCode);
            Assert.IsType<ConvertCurrencyResponse>(result?.Value);
            Assert.Equal(expectedResult, (result?.Value as ConvertCurrencyResponse)?.ConvertedAmounts?[to]);
        }

        [Theory]
        [InlineData("USD", 12)]
        public async Task GetConvertedAmountShouldExcludeUnsupportedCurrencies(string from, decimal amount)
        {
            //setup mock http response
            HttpResponseMessage successResponse = new()
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(_expectedExchangeResponse)),

            };
            List<string> _unsupportedCurrenciesForConverion = new(){
                "TRY", "PLN", "THB", "MXN"
            };

             _testMessageHandler.SetResponse(successResponse);

            var result = await _currencyExchangeController.GetConvertedAmount(amount, from) as OkObjectResult;
            Assert.NotNull(result);
            Assert.Equal(StatusCodes.Status200OK, result?.StatusCode);
            Assert.IsType<ConvertCurrencyResponse>(result?.Value);
            var resultBody = result?.Value as ConvertCurrencyResponse;

            _unsupportedCurrenciesForConverion.ForEach((currency) =>
            {
                Assert.False(resultBody?.ConvertedAmounts?.Keys?.Contains(currency));
                Assert.False(resultBody?.ExchangeRatesApplied?.Keys?.Contains(currency));
            });
            
        }


        [Theory]
        [InlineData("USD", "INR", 2)]
        [InlineData("USD", "SFK", 29.5)]
        public async Task GetConvertedAmountThrowsInvalidCurrencyExceptionForMissingConversionRates(string from, string to, decimal amount)
        {
            //setup mock http response

            HttpResponseMessage successResponse = new()
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(_expectedExchangeResponse)),

            };

            _testMessageHandler.SetResponse(successResponse);


            //Execute and Assert
            await Assert.ThrowsAsync<InvalidCurrencyException>(async () => await _currencyExchangeController.GetConvertedAmount(amount, from, to));
        }



        [Theory]
        [InlineData("ABCD", "AUD", 2)]
        [InlineData("ABCD", "BRL", 29.5)]
        public async Task GetConvertedAmountThrowsInvalidCurrencyExceptionOnHttpNotFound(string from, string to, decimal amount)
        {
            //setup mock http response

            HttpResponseMessage successResponse = new()
            {
                StatusCode = HttpStatusCode.NotFound,
                Content = new StringContent(JsonSerializer.Serialize(_expectedExchangeResponse)),

            };

            _testMessageHandler.SetResponse(successResponse);


            //Execute and Assert
            await Assert.ThrowsAsync<InvalidCurrencyException>(async () => await _currencyExchangeController.GetConvertedAmount(amount, from, to));
        }



        [Fact]
        public async Task GetHistoricalRatesReturnsOKWithSinglePageResponse()
        {
            //setup mock http response
            HttpResponseMessage successResponse = new()
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(_historicalRatesServiceResponse)),

            };

            _testMessageHandler.SetResponse(successResponse);

            var expectedResponse = new HistoricalRateResponse
            {
                Amount = _historicalRatesServiceResponse.Amount,
                Base = _historicalRatesServiceResponse.Base,
                StartDate = _historicalRatesServiceResponse.StartDate,
                EndDate = _historicalRatesServiceResponse.EndDate,
                TotalPages = 1,
                PageSize = 90,
                CurrentPage = 1,
                NextPageUrl = "",
                Rates = _historicalRatesServiceResponse.Rates
            };

            //Execute
            var result = await _currencyExchangeController.GetHistoricalRates("USD", "2024-07-08", "2024-07-10") as OkObjectResult;

            //Assert
            Assert.NotNull(result);
            Assert.Equal(StatusCodes.Status200OK, result?.StatusCode);
            Assert.IsType<HistoricalRateResponse>(result?.Value);

            var resultBody = result?.Value as HistoricalRateResponse;
            Assert.Equal(expectedResponse.Amount, resultBody?.Amount);
            Assert.Equal(expectedResponse.Base, resultBody?.Base);
            Assert.Equal(expectedResponse.StartDate, resultBody?.StartDate);
            Assert.Equal(expectedResponse.EndDate, resultBody?.EndDate);
            if (expectedResponse.Rates != null)
                Assert.All(expectedResponse.Rates.Keys, (key) => resultBody?.Rates?.ContainsKey(key));
        }

        [Fact]
        public async Task GetHistoricalRatesReturnsOKWithMultiplePageResponse()
        {
            //setup mock http response
            HttpResponseMessage successResponse = new()
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(_historicalRatesServiceResponse)),

            };

            _testMessageHandler.SetResponse(successResponse);

            var expectedResponse = new HistoricalRateResponse
            {
                Amount = _historicalRatesServiceResponse.Amount,
                Base = _historicalRatesServiceResponse.Base,
                StartDate = _historicalRatesServiceResponse.StartDate,
                EndDate = _historicalRatesServiceResponse.EndDate,
                TotalPages = 3,
                PageSize = 1,
                CurrentPage = 1,
                NextPageUrl = "https://localhost:7037/CurrencyExchange/historicalRates?currencyCode=USD&fromDate=2024-07-08&toDate=2024-07-10&pageSize=1&page=2",
                Rates = _historicalRatesServiceResponse.Rates
            };

            //Execute
            var result = await _currencyExchangeController.GetHistoricalRates("USD", "2024-07-08", "2024-07-10",1) as OkObjectResult;

            //Assert
            Assert.NotNull(result);
            Assert.Equal(StatusCodes.Status200OK, result?.StatusCode);
            Assert.IsType<HistoricalRateResponse>(result?.Value);

            var resultBody = result?.Value as HistoricalRateResponse;
            Assert.Equal(expectedResponse.Amount, resultBody?.Amount);
            Assert.Equal(expectedResponse.Base, resultBody?.Base);
            Assert.Equal(expectedResponse.StartDate, resultBody?.StartDate);
            Assert.Equal(expectedResponse.EndDate, resultBody?.EndDate);
            Assert.Equal(expectedResponse.NextPageUrl, resultBody?.NextPageUrl);
            Assert.Equal(expectedResponse.CurrentPage, resultBody?.CurrentPage);
            Assert.Equal(expectedResponse.PageSize, resultBody?.PageSize);
            Assert.Equal(expectedResponse.TotalPages, resultBody?.TotalPages);
            if (expectedResponse.Rates != null)
                Assert.All(expectedResponse.Rates.Keys, (key) => resultBody?.Rates?.ContainsKey(key));
        }


        [Fact]
        public async Task GetHistoricalRatesThrowsInvalidDataExceptionForInvalidDates()
        {
            //setup mock http response
            HttpResponseMessage successResponse = new()
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(_historicalRatesServiceResponse)),

            };

            _testMessageHandler.SetResponse(successResponse);

            var expectedResponse = new HistoricalRateResponse
            {
                Amount = _historicalRatesServiceResponse.Amount,
                Base = _historicalRatesServiceResponse.Base,
                StartDate = _historicalRatesServiceResponse.StartDate,
                EndDate = _historicalRatesServiceResponse.EndDate,
                TotalPages = 3,
                PageSize = 1,
                CurrentPage = 1,
                NextPageUrl = "https://localhost:7037/CurrencyExchange/historicalRates?currencyCode=USD&fromDate=2024-07-08&toDate=2024-07-10&pageSize=1&page=2",
                Rates = _historicalRatesServiceResponse.Rates
            };

            //Execute and Assert
            await Assert.ThrowsAsync<InvalidDataException>(async () => await _currencyExchangeController.GetHistoricalRates("USD", "2024-07-08", "2024-01-10", 1));
        }


        [Fact]
        public async Task GetHistoricalRatesThrowsExceptionOnHttpFailure()
        {
            //setup mock http response

            HttpResponseMessage successResponse = new()
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content = null,

            };

            _testMessageHandler.SetResponse(successResponse);


            //Execute and Assert
            await Assert.ThrowsAnyAsync<Exception>(async () => await _currencyExchangeController.GetHistoricalRates("USD", "2024-07-08", "2024-07-10"));
        }

    }
}
