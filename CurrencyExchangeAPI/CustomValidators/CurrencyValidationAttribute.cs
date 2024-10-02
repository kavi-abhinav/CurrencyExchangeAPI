using System.ComponentModel.DataAnnotations;

namespace CurrencyExchangeAPI.CustomValidators
{
    public class CurrencyValidationAttribute : ValidationAttribute
    {
        private readonly List<string> _unsupportedCurrenciesForConverion = new(){
            "TRY", "PLN", "THB", "MXN"
        };
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            var input = value as string;

            if (input!=null && _unsupportedCurrenciesForConverion.Contains(input.ToUpper()))
                return new ValidationResult($"{input} is not a supported currency for conversion");

            else
                return ValidationResult.Success;

        }
    }
}
