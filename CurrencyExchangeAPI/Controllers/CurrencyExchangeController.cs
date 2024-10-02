using CurrencyExchangeAPI.CustomValidators;
using CurrencyExchangeAPI.Models;
using CurrencyExchangeAPI.Services;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Net;

namespace CurrencyExchangeAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CurrencyExchangeController : ControllerBase
    {
        private readonly ICurrencyService _currencyService;

        public CurrencyExchangeController(ICurrencyService currencyService)
        {
            _currencyService = currencyService;
        }

        [HttpGet]
        [Route("exchangeRates/{currencyCode}")]
        public async Task<IActionResult> GetExchangeRates(string currencyCode)
        {              
            var exchangeRateResponse =  await _currencyService.GetExchangeRateAsync(currencyCode);
            return Ok(exchangeRateResponse);              
        }
        
        [HttpGet]
        [Route("convert")]
        public async Task<IActionResult> GetConvertedAmount([Required]decimal amount,
            [CurrencyValidation] string fromCurrencyCode, 
            [CurrencyValidation] string? toCurrencyCode = null)
        {        
            var conversionResponse = await _currencyService.ConvertAmountAsync(amount, fromCurrencyCode, toCurrencyCode);  
            return Ok(conversionResponse);          
        }

        [HttpGet]
        [Route("historicalRates/{currencyCode}")]
        public async Task<IActionResult> GetHistoricalRates(string currencyCode,
            [FromDateValidation] string fromDate,
            [ToDateValidation] string toDate,
            [Range(1, 90)] int pageSize = 90, //Restricting page size to max 90 to avoid multiple network calls to frankfurter for single response and to limit page size
            int page = 1)
        {
            var conversionResponse = await _currencyService.GetHistoricalRatesAsync(currencyCode, fromDate, toDate, pageSize, page);
            return Ok(conversionResponse);
        }
    }
}
