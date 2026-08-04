using IPO.Dictionary.Models.DictionarySearch;
using System;

namespace IPO.Dictionary.Models
{
    public class ProcessResults
    { 
        public string? Match { get; private set; }
        public bool HasMatch { get; private set; }

        public ErrorType ErrorType { get; private set; }

        public bool HasError { get; private set; }

        public static ProcessResults CreateFailedProcessResultsModel(ErrorType errorType)
        {
            return new ProcessResults() { ErrorType = errorType, HasError = true };
        }

        public static ProcessResults CreateSuccesfulProcessResultsModel(bool hasMatch, string match = null!)
        {
            return new ProcessResults()
            {
                HasMatch = hasMatch,
                Match = ((hasMatch && string.IsNullOrWhiteSpace(match)) ? throw new ArgumentException("The match cannot be empty or null when there is a match.", nameof(match)) : match)
            };
        }
    }
}