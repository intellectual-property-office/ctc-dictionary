using IPO.Dictionary.Interfaces;
using IPO.Dictionary.Models.DictionarySearch;
using System.IO;

namespace IPO.Dictionary.BDDTests.DictionarySearch
{
    public class MockedDictionarySearchValidator : IDictionarySearchValidator
    {
        public string GetErrorMessage(ErrorType errorType)
        {
            throw new System.NotImplementedException();
        }

        public DictionarySearchValidationResult Validate(Stream file, string fileName, string contentType, int? optionalFileSizeHeader)
        {
            return DictionarySearchValidationResult.CreateSuccessValidationResult();
        }

        public ErrorType? ValidateFormat(Stream file, string fileName)
        {
            return null;
        }
    }
}