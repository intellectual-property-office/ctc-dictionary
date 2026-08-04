using IPO.Dictionary.Models.DictionarySearch;

namespace IPO.Dictionary.BDDTests.DictionarySearch
{
    public class SearchDictionaryRequest
    {
        public int ResultsId { get; set; }
        public string? FileName { get; set; }
        public DictionaryType DictionaryType { get; set; }
        public bool HasMatch { get; set; }
    }
}
