using System;

namespace IPO.Dictionary.Models.DictionarySearch.Processing
{
    public class MatchingResult
    {  
        public string? Match { get; private set; } 

        public bool HasMatch { get; private set; }

        public static MatchingResult CreateMatchResultWithoutMatch()
        {
            return new MatchingResult();
        }

        public static MatchingResult CreateMatchResultWithMatch(string match)
        {
            return new MatchingResult() { Match = match ?? throw new ArgumentNullException(nameof(match)), HasMatch = true };
        }
    }
}