using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace CurrencyExchangeAPI.CustomValidators
{
    public class ToDateValidationAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            var regex = new Regex("^\\d{4}\\-(0[1-9]|1[012])\\-(0[1-9]|[12][0-9]|3[01])$");
            var inputString = value as string ?? "";

            if (!regex.IsMatch(inputString))
                return new ValidationResult("Incorrect date format, expected format is yyyy-mm-dd");

            DateOnly inputDate;

            if (!DateOnly.TryParse(inputString, out inputDate))
                return new ValidationResult("toDate is not a valid date");

            var todaysDate = DateOnly.FromDateTime(DateTime.Now);

            if (inputDate > todaysDate)
                return new ValidationResult("toDate cannot be in future");
            else
                return ValidationResult.Success;
        }
    }
}
