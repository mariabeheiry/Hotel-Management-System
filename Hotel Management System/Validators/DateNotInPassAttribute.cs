using System;
using System.ComponentModel.DataAnnotations;

namespace HotelBookingSystem.Validators
{
    public class DateNotInPastAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var date = (DateTime?)value;
            if (date != null && date.Value.Date < DateTime.Today)
            {
                return new ValidationResult(ErrorMessage ?? "Date cannot be in the past.");
            }
            return ValidationResult.Success;
        }
    }
}
