using CurrencyExchangeAPI.CustomValidators;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CurrencyExchangeAPI.UnitTests.Validation
{
    public class ValidationTests
    {
        [Theory]
        [InlineData("try", "try is not a supported currency for conversion")]
        [InlineData("TRY", "TRY is not a supported currency for conversion")]
        [InlineData("PLN", "PLN is not a supported currency for conversion")]
        [InlineData("pln", "pln is not a supported currency for conversion")]
        [InlineData("THB", "THB is not a supported currency for conversion")]
        [InlineData("thb", "thb is not a supported currency for conversion")]
        [InlineData("MXN", "MXN is not a supported currency for conversion")]
        [InlineData("mxn", "mxn is not a supported currency for conversion")]
        public void CurrencyValidationAttributeShouldFailForInvalidCurrencies(string input, string errorMessage)
        {
            var attribute = new CurrencyValidationAttribute();
            var result = attribute.GetValidationResult(input, new ValidationContext(input));
            var isSuccess = result == ValidationResult.Success;
            Assert.False(isSuccess);
            Assert.Equal(errorMessage, result?.ErrorMessage);
        }


        [Theory]
        [InlineData("usd")]
        [InlineData("USD")]
        [InlineData("INR")]

        public void CurrencyValidationAttributeShouldPassForValidCurrencies(string input)
        {
            var attribute = new CurrencyValidationAttribute();
            var result = attribute.GetValidationResult(input, new ValidationContext(input));
            var isSuccess = result == ValidationResult.Success;
            Assert.True(isSuccess);
        }


        [Theory]
        [InlineData("2-2-1994", "Incorrect date format, expected format is yyyy-mm-dd")]
        [InlineData("02-20-2023", "Incorrect date format, expected format is yyyy-mm-dd")]
        [InlineData("2000-02-30", "fromDate is not a valid date")]
        [InlineData("1994-01-01", "fromDate cannot be older than January 4, 1999")]
        [InlineData("2040-01-01", "fromDate cannot be in future")]
        public void FromDateValidationAttributeShouldFailForInvalidDates(string input, string errorMessage)
        {
            var attribute = new FromDateValidationAttribute();
            var result = attribute.GetValidationResult(input, new ValidationContext(input));
            var isSuccess = result == ValidationResult.Success;
            Assert.False(isSuccess);
            Assert.Equal(errorMessage, result?.ErrorMessage);
        }


        [Theory]
        [InlineData("2000-01-01")]
        [InlineData("2020-03-16")]
        [InlineData("2024-07-01")]
        public void FromDateValidationAttributeShouldPassForValidDates(string input)
        {
            var attribute = new FromDateValidationAttribute();
            var result = attribute.GetValidationResult(input, new ValidationContext(input));
            var isSuccess = result == ValidationResult.Success;
            Assert.True(isSuccess);
        }


        [Theory]
        [InlineData("2-2-1994", "Incorrect date format, expected format is yyyy-mm-dd")]
        [InlineData("02-20-2023", "Incorrect date format, expected format is yyyy-mm-dd")]
        [InlineData("2000-02-30", "toDate is not a valid date")]
        [InlineData("2040-01-01", "toDate cannot be in future")]
        public void ToDateValidationAttributeShouldFailForInvalidDates(string input, string errorMessage)
        {
            var attribute = new ToDateValidationAttribute();
            var result = attribute.GetValidationResult(input, new ValidationContext(input));
            var isSuccess = result == ValidationResult.Success;
            Assert.False(isSuccess);
            Assert.Equal(errorMessage, result?.ErrorMessage);
        }


        [Theory]
        [InlineData("2000-01-01")]
        [InlineData("2020-03-16")]
        [InlineData("2024-07-01")]
        public void ToDateValidationAttributeShouldPassForValidDates(string input)
        {
            var attribute = new ToDateValidationAttribute();
            var result = attribute.GetValidationResult(input, new ValidationContext(input));
            var isSuccess = result == ValidationResult.Success;
            Assert.True(isSuccess);
        }
    }
}
