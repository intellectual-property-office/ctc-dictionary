using System.Collections.Generic;

namespace IPO.Dictionary.Models.DictionarySearch
{
    public class DictionarySearchValidationResult
    {
        public int Code { get; set; }
        public string? Error { get; set; }
        public string? ErrorCode { get; set; }

        public static DictionarySearchValidationResult CreateSuccessValidationResult()
        {
            return new DictionarySearchValidationResult()
            {
                Code = 200,
                Error = string.Empty,
                ErrorCode = string.Empty
            };
        }

        public static DictionarySearchValidationResult CreateUnauthorisedValidationResult()
        {
            return new DictionarySearchValidationResult()
            {
                Code = 401,
                Error = "Unauthorised",
                ErrorCode = string.Empty
            };
        }
        public static DictionarySearchValidationResult CreatePayloadTooLargeValidationResult(long sizeLimit)
        {
            return new DictionarySearchValidationResult()
            {
                Code = 413,
                Error = $"File size larger than {(sizeLimit / 1024f / 1024f).ToString("#0")} MB.",
                ErrorCode = string.Empty
            };
        }

        public static DictionarySearchValidationResult CreateUnsupportedFileTypesValidationResult(IEnumerable<string> acceptedFileExtensions)
        {
            return new DictionarySearchValidationResult()
            {
                Code = 415,
                Error = $"Unsupported file type, supported media types: {string.Join(", ", acceptedFileExtensions)}.",
                ErrorCode = string.Empty
            };
        }
        public static DictionarySearchValidationResult CreateUnsupportedMediaTypesValidationResult(IEnumerable<string> acceptedMediaExtensions)
        {
            return new DictionarySearchValidationResult()
            {
                Code = 415,
                Error = $"Unsupported media type, supported media types: {string.Join(", ", acceptedMediaExtensions)}.",
                ErrorCode = string.Empty
            };
        }
    }
}