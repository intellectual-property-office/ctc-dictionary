using IPO.Dictionary.Models.DictionarySearch;

namespace IPO.Dictionary.Interfaces
{
    public interface IDictionarySearchValidator
    {
        string @GetErrorMessage(ErrorType errorType);
        DictionarySearchValidationResult Validate(Stream file, string fileName, string contentType, int? optionalFileSizeHeader);
        ErrorType? ValidateFormat(Stream file, string fileName); 
    }
}