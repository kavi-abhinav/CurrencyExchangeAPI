using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace CurrencyExchangeAPI.CustomValidators
{
    public class FromDateValidationAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            var regex = new Regex("^\\d{4}\\-(0[1-9]|1[012])\\-(0[1-9]|[12][0-9]|3[01])$");
            var inputString = value as string ?? "";

            if (!regex.IsMatch(inputString))
                return new ValidationResult("Incorrect date format, expected format is yyyy-mm-dd");

            DateOnly inputDate;

            if (!DateOnly.TryParse(inputString, out inputDate))
                return new ValidationResult("fromDate is not a valid date");


            var oldestExchangeRateDate = new DateOnly(1999, 01, 04); //as per docs https://www.frankfurter.app/docs/

            if (inputDate < oldestExchangeRateDate)
                return new ValidationResult("fromDate cannot be older than January 4, 1999");

            var todaysDate = DateOnly.FromDateTime(DateTime.Now);

            if (inputDate > todaysDate)
                return new ValidationResult("fromDate cannot be in future");
            else
                return ValidationResult.Success;
        }
    }
}
