using Microsoft.AspNetCore.Http;
using System;
using System.ComponentModel.DataAnnotations;

namespace IPO.Dictionary.Models.API.Validation
{
    public class OnlyOneFormFileIsAllowedAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
        {
            var httpContextAccessor = (IHttpContextAccessor)validationContext.GetService(typeof(IHttpContextAccessor))!; 
            var request = httpContextAccessor.HttpContext!.Request; 

            if (request.Form.Files.Count == 0)
                return CreateOneFormFileIsRequiredValidationResult();

            if (request.Form.Files.Count > 1)
                return CreateOnlyOneFormFileIsAllowedValidationResult();

            return ValidationResult.Success!; 
        }

        private static ValidationResult CreateOneFormFileIsRequiredValidationResult()
        {
            return new ValidationResult("The file is required.");
        }
         
        private static ValidationResult CreateOnlyOneFormFileIsAllowedValidationResult()
        {
            return new ValidationResult("Only one file is allowed.");
        }
    }
}